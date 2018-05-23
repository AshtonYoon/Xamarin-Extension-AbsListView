using System;
using System.Collections.Generic;
using Aurender.Core.Contents;

namespace Aurender.Core
{

    public interface ISong : IPlayableItem
	{		
		String ComposerName { get; }
		String Conductor { get; }

		Int32 DiscIndex { get; }
		Int32 TrackIndex { get; }

		IArtist GetArtist();
		IArtist GetComposer();
		IArtist GetConductor();

		IAlbum GetAlbum();

        IList<String> GetLyrics();

        String Genre { get; }
	}

    public interface ISongFromDB : ISong, IRatableDBItem
    {
        int GetGenreID();
        IList<IPlayableItem> GetTracks(ISongManager manager);
    }
}
