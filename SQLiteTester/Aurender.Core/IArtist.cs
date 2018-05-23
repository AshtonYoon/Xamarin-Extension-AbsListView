namespace Aurender.Core
{
    public interface IArtist : ICountable
	{
		string ArtistName { get; }

        object ArtistImage { get; }
    }

    public interface IArtistFromDB : IArtist, IDatabaseItem { }
    public interface IComposerFromDB : IArtistFromDB { }
    public interface IConductorFromDB : IArtistFromDB { }
}