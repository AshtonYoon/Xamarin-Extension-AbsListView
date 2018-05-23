using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Aurender.Core.Player;

namespace Aurender.Core.Utility.SmartCopier
{
    public class CopyClient : IARLog
    {
        #region IARLog

        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get => LogAll; set => LogAll = value; }

        #endregion


        const int ServerPort = 13019;
        const String API_Start = "/php/startCopyServer";
        const String API_Stop = "/php/stopCopyServer";
        const String API_Status = "api/1/status";
        const String API_TASKS = "api/1/tasks";

        const String API_STATUS_FIELD = "field";
        const String API_STATUS_PAUSE = "pause";
        const String API_STATUS_RESUME = "resume";
        const String API_STATUS_POSTPONE = "delay_cont_copy";
        const String API_STATUS_CONTINUE = "start_delayed_cont_copy";

        private List<CopyTask> tasks;
        public CopyStatus Status { get; protected set; }

        public CopyProgress? Progress { get; protected set; }

        public event EventHandler<CopyClient> ProgressUpdated;

        private readonly IAurenderEndPoint aurender;

        public CopyClient(IAurender aur)
        {
            this.aurender = aur.ConnectionInfo;
            this.tasks = new List<CopyTask>();
            this.Status = CopyStatus.None;
            this.Progress = null;
        }

        private bool stopCheck;

        public Task StartCheckingProgress(int duration)
        {
            try
            {
                StopCheckingProgress();
            }
            catch(ArgumentNullException e)
            {
                ;
            }

            stopCheck = true;
            TimerUtility.SetTimer(duration, CallUpdateProgress);

            return Task.Run(() => this.LoadTasks());
        }

        public void StopCheckingProgress()
        {
            stopCheck = true;
            TimerUtility.UnsetTimer(CallUpdateProgress);
        }

        private async Task<bool> CallUpdateProgress()
        {
            await this.UpdateProgress();

            return stopCheck;
        }

        public async Task UpdateProgress()
        {
            String url = CopyAPI("api/1/tasks/progress");

            using (var result = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {
                bool statusChanged = false;

                if (result.IsSuccessStatusCode)
                {
                    var str = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

                    JToken token = JsonConvert.DeserializeObject<dynamic>(str);
                    var newProgress = new CopyProgress(token);

                    if (this.Progress.HasValue)
                    {
                        statusChanged = (newProgress.Status != this.Progress.Value.Status)
                            || (newProgress.Count != this.tasks.Count)
                            || (newProgress.TaskID != this.Progress.Value.TaskID);
                    }
                    else
                    {
                        statusChanged = true;
                    }

                    this.Progress = newProgress;
                    this.Status = this.Progress.Value.Status;
                }
                else
                {
                    IARLogStatic.Error("SmartCopy", $"Failed to get progress {(int)(result.StatusCode)}\n\t{result.ReasonPhrase}");
                }

                this.ProgressUpdated?.BeginInvoke(this, this, null, null);

                if (statusChanged)
                {
                    this.LoadTasks();
                }
            }
        }

        private void LoadTasks()
        {
            String url = CopyAPI($"{API_TASKS}");
            using (var task = WebUtil.GetResponseAsync(url))
            {
                task.Wait();
                using (var response = task.Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        String content = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        JToken json = JToken.Parse(content);

                        var tasks = json["tasks"];
                        if (tasks != null && tasks.Type == JTokenType.Array)
                        {

                            var newTasks = new List<CopyTask>();

                            foreach (var t in tasks)
                            {
                                var copyTask = new CopyTask(t);

                                newTasks.Add(copyTask);
                            }

                            this.tasks = newTasks;
                        }
                        else
                        {
                            this.E("Failed to get tasks from result");
                        }
                    }
                    else
                    {
                        this.E("Failed to get tasks so clear tasks ");
                        this.tasks.Clear();
                    }
                }
            }
        }

        private void UpdateStatus(String text)
        {
            JToken json = JToken.Parse(text);
            UpdateTasksWith(json);
            this.Status = CopyUtility.CopyStatusFromJSON(json);
        }


        public async Task<Tuple<bool, String>> AddTask(IList<String> sources, String target)
        {
            bool result = false;
            String errorMessage = string.Empty;
            String updatedTarget;

            updatedTarget = ProcessPath(target);
            IList<string> processedSource = new List<string>();
            foreach (var element in sources)
                processedSource.Add(ProcessPath(element));

            string source = String.Join("\t", processedSource);

            List<KeyValuePair<String, String>> postData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<String, String>("target", updatedTarget),
                new KeyValuePair<string, string>("source", source),
                new KeyValuePair<string, string>("use_all_targets", "0")
            };

            String url = CopyAPI($"{API_TASKS}");
            using (var response = await WebUtil.GetResponseByPostDataAsync(url, postData))
            {
                if (response.IsSuccessStatusCode)
                {
                    String content = await response.Content.ReadAsStringAsync();
                    result = true;
                    UpdateStatus(content);
                }
                else
                {
                    switch ((int)response.StatusCode)
                    {
                        case 507:
                            errorMessage = Resource.GetLocalizedString("NotEnoughSpaceOnTheTarget");
                            break;

                        case 491:
                        case 490:
                            errorMessage = Resource.GetLocalizedString("ThereIsASameCopyTask");
                            break;

                        default:
                            errorMessage = $"Error code : {(int)(response.StatusCode)}";
                            break;
                    }
                }

                return new Tuple<bool, string>(result, errorMessage);
            }
        }

        private static string ProcessPath(string target)
        {
            string updatedTarget;
            if (target.StartsWith("Music"))
            {
                updatedTarget = $"/hdds/{target}";
            }
            else if (target.StartsWith("USB/"))
            {
                updatedTarget = $"/hdds/USB/{target.Substring(4)}";
            }
            else if (target.StartsWith("NAS/"))
            {
                updatedTarget = $"/hdds/SMB/{target.Substring(4)}";

            }
            else if (target.StartsWith("SMB/"))
            {
                updatedTarget = $"/hdds/{target}";
            }
            else
            {
                throw new ArgumentException("Target should starts with Music, USB/, NSA/ or SMB/");
            }

            return updatedTarget;
        }

        public async Task<bool> DeleteTask(String taskID)
        {
            bool result = false;
            string url = CopyAPI($"{API_TASKS}/{taskID}");

            var response = await WebUtil.DeleteDataAndDownloadContentsAsync(url);

            if (response.Item1)
            {
                result = true;
                UpdateStatus(response.Item2);
            }
            else
            {
                IARLogStatic.Error("SmartCopy", $"Failed to delete task [{url}]\n[{response.Item2}]");
            }

            return result;
        }


        public async Task<bool> PauseCopy()
        {
            List<KeyValuePair<String, String>> postData = new List<System.Collections.Generic.KeyValuePair<String, String>>()
            {
                new KeyValuePair<String, String>(API_STATUS_FIELD, API_STATUS_PAUSE)
            };
            return await SendStatusCommand(postData);
        }

        public async Task<bool> ResumeCopy()
        {
            String action = API_STATUS_RESUME;

            if (Status == CopyStatus.Postponed)
                action = API_STATUS_CONTINUE;

            List<KeyValuePair<String, String>> postData = new List<System.Collections.Generic.KeyValuePair<String, String>>()
            {
                new KeyValuePair<String, String>(API_STATUS_FIELD, action)
            };

            bool result = await SendStatusCommand(postData);
            return result;
        }


        public async Task<bool> StartCopyServer()
        {
            bool result = false;
            String url = aurender.WebURLFor(API_Start);
            using (var response = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {

                if (response.IsSuccessStatusCode)
                {
                    String html = await response.Content.ReadAsStringAsync();

                    result = html.Contains("Started copyserver.</pre>") || html.Contains("Already started copyserver.</pre>");
                }

                return result;
            }
        }

        public async Task<bool> StopCopyServer()
        {
            bool result = false;
            String url = aurender.WebURLFor(API_Stop);
            using (var response = await WebUtil.GetResponseAsync(url).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    String html = await response.Content.ReadAsStringAsync();

                    result = html.Contains("Stopped copyserver.</pre>");
                }
            }
            return result;
        }

        private string CopyAPI(String url)
        {
            return $"http://{aurender.IPV4Address}:{ServerPort}/{url}";
        }

        private async Task<bool> SendStatusCommand(List<KeyValuePair<string, string>> postData)
        {
            bool result = false;
            var response = await WebUtil.PostDataAndDownloadContentsAsync(CopyAPI(API_Status), postData);

            if (response.Item1)
            {
                result = true;

                JToken token = JsonConvert.DeserializeObject<dynamic>(response.Item2); 
                this.Status = CopyUtility.CopyStatusFromJSON(token);
            }

            return result;
        }

        private void UpdateTasksWith(JToken json)
        {
            var taks = json["tasks"];

            if (taks != null && taks is JArray)
            {
                List<CopyTask> newTasks = new List<CopyTask>();

                JArray jTasks = taks as JArray;

                foreach(var jTask in jTasks)
                {
                    CopyTask task = new CopyTask(jTask);
                    newTasks.Add(task);
                }
                this.tasks = newTasks;
            }
        }

    }
    internal static class CopyUtility { 

        internal static bool CanDeleted(CopyStatus status)
        {
            bool canDeleted = false;
            switch (status)
            {
                case CopyStatus.NotEnoughSpace:
                case CopyStatus.Paused:
                case CopyStatus.Waiting:
                case CopyStatus.Copying:
                case CopyStatus.Prepared:
                case CopyStatus.UnfinishedCopy:
                case CopyStatus.Postponed:
                    canDeleted = true;
                    break;
            }

            return canDeleted;
        }


        internal static CopyStatus CopyStatusFromJSON(JToken token)
        {
            CopyStatus status = CopyStatus.None;
            String text = token.Value<string>("status");
            if (text == null)
                text = token["tasks"][0].Value<string>("status");

            switch (text.ToLower())
            {
                case "none":
                case "has_delayed_cont_copy":

                    if (token["has_delayed_cont_copy"] == null
                        || token["has_delayed_cont_copy"].ToObject<int>() != 1)
                    {
                        status = CopyStatus.None;
                    }
                    else
                    {
                        status = CopyStatus.Postponed;
                    }
                    break;

                case "preparing":
                    status = CopyStatus.Preparing;
                    break;

                case "not enough space":
                    status = CopyStatus.NotEnoughSpace;
                    break;

                case "prep_completed":
                    status = CopyStatus.Prepared;
                    break;

                case "copying":
                    status = CopyStatus.Copying;
                    break;

                case "paused":
                    status = CopyStatus.Paused;
                    break;

                case "stopped":
                    status = CopyStatus.Canceled;
                    break;

                case "completed":
                    status = CopyStatus.Completed;
                    break;

                case "waiting":
                    status = CopyStatus.Waiting;
                    break;

                case "ready_to_cont_copy":
                    status = CopyStatus.UnfinishedCopy;
                    break;

                case "initializing":
                    status = CopyStatus.Initializing;
                    break;

                default:
                    IARLogStatic.Error("Error", $"Not supported copy status, {text}");
                    status = CopyStatus.None;
                    break;
            }

            return status;
        }

        internal static bool ShouldUpdateStatusForEstimating(CopyStatus status)
        {
            if (status == CopyStatus.Initializing
                || status == CopyStatus.Prepared
                || status == CopyStatus.Preparing)
            {
                return true;
            }
            return false;
        }
    }
}