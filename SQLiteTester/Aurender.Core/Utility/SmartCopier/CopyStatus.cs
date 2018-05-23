using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using GalaSoft.MvvmLight;
using System.ComponentModel;
using Aurender.Core.Player;

namespace Aurender.Core.Utility.SmartCopier
{

    public enum CopyStatus
    {
        /// <summary>
        /// Idle
        /// </summary>
        None,
        Initializing,
        Preparing,

        /// <summary>
        /// Not enough space on the target
        /// </summary>
        NotEnoughSpace,

        Prepared,
        Copying,

        Waiting,

        Paused,
        Canceled,

        UnfinishedCopy,

        Postponed,

        Completed,
            
        
    }

}