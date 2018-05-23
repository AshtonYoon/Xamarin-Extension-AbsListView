namespace Aurender.Core
{
    public interface IGenre : ICountable
    {
        string Name { get; }

        object GenreImage { get; }
    }

    public interface IGenreFromDB : IGenre, IDatabaseItem
    {

    }
}