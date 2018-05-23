using System.Collections.Generic;
using System.Diagnostics;


namespace Aurender.Core.Data.DB
{

    [DebuggerDisplay("Conductor : {dbID} {ArtistName}")]
    public class Conductor : ArtistBase, IConductorFromDB
    {
        public Conductor() : base()
        {

        }
        public Conductor(IList<object> data) : base(data)
        {

        }

        public override string ToString()
        {
            return $"Conductor : {dbID} - {ArtistName} [{CountOfAlbums}]";
        }


        public override IInformationAvailablity GetAvailability() {

            IInformationAvailablity info = IInformationAvailablity.NONE;

            return info;
        }
    }
}
