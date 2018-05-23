using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Aurender.Core.Utility.SmartCopier
{

    [DebuggerDisplay("CopyTask : [{TaskID} : {Status}]")]
    public class CopyTask
    {
        protected String taskID { get; private set; }
        protected List<String> sources { get; private set; }
        public String Target { get; private set; }
        public String Comment { get; private set; }
        public CopyStatus Status { get; private set; }
        

        public bool IsReadyToStart()
        {
            if (sources.Count > 0 && Target.Length > 0)
            {
                return true;
            }
            return false;
        }

        public String TaskID 
        {
            get
            {
                if (taskID.Length > 0)
                {
                    return taskID;
                }
                return "Not registered yet";
            }
        }

        public String Source
        {
            get
            {
                if (sources.Count > 0) {
                    String first = sources[0];

                    if (first.StartsWith("/hdds/"))
                    {
                        first = first.Substring(6);
                    }
                    else if (first.StartsWith("/mnt/smb/"))
                    {
                        first = first.Substring(9);
                    }
                    else if (first.StartsWith("/mnt/usb/") || first.StartsWith("/mnt/USB/"))
                    {
                        first = first.Substring(9);
                    }

                    return first;
                }
                return String.Empty;
            }
        }

        public CopyTask(JToken token)
        {
            String tID = token["id"].ToString();

            this.Status = CopyUtility.CopyStatusFromJSON(token);

            this.Target = token["target"].ToString();

            this.Comment = token["comment"].ToString();
            String tSources = token["source"].ToString();
            this.sources = new List<String>(tSources.Split('\t'));
        }

        public void AddSource(String sourcePath)
        {
            this.sources.Add(sourcePath);
        }

        public override string ToString()
        {
            return $"Copy Task [{TaskID}]\n\tSource : {sources}\n\tTarget : {Target}";
        }

        public bool RemoveSource(String source)
        {
            if (sources.Contains(source))
            {
                return sources.Remove(source);
            }
            return false;
        }


    }

}


