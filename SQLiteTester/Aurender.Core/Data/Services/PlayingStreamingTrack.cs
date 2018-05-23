using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Data.Services
{
    class PlayingStreamingTrack : IStreamingTrack, IPlayingTrack
    {
        IStreamingTrack baseTrack;
        bool isPlaying;

        public PlayingStreamingTrack(IStreamingTrack track)
        {
            baseTrack = track;
        }

        public ContentType ServiceType => baseTrack.ServiceType;

        public string StreamingArtistID => baseTrack.StreamingArtistID;

        public string StreamingAlbumID => baseTrack.StreamingAlbumID;

        public bool AllowStreaming => baseTrack.AllowStreaming;

        public bool IsPremiumOnly => baseTrack.IsPremiumOnly;

        public string ImageUrl => baseTrack.ImageUrl;

        public string ImageUrlForMediumSize => baseTrack.ImageUrlForMediumSize;

        public string ImageUrlForLargeSize => baseTrack.ImageUrlForLargeSize;

        public string ComposerName => baseTrack.ComposerName;

        public string Conductor => baseTrack.Conductor;

        public int DiscIndex => baseTrack.DiscIndex;

        public int TrackIndex => baseTrack.TrackIndex;

        public string Genre => baseTrack.Genre;

        public string Title => baseTrack.Title;

        public string ArtistName => baseTrack.ArtistName;

        public string ItemPath => baseTrack.ItemPath;

        public string AlbumTitle => baseTrack.AlbumTitle;

        public object FrontCover => baseTrack.FrontCover;

        public object BackCover => baseTrack.BackCover;

        public ulong FileSize => baseTrack.FileSize;

        public int Duration => baseTrack.Duration;

        public string ContainerFormat => baseTrack.ContainerFormat;

        public byte BitWidth => baseTrack.BitWidth;

        public int SamplingRate => baseTrack.SamplingRate;

        public int Bitrate => baseTrack.Bitrate;

        public byte Rating => baseTrack.Rating;

        public StreamingItemType StreamingItemType => baseTrack.StreamingItemType;

        public bool IsFavorite => baseTrack.IsFavorite;

        public string StreamingID => baseTrack.StreamingID;

        public bool IsPlaying
        {
            get => isPlaying;
            set
            {
                isPlaying = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Task AddToCache() => baseTrack.AddToCache();

        public IAlbum GetAlbum() => baseTrack.GetAlbum();

        public Credits GetAlbumCredits() => baseTrack.GetAlbumCredits();

        public IArtist GetArtist() => baseTrack.GetArtist();

        public IArtist GetComposer() => baseTrack.GetComposer();

        public IArtist GetConductor() => baseTrack.GetConductor();

        public object GetExtraImage() => baseTrack.GetExtraImage();

        public IList<string> GetLyrics() => baseTrack.GetLyrics();

        public Credits GetSongCredits() => baseTrack.GetSongCredits();

        public Task<string> LoadLyrics() => baseTrack.LoadLyrics();

        public void SetFavorite(bool favorite) => baseTrack.SetFavorite(favorite);
    }
}
