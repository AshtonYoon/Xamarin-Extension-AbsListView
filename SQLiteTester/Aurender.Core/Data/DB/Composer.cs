using System.Collections.Generic;
using System.Diagnostics;

namespace Aurender.Core.Data.DB
{
    [DebuggerDisplay("Composer : {dbID} {ArtistName}")]
    public class Composer : ArtistBase, IComposerFromDB
    {
        public Composer() : base()
        {

        }
        public Composer(IList<object> data) : base(data)
        {

        }
        public override string ToString()
        {
            return $"Composer : {dbID} - {ArtistName} [{CountOfSongs}]";
        }

        public override IInformationAvailablity GetAvailability() {

            IInformationAvailablity info = IInformationAvailablity.NONE;

            return info;
        }
    }
}
