using System;
using System.Threading.Tasks;
using Aurender.Core.Utility;

namespace Aurender.Core.Player.mpd
{
    public abstract  class VersionCheckerBase : IVersionChecker, IARLog
    {
        #region IARLog
        private bool LogAll = true;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }
        #endregion

        internal protected VersionCheckerBase(String urlToCheck, int interval)
        {
            this.CurrentVersion = null;
            this.shouldContinue = false;

            this.urlToCheck = urlToCheck;
            this.Interval = interval;
        }

        protected readonly int Interval;
        public String CurrentVersion { get; private set; }

        public bool IsWorking { get => shouldContinue; }
        //protected 
        public event EventHandler<string> OnVersionChecked;

        public void StartCheckPeriodically()
        {
            this.shouldContinue = true;
//            Task.Run(() => this.CheckVersion()).ContinueWith(x => SetTimer());
            this.CheckVersion().ContinueWith(x => SetTimer());
            //this.CheckVersion();
            //SetTimer();

        }

        public void StopChecking()
        {
            shouldContinue = false;
        }

        public void ResumeChecking()
        {
            shouldContinue = true;

            SetTimer();
        }

        public void ResumeCheckingWithImediatCheck()
        {
            if (shouldContinue)
            {
                return;
            }
            shouldContinue = true;

            Task.Run(() =>
            {
                CheckVersion();
                SetTimer();
            });
        }


        protected virtual void SetTimer()
        {
                TimerUtility.SetTimer(Interval, CheckVersion);
            //if (TimerUtility.TimerFunc != null)
            //{
            //    TimerUtility.SetTimer(Interval, CheckVersion);
            //}
            //else
            //{
            //    Device.StartTimer(TimeSpan.FromSeconds(Interval), () =>
            //    {
            //        var result = CheckVersion().ConfigureAwait(false);


            //        return result;
            //    });
            //}
        }

        protected async Task<bool> CheckVersion()
        {

            if (!shouldContinue)
            {
                this.LP("VersionChecker", $"Stop checking for [{urlToCheck}");
                return false;
            }

            var result = await Utility.WebUtil.DownloadContentsAsync(urlToCheck).ConfigureAwait(false);
            //  result.Wait();

            if (result.Item1)
            {
                var content = result.Item2;// result.Result.Item2;

                String version = parseResultForVersion(content);

                this.LP("VersionChecker", $"URL : {urlToCheck}  Version : {version}");

                this.CurrentVersion = version;
                /// Player will call using Task.Run
                OnVersionChecked?.Invoke(this, version);
            }
            else
            {
               // this.EP("Version", $"Failed to check version.");
                /// Player will call using Task.Run
                //OnVersionChecked?.Invoke(this, null);
            }

            return shouldContinue;
        }

        internal protected abstract string parseResultForVersion(string data);

        private String urlToCheck;

        private bool shouldContinue { get; set; } = false;

        public void ClearVersion()
        {
            this.CurrentVersion = string.Empty;
            this.StopChecking();
            this.ResumeCheckingWithImediatCheck();
        }
    }


    public class DBVersionChecker : VersionCheckerBase
    {

        public DBVersionChecker(string url, int interval) : base(url, interval) { }

        protected internal override string parseResultForVersion(string data)
        {
            return data.Trim();
        }
    }

    public class RatingDBVersionChecker : VersionCheckerBase
    {
        public RatingDBVersionChecker(string url, int interval) : base(url, interval) { }

        static readonly System.Text.RegularExpressions.Regex ptn = new System.Text.RegularExpressions.Regex("[0-9.:]*,[0-9]*");
        protected internal override string parseResultForVersion(string data)
        {
            var match = ptn.Match(data);

            if (match != null && match.Success)
            {
                return match.Value;
            }
            return null;
        }

        //protected override void SetTimer()
        //{
        //    TimerUtility.SetTimer(Interval, CheckVersion); 
        //}
    }

    internal class SystemSWVersionChecker : VersionCheckerBase
    {

        internal SystemSWVersionChecker(string url, int interval) : base(url, interval) { }

        static readonly System.Text.RegularExpressions.Regex ptn = new System.Text.RegularExpressions.Regex("auRender_AllIn1-([\\.0-9]+)-[0-9]+\\.aup");

        protected internal override string parseResultForVersion(string data)
        {

            var result = ptn.Match(data);
            if (result != null && result.Success)
            {
                return result.Groups[1].Value;
            }

            return null;
        }
        //protected override void SetTimer()
        //{
        //  //  TimerUtility.SetTimer(Interval, CheckVersion); 
        //}

    }


}
