using System;

namespace Aurender.Core
{
    public interface IPlayableItem
	{
		String Title { get; }
		String ArtistName { get; }
		String ItemPath { get; }
		String AlbumTitle { get; }

		/// <summary>
		/// If there is no cover, it will return default image.
		/// </summary>
		/// <value>The front cover.</value>
		object FrontCover { get; }

        /// <summary>
        /// Return might be null in case of there is no cover
        /// </summary>
        /// <value>The back cover.</value>
        object BackCover { get; }

		Credits GetSongCredits();
		Credits GetAlbumCredits();

		UInt64 FileSize { get; }

        Int32 Duration { get; }

        String ContainerFormat { get; }
		Byte BitWidth {get;}
		Int32 SamplingRate { get; }
		Int32 Bitrate { get; }

        Byte Rating { get; }

        ContentType ServiceType {get;} 
	}

    public interface IRatableDBItem : IDatabaseItem
    {
        byte Rating { get; set; }
        void UpdateRating(int rate);
    }
}
