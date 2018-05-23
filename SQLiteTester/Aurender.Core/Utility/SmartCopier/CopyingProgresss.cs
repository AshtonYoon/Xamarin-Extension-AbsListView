using System;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Utility.SmartCopier
{

    public struct CopyingProgresss
    {
        public readonly String TaskID;
        public readonly Byte Progress;
        public readonly Byte TotalProgress;

        public readonly Int64 SourceSizeInKb;
        public readonly Int64 CopiedSizeInKb;

        public readonly float SpeedInMBPS;
        public readonly Int64 TotalSourceSizeInKb;
        public readonly Int64 TotalCopiedSizeInKb;
        /// <summary>
        /// Sec
        /// </summary>
        public readonly Int64 RemainedTime;

        internal CopyingProgresss(JToken token)
        {
            TaskID = token["id"].Value<String>();

            Progress = token["progress"].Value<byte>();
            TotalProgress = token["total_progress"].Value<byte>();
            SourceSizeInKb = token["total_size_in_kb"].Value<long>();
            CopiedSizeInKb = token["copied_size_in_kb"].Value<long>();

            SpeedInMBPS = token["speed_in_mbps"].Value<float>();

            TotalSourceSizeInKb = token["all_tasks_total_size_in_kb"].Value<long>();
            TotalCopiedSizeInKb = token["all_tasks_copied_size_in_kb"].Value<long>();

            RemainedTime = token["remained_time"].Value<long>();        

        }
    }

}