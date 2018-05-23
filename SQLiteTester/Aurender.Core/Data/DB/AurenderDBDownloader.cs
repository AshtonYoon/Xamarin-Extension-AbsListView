using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aurender.Core.Player;
using Aurender.Core.Player.mpd;
using Aurender.Core.Utility;

namespace Aurender.Core.Data.DB
{
    internal enum DownloadStatus
    {
        Idle,
        Downdloading,
        Downloaded
    }

    public interface IDownloader { 
}
    public abstract class AurenderDBDownloader : IARLog
    {
        public static int INTERVAL_FOR_DB_VERSION_CHECK = 60;
        public static int INTERVAL_FOR_RATEDB_VERSION_CHECK = 60;
        
        #region IARLog
        private bool LogAll = false;
        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }

        #endregion
        private bool _isOpen;
        protected bool isOpen
        {
            get
            {
                return _isOpen;
            }
            set
            {
                _isOpen = value;
            }
        }

        public IVersionChecker DBVersionChecker { get; private set; }

        public IVersionChecker RateVersionChecker { get; private set; }

        public String DBVersion { get; protected set; }
        public String RateVersion { get; protected set; }

        public Func<String, String> WebURL { get; private set; }

        public event EventHandler<Tuple<String, long, long>> OnDBDownloadProgress;
        public event EventHandler<String> OnDBReOpened;
        public event EventHandler<String> OnDBDownloadStarted;

        internal DownloadStatus IsDBDownloading { get; set; } = DownloadStatus.Idle;
        internal DownloadStatus IsRatingDownloading { get; set; } = DownloadStatus.Idle;

        protected String targetPath;
        
        protected AurenderDBDownloader(Func<String, String> urlGetter, String filePath)
        {
            targetPath = filePath;

            this.WebURL = urlGetter;
            if (urlGetter != null)
            {
                var dbCheckURL = WebURL("/aurender/db_version");
                var rateCheckURL = WebURL("/aurender/rate_version");

                this.DBVersionChecker = new DBVersionChecker(dbCheckURL, INTERVAL_FOR_DB_VERSION_CHECK);
                this.RateVersionChecker = new RatingDBVersionChecker(rateCheckURL, INTERVAL_FOR_RATEDB_VERSION_CHECK);

                this.DBVersionChecker.OnVersionChecked += DBVersionChecker_OnVersionChecked;
                this.RateVersionChecker.OnVersionChecked += RateVersionChecker_OnVersionChecked;

            }
            else
            {
                this.EP("Aurender DB Downaloder", "You haven't set urlGetter, so db dowload will not work at all.");
            }

            var oldFile = Path.Combine(filePath, "aurender.db.new");
            if (File.Exists(oldFile))
            {
                File.Delete(oldFile);
            }
            oldFile = Path.Combine(filePath, "rate.db.new");
            if (File.Exists(oldFile))
            {
                File.Delete(oldFile);
            }
        }
        
        private void RateVersionChecker_OnVersionChecked(object sender, string newVersion)
        {
            if (!isOpen || newVersion == null)
            {
                this.LP("Download Rate", $"DB isn't open yet. [{newVersion}]");
                return;
            }
            if (newVersion == this.RateVersion)
            {
                this.LP("Download Rate", $"Has same version already. [{newVersion}]");

           /*     if (this.IsDBDownloading == DownloadStatus.Downloaded)
                {
                    this.LP("Download Rate", "We still need to update DB since it has new DB");
                    ReOpenDatabaseAsync();
                }*/

                return;
            }
            if (this.IsDBDownloading == DownloadStatus.Downdloading)
            {
                this.LP("Download Rate", "DB is being downloaded, so we skip, since it will be downlaoded after finish DB is done.");
                return;
            }


            this.LP("Download Rate", $"Found new rate version : {newVersion}");
            this.IsRatingDownloading = DownloadStatus.Downdloading;

            DownloadRate();

        }

        private void DownloadRate()
        {
            Debug.Assert(IsRatingDownloading == DownloadStatus.Downdloading, "[Rate DB] Wrong condition");

            var urlForDB = WebURL("/aurender/rate.db");
            var targetFilePath = Path.Combine(targetPath, "rate.db.new");

            Action<long, long> reporter = delegate (long total, long read)
            {
                int progress = 0;
                if (total > 0)
                {
                    progress = (int)((read * 100) / total);
                }

                //this.LP("Download Rate", $"Rate Progress {read:n0}/{total:n0} {progress}%");
                if (total == read)
                {
                    this.IsRatingDownloading = DownloadStatus.Downloaded;
                }
            };

            void updateDatabase(Task<bool> dbDownloaded)
            {
                this.LP("Download Rate", $"Download finished.");
                if (dbDownloaded.IsFaulted)
                {
                    this.IsRatingDownloading = DownloadStatus.Idle;

                    File.Delete(targetFilePath);

                    ResumeCheckers();

                    this.EP("Download Rate", "Faeild to download DB", dbDownloaded.Exception);
                    return;
                }
                if (dbDownloaded.Result)
                {
                    this.LP("Download Rate", $"Download was successful. {this.IsRatingDownloading}");
                    ReOpenDatabaseAsync();

                }
                else
                {
                    this.IsRatingDownloading = DownloadStatus.Idle;
                    File.Delete(targetFilePath);

                    this.EP("Download Rate", "Faeild to download DB");

                    ResumeCheckers();
                }
            };
            
            updateDatabase(FileDownloader.CreateDownloadTask(urlForDB, targetFilePath, reporter));
        }

        private  void DBVersionChecker_OnVersionChecked(object sender, string newVersion)
        {
            if (!isOpen || newVersion == null)
            {
                this.LP("Download DB", $"DB isn't open yet. [{newVersion}]");
                return;
            }
            if (newVersion == this.DBVersion)
            {
                this.LP("Download DB", $"Has same version already. [{newVersion}]");
                return;
            }
            if (this.IsDBDownloading != DownloadStatus.Idle)
            {
                this.LP("Download DB", $"Already downloading. [{newVersion}]");
                return;
            }
            try
            {
                IsDBDownloading = DownloadStatus.Downdloading;
                StopChecking();

                this.OnDBDownloadStarted?.Invoke(this, newVersion);
                this.LP("Download DB", $"Found new version : {newVersion}");

                var urlForDB = WebURL("/aurender/aurender.db");

                var targetFolder = FileSystemUtility.GetDataFolderPath();
                var targetFilePath = $"{targetPath}/aurender.db.new";


                Action<long, long> reporter = delegate (long total, long read)
                {
                    var tuple = new Tuple<String, long, long>(newVersion, total, read);

                    Task.Run(() =>
                    {
                        OnDBDownloadProgress?.Invoke(this, tuple);
                    }).ContinueWith(task =>
                    {
                        task.Exception.Handle(ex =>
                        {
                            IARLogStatic.Error("Exception in Event", "For Aurender.OnDBDownloadProgress.", ex);
                            return true;
                        });
                    }, TaskContinuationOptions.OnlyOnFaulted);//..ConfigureAwait(false);

                    if (read == total)
                    {
                        this.IsDBDownloading = DownloadStatus.Downloaded;
                    }
                };

                FileDownloader.CreateDownloadTask(urlForDB, targetFilePath, reporter).ContinueWith(DownloadRate);
            }
            catch (Exception e)
            {
                ResumeCheckers();          
                throw e;
            }
        }

        private void DownloadRate(Task<bool> dbDownloaded)
        {
            this.LP("Download DB", $"Download finished.");
            if (dbDownloaded.IsFaulted)
            {
                this.IsDBDownloading = DownloadStatus.Idle;
                ResumeCheckers();
                this.EP("Download DB", "Faeild to download DB", dbDownloaded.Exception);
                return;
            }
            
            if (dbDownloaded.Result)
            {
                this.LP("Download DB", $"Download was succesul. { this.IsDBDownloading}");

                Debug.Assert(IsRatingDownloading != DownloadStatus.Downdloading, "[Download DB] Wrong condition");

                this.IsRatingDownloading = DownloadStatus.Downdloading;
                DownloadRate();
            }
            else
            {
                this.IsDBDownloading = DownloadStatus.Idle;
                this.EP("Download DB", "Faeild to download DB");
                this.ResumeCheckers();
            }

        }

        public void CheckUpgradePeriodically()
        {
            this.LP("DB Checkers", "Start check periodically");
            this.DBVersionChecker?.StartCheckPeriodically();
            this.RateVersionChecker?.StartCheckPeriodically();
        }

        public void StopChecking()
        {
            this.LP("DB Checkers", "Pause checking");
            this.RateVersionChecker?.StopChecking();
            this.DBVersionChecker?.StopChecking();
        }

        protected void ResumeCheckers()
        {
            this.LP("DB Checkers", "Resume checking");
            this.RateVersionChecker?.ResumeChecking();
            this.DBVersionChecker?.ResumeChecking();
        }
        
        protected async Task<Boolean> MoveDatabaseAsync(String source,  String target)
        {
            this.LP("Move DB", $"Now trying to move {source} to {target}");

            string sourceFilePath = Path.Combine(targetPath, source);
            string targetFilePath = Path.Combine(targetPath, target);

            if (File.Exists(sourceFilePath))
            {
                bool copySuccessful = true;

                try
                {
                    string fileName = Path.GetFileName(source);
                    IARLogStatic.Info("Move", $"Open file    +++++++++++++++++++++ [{fileName}]");

                    byte[] buffer = new byte[1048576];
                    int offset = 0;

                    using (var reader = File.OpenRead(sourceFilePath))
                    using (var writer = File.OpenWrite(targetFilePath))
                    {
                        Task<int> readResult;
                        do
                        {
                            readResult = reader.ReadAsync(buffer, 0, buffer.Length);
                    
                            readResult.Wait();
                    
                            if (readResult.IsFaulted)
                            {
                                writer.Flush();
                                copySuccessful = false;
                                break;
                            }
                            //                            IARLogStatic.Info("Move", $"   Read => {readResult.Result}]");
                    
                            if (readResult.Result != 0)
                            {
                                await writer.WriteAsync(buffer, 0, readResult.Result);
                            }
                            offset += readResult.Result;
                    
                        } while (readResult.Result != 0);

                        writer.Flush();
                    
                        this.Info("[DB Move]", $"Write to {targetFilePath} to {offset:n0}");
                    }
                    IARLogStatic.Info("Move", $"Open file    ---------------------- [{fileName}]");

                    if (!copySuccessful)
                    {
                        this.EP("DB", $"Failed to move {source}");
                    }

                    return copySuccessful;
                }
                catch (IOException ex)
                {
                    this.EP("DB", $"Failed to move {source}", ex);
                }
            }
            else
            {
                this.EP("DB", $"File doesn't exist. {source}");
            }

            return false;
        }

        protected abstract Task ReOpenDatabaseAsync();
        protected void CallOnDBReopen()
        {
            OnDBReOpened?.Invoke(this, this.DBVersion);
        }
    }

}