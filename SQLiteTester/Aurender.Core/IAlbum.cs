using System;
using System.Collections.Generic;

namespace Aurender.Core
{
    public interface IAlbum
    {
        String AlbumTitle { get; }
        String AlbumArtistName { get; }
        IArtist AlbumArtist { get; }

        object FrontCover { get; }
        object BackCover { get; }

        Int16 ReleaseYear { get; }
        String Publisher { get; }

        Int16 NumberOfDisc { get; }
        Int16 TotalSongs { get; }

        Credits AlbumCredit();

        /// <summary>
        /// In case songs are not loaded, it will return null.
        /// </summary>
        /// <value>The songs.</value>
        IList<ISong> Songs { get; }
        void LoadSongs();
    }

    public interface IAlbumFromDB : IAlbum, IDatabaseItem
    {
    }

}