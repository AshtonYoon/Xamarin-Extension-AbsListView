using Aurender.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace ListView
{
    public interface IDBHelper
    {
        void Open();

        IReadOnlyList<ISongFromDB> SongManger { get; }
    }
}
