using System;
using System.Collections.Generic;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB.Queries;
using Aurender.Core.Player.mpd;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.DB.Managers
{
    public class AlbumManager : AbstractManager<IAlbumFromDB>, IAlbumManager
    {
        public AlbumManager(IDB db) : base(db)
        {
            var qf = new Queries.QueryFactoryForAlbumOrderByAlbumTitle();
            this.cursor = new Windowing.WindowedData<IAlbumFromDB>(this.db, qf, EViewType.Albums, db.popupDelegate, x => new Album(x));
            LoadTotalItemCount();

            this.Y = typeof(Album);
        }

        public IAlbumFromDB GetAlbumByID(int albumID)
        {
            Album album = null;
            if (!db.IsOpen())
            {
                return null;
            }
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select album_id, album, artistNames, songCount, cover_m, album_key from albums where album_id = ?";

            using (var con = this.db.CreateConnection())
            {
                var cmd = con.CreateCommand(query, albumID);
                
                try
                {
                    var result = cmd.ExecuteDeferredQuery(QueryFactoryForAlbumOrderByAlbumTitle.types);

                    foreach (var obj in result)
                    {
                        album = new Album(obj);

                        break;
                    }

                }
                catch (Exception ex)
                {
                    IARLogStatic.Error("AlbumManager", $"Failed to excute : {ex.Message}", ex);
                }
            }

            return album;
        }
        public IAlbumFromDB GetAlbumByFilePath(String path)
        {
            Album album = null;
            if (!db.IsOpen())
            {
                return null;
            }
            //song_id, artistsNames, album, indexOfSong, song_title, duration, totalTracks, track_id, a.songRate, a.f2 "
            String query = "select album_id, album, artistNames, songCount, cover_m, album_key from albums where album_id ="
                                +    " (select album_id from songs where song_id =(select song_id from tracks where fileName = ?))";

            using (var con = this.db.CreateConnection())
            {
                var cmd = con.CreateCommand(query, path);

                try
                {
                    var result = cmd.ExecuteDeferredQuery(QueryFactoryForAlbumOrderByAlbumTitle.types);

                    foreach (var obj in result)
                    {
                        album = new Album(obj);

                        break;
                    }

                }
                catch (Exception ex)
                {
                    IARLogStatic.Error("AlbumManager", $"Failed to excute : {ex.Message}", ex);
                }
            }

            return album;
        }
        
        public object GetImageBySongID(int songID)
        {
            object image = null;

            try
            {
                String query = "select cover_m from albums where album_id = (select album_id from songs where song_id = ? )";
                image = this.GetImageDataWithQuery(query, songID);
            }
            catch (Exception ex)
            {
                IARLogStatic.Error("AlbumManager", $"Failed to excute : {ex.Message}", ex);
            }
            
            return image;
        }

        public object GetImageByFilePath(String filePath)
        {
            object image = null;

            try
            {
                String query = "select cover_m from albums where album_id = (select album_id from songs where song_id = (select song_id from tracks where fileName = ? ))";
                image = this.GetImageDataWithQuery(query, filePath);
            }
            catch (Exception ex)
            {
                IARLogStatic.Error("AlbumManager", $"Failed to excute : {ex.Message}", ex);
            }

            return image;
        }

        public String GetLargeSizeFrontCoverURLBySongID(int songID)
        {
            String query = "select coverFileName, imageType from album_meta m join album_images a on m.album_id = a.album_id "
                + " where a.album_id = (select album_id from songs where song_id = ?)";

            String coverFileName = this.ExecuteQueryForString(songID, query);
            String url = ConvertToCoverFilePath(coverFileName);
            return url;
        }

        public String GetLargeSizeFrontCoverURLByFilePath(String filePath)
        {
            String query = "select coverFileName, imageType from album_meta m join album_images a on m.album_id = a.album_id "
                + " where a.album_id = (select album_id from songs where song_id = (select song_id from tracks where fileName =? ))";

            String coverFileName = this.ExecuteQueryForString(filePath, query);
            String url = ConvertToCoverFilePath(coverFileName);
            return url;
        }

        public String GetLargeSizeBackCoverURLBySongID(int songID)
        {
            String query = "select coverFileName, imageType from album_meta m join album_images a on m.album_id = a.album_id "
               + " where a.album_id = (select album_id from songs where song_id = ?)";

            String coverFileName = this.ExecuteQueryForString(songID, query);
            String url = ConvertToCoverFilePath(coverFileName, "-back");
            return url;
        }
        public String GetLargeSizeBackCoverURLByFilePath(String filePath)
        {
            String query = "select coverFileName, imageType from album_meta m join album_images a on m.album_id = a.album_id "
                + " where a.album_id = (select album_id from songs where song_id = (select song_id from tracks where fileName =? ))";

            String coverFileName = this.ExecuteQueryForString(filePath, query);
            String url = ConvertToCoverFilePath(coverFileName, "-back");
            return url;
        }

        public bool HasImageForAlbumID(int albumID, int imageIndex)
        {
            String query = $"select album_id from album_images where album_id = ? and imageOrder = {imageIndex} ";
            int has = this.ExecuteQueryForInt(albumID, query);
            return has != -1;
        }
        public bool HasImageForSongID(int songID, int imageIndex)
        {
            String query = $"select album_id from album_images where album_id = (select album_id from songs where song_id = ?) and imageOrder = {imageIndex} ";
            int has = this.ExecuteQueryForInt(songID, query);
            return has != -1;
        }
        public bool HasImageForFilePath(String filePath, int imageIndex)
        {
            String query = "select album_id from album_images "
                + "where album_id = (select album_id from songs where song_id = (select song_id from tracks where fileName = ?))"
                + $" and imageOrder = {imageIndex} ";
            int has = this.ExecuteQueryForInt(filePath, query);
            return has != 0;
        }
        
        public override List<Ordering> SupportedOrdering()
        {
            return new List<Ordering>()
            {
                //Ordering.AddedDate,
                Ordering.AlbumName,
                Ordering.AritstNameAndReleaseYear,
                Ordering.ArtistNameAndAlbumName,
                Ordering.ReleaseYear,
                Ordering.ReleaseYearDesc,
            };
        }

        public override Ordering CurrentOrder
        {
            get => this.QueryFactoryForAlbum.Ordering;
        }

        private QueryFactoryForAlbumOrderByAlbumTitle QueryFactoryForAlbum
        {
            get => (QueryFactoryForAlbumOrderByAlbumTitle)this.cursor.queryFactory;
        }

        private string ConvertToCoverFilePath(string coverFileName, String imageKind = "")
        {
            String fullName;
            if (imageKind != null && imageKind.Length == 0)
                fullName = coverFileName;
            else
            {
                int position = coverFileName.LastIndexOf(".");
                fullName = coverFileName.Insert(position, imageKind);
            }
            String url = AurenderBrowser.GetCurrentAurender().urlFor("aurender/" + fullName);

            return url;
        }

        private string ImageExtFor(int imageType)
        {
            String ext;
            if (imageType == 1)
            {
                ext = "jpg";
            }
            else if (imageType == 2)
            {
                ext = "pdf";
            }
            else
            {
                ext = ".png";
            }
            return ext;
        }

        private object GetImageDataWithQuery(String query, Object param)
        {
            if (!db.IsOpen())
            {
                return null;
            }

            Byte[] image = null;

            using (var con = this.db.CreateConnection())
            {
                var cmd = con.CreateCommand(query, param);

                try
                {
                    var result = cmd.ExecuteScalar<byte[]>();

                    if (result != null)
                    {
                        image = result;
                    }
                    // if result is null, image doesn't exist.
                }
                catch (Exception ex)
                {
                    IARLogStatic.Error("AlbumManager", $"Failed to get image : {ex.Message}", ex, new Dictionary<string, string>
                    {
                        { "Query", query },
                        { "Parameter", param.ToString() }
                    });
                }
            }
            object resultImage = null;
            if (image != null && image.Length > 0)
            {
                resultImage = ImageUtility.GetImageSourceFromStream(new System.IO.MemoryStream(image));
            }
            return resultImage;
        }

        internal override IQueryFactory<IAlbumFromDB> GetQueryFactoryForOrdering(Ordering ordering)
        {
            QueryFactoryForAll<IAlbumFromDB> result = null;
            switch (ordering)
            {

                case Ordering.AritstNameAndReleaseYear:
                    result = new QueryFactoryForAlbumOrderByArtistNameAndReleaseYear();
                    break;

                case Ordering.ArtistNameAndAlbumName:
                    result = new QueryFactoryForAlbumOrderByArtistNameAndAlbumName();
                    break;

                case Ordering.ReleaseYear:
                    result = new QueryFactoryForAlbumOrderByReleaseYear();
                    break;

                case Ordering.ReleaseYearDesc:
                    result = new QueryFactoryForAlbumOrderByReleaseYearDesc();
                    break;

                //case Ordering.AddedDate:
                //    result = new QueryFactoryForAlbumOrderByAddedDate();
                //    break;

                case Ordering.AlbumName:
                    result = new QueryFactoryForAlbumOrderByAlbumTitle();
                    break;

                default:
                    IARLogStatic.Error("AlbumManager", $"Doesn't support this sorting option : {ordering}");
                    break;
            }

            return result;
        }

        public string GetLargeSizeFrontCoverURLByAlbumID(int albumID)
        {
            String query = "select coverFileName, imageType from album_meta m join album_images a on m.album_id = a.album_id "
                + " where a.album_id = ?";

            String coverFileName = this.ExecuteQueryForString(albumID, query);
            String url = ConvertToCoverFilePath(coverFileName);
            return url;
        }

        public string GetLargeSizeBackCoverURLByAlbumID(int albumID)
        {
            String query = "select coverFileName, imageType from album_meta m join album_images a on m.album_id = a.album_id "
               + " where a.album_id = ?";

            String coverFileName = this.ExecuteQueryForString(albumID, query);
            String url = ConvertToCoverFilePath(coverFileName, "-back");
            return url;
        }
    }
}
