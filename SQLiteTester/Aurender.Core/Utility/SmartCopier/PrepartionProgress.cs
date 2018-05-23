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

    public struct PrepartionProgress
    {
        public readonly byte Progress;
        public readonly String CheckedPath;
        public readonly int CurrentIndex;
        public readonly int TotalCount;
        public readonly int StepIndex;
        public readonly String TaskID;

        internal PrepartionProgress(JToken token)
        {
            Progress = token["progress_rate"].Value<byte>();
            CheckedPath = token["checked_path"].Value<String>();
            CurrentIndex = token["current_index"].Value<int>();
            TotalCount = token["total_count"].Value<int>();
            StepIndex = token["step_index"].Value<int>();
            //TaskID = token["task_id"].Value<string>();
            TaskID = string.Empty;
        }
    }

}