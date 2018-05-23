using System;
using Newtonsoft.Json.Linq;

namespace Aurender.Core.Utility.SmartCopier
{

    public struct CopyProgress 
    {
        public readonly DateTime UpdatedTime;
        public readonly int Count;
        public readonly CopyingProgresss? ProgressForCopying;
        public readonly PrepartionProgress? ProgressForPreparation;

        public readonly CopyStatus Status;

        public bool IsCopying => (ProgressForCopying != null);
        public bool IsPreparing => (ProgressForPreparation != null);

        public String TaskID
        {
            get
            {
                if (IsCopying)
                {
                    return this.ProgressForCopying.Value.TaskID;
                }
                else if (IsPreparing)
                {
                    return this.ProgressForPreparation.Value.TaskID;
                }

                return string.Empty;
            }
        }

        internal CopyProgress(JToken token)
        {
            UpdatedTime = DateTime.Now;

            this.Status = CopyUtility.CopyStatusFromJSON(token);
            if (token["task_count"] != null)
                this.Count = token["task_count"].Value<int>();
            else
                this.Count = 0;

            JToken progress = token["tasks/progress"];

            if (Status == CopyStatus.Preparing)
            {
                this.ProgressForCopying = null;
                this.ProgressForPreparation = new PrepartionProgress(progress);
            }
            else if (Status == CopyStatus.Copying)
            {
                this.ProgressForCopying = new CopyingProgresss(progress);
                this.ProgressForPreparation = null;
            } 
            else
            {
                this.ProgressForPreparation = null;
                this.ProgressForCopying = null;
            }
        }
    }

}