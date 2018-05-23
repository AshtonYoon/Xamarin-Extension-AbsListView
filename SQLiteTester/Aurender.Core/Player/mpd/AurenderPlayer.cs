using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aurender.Core.Contents;
using Aurender.Core.Player.VolumeController;
using Aurender.Core.Setting;
using Aurender.Core.Utility;

namespace Aurender.Core.Player.mpd
{
    [DebuggerDisplay("Aurender : {ConnectionInfo} DBVersion:{DBVersion} ")]
    public abstract class AurenderPlayer : IAurender, IARLog
    {

        private const int INTERVAL_FOR_SYSTEM_VERSION_CHECK = 600000;

        public static Func<String, String> PathConverter;



        public string Name { get => this.ConnectionInfo.Name; }
        private MPDStatusMonitor statusMonitor;
        public String MAC { get; private set; }

        #region IAurender
        public event EventHandler OnConnect;
        public event EventHandler AfterDisconnect;

        public event EventHandler<Tuple<byte, int>> RatingUpdated;

        public event EventHandler<bool> OnDatabaseOpened;
        public event EventHandler<string> OnDatabaseUpdated;
        public event EventHandler<string> OnFindNewSystemSW;
        public event EventHandler<string> OnDBDownloadStarted;

        public event EventHandler<Dictionary<string, string>> OnNewMessage;
        public event EventHandler<string> AfterDatabaseUpdated;

        public event EventHandler<Tuple<String, long, long>> OnDBDownloadProgress;
        public event EventHandler<IVolumeController> OnControllerableAmpDetected;

        public IAurenderStatus Status { get => this.statusMonitor?.Status; }

        public IPlayerController Controller { get; private set; }
        public IVolumeController VolumeController { get; private set; }

        public IAurenderEndPoint ConnectionInfo { get; private set; }

        public string DBVersion { get => this.Database?.DBVersion ?? ""; }

        public void DownloadDBAgain()
        {
            this.Database?.ResetDBVersion();
        }

        public string RateVersion { get => this.Database?.RateVersion ?? ""; }

        //        public IVersionChecker DBVersionChecker { get; private set; }

        //public IVersionChecker RateVersionChecker { get; private set; }

        public IVersionChecker SystemSoftwareChecker { get; private set; }

        public int IntegerSystemSoftwareVersion { get; private set; }

        public string SystemSoftwareVersion { get; private set; }

        public ConfigForDevice Settings => deviceSetting;
        protected Setting.ConfigForDevice deviceSetting;

        public IDB Database { get; protected set; }
        public bool IsARLogEnabled { get => false; set => throw new NotImplementedException(); }

        public IDataManager<IDatabaseItem>[] Managers { get; protected set; }

        public IAlbumManager AlbumManager { get; protected set; }
        public IArtistManager ArtistManager { get; protected set; }
        public IConductorManager ConductorManager { get; protected set; }
        public IComposerManager ComposerManager { get; protected set; }
        public IGenreManager GenreManager { get; protected set; }
        public ISongManager SongManager { get; protected set; }

        public abstract IWindowedDataWatingDelegate waitingPopupDelegate { get; set; }

        public void Connect()
        {
            CheckConditionForConnect();
            PrepareStatus();
            if (!statusMonitor.IsConnected)
            {
                statusMonitor.ConnectAsync().Wait();

                /* if (statusMonitor.IsConnected)
                 {
                     Task.Run(async () => await this.CheckNamdAndHWAddr());
                 }*/

                //this.Controller = new MPDController(this.ConnectionInfo, statusMonitor.Status, GetPlaybleItem);
                //this.statusMonitor.SetQueue(this.Controller.Queue);

                /*Task.Run(() => OnConnect?.Invoke(this, EventArgs.Empty)).ContinueWith(task =>
                {
                    task.Exception.Handle(ex => {
                        IARLogStatic.Error("Exception in Event", "For Aurender.OnConnect.", ex);
                        return true;
                    });

                }, TaskContinuationOptions.OnlyOnFaulted);
                */
            }
        }


        public void Disconnect()
        {
            this.Database.Close();
            CleanUpBeforeDisconnect();
            statusMonitor.DisconnectAsync().Wait();
            Task.Run(() => AfterDisconnect?.Invoke(this, EventArgs.Empty)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", "For Aurender.Disconnect.", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public bool IsConnected()
        {
            if (statusMonitor == null)
            {
                return false;
            }

            return statusMonitor.IsConnected;
        }

        public async Task<bool> ConnectAsync()
        {
            CheckConditionForConnect();
            PrepareStatus();

            bool connected = false;
            try
            {
                Controller = new MPDController(ConnectionInfo, statusMonitor.Status, GetPlayableItem);

                //TODO: handling connection refused
                connected = await statusMonitor.ConnectAsync().ConfigureAwait(false);
                if (connected)
                {
                    if (statusMonitor.IsConnected)
                    {
                        await this.CheckNamdAndHWAddr().ConfigureAwait(false);
                        this.SystemSoftwareChecker.StartCheckPeriodically();
                        await Task.Run(() => CheckVolumeController()).ConfigureAwait(false);
                    }

                    this.statusMonitor.SetQueue(this.Controller.Queue);

                    Task.Run(() => OnConnect?.Invoke(this, EventArgs.Empty)).ContinueWith(task =>
                    {
                        task.Exception.Handle(ex =>
                        {
                            IARLogStatic.Error("Exception in Event", "For Aurender.ConnectAsync.", ex);
                            return true;
                        });
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (Exception ex)
            {
                IARLogStatic.Error("Aurender", $"Failed to connect : {ex.Message}", ex);
            }

            return connected;
        }

        private void CheckVolumeController()
        {
            var volumeController = this.GetVolumeController();
            this.VolumeController = volumeController;

            this.OnControllerableAmpDetected?.BeginInvoke(this, this.VolumeController, null, null);
        }

        public async Task DisconnectAsync()
        { 
            CleanUpBeforeDisconnect();
            if (statusMonitor != null)
                await statusMonitor.DisconnectAsync().ConfigureAwait(false);
            Task.Run(() => AfterDisconnect?.Invoke(this, EventArgs.Empty)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", "For Aurender.DisconnectAsync.", ex);
                    return true;
                });
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public async Task<String> ShowPasscoderOnAurenderAsync()
        {
            var url = WebURL("/php/register");
            var result = await AurenderRegister.GetPasscode(url).ConfigureAwait(false);

            return result.Passcode;
        }

        public Task HidePasscoder()
        {
            var url = WebURL("/php/register?kpc=1");
            return Task.Run(() => WebUtil.DownloadContentsAsync(url));
        }


        /// <summary>
        /// If returned string is null, it means it i
        /// </summary>
        /// <param name="orgPasscode"></param>
        /// <param name="userPassCode"></param>
        /// <returns>Tuple(bool, string) bool:registered, string:newPasscode in case failed to regiter</returns>
        public async Task<AurenderPassCodeInfo> RegisterRemoteToAurenderAsync(String orgPasscode, String userPassCode)
        {
            var url = WebURL("/php/register");
            var result = await AurenderRegister.RegisterRemoteToAurenderAsync(url, orgPasscode, userPassCode).ConfigureAwait(false);

            if (!result.IsRegistered)
            {
                url = WebURL("/php/register");
                result = await AurenderRegister.GetPasscode(url).ConfigureAwait(false);
            }
            return result;
        }

        #endregion

        protected IWindowedDataWatingDelegate _waitingPopupDelegate { get; set; }

        public IFileBrowser FileBrowser { get; protected set; }
        public IFileBrowser CreateFileBrowser() => new Data.Folder.FileBrowser(this.ConnectionInfo);
        protected abstract void OpenDatabase();

        protected void CallOnDatabaseOpened()
        {
            this.L("Call OnDatabaseOpened");

            Task.Run(() => this.OnDatabaseOpened?.Invoke(this, this.Database.IsOpen())).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", "For Aurender.CallOnDatabaseOpened.", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected void CallOnDatabaseUpdated()
        {
            this.Controller.ReloadPlaylist();
            Task.Run(() => this.OnDatabaseUpdated?.Invoke(this, this.Database.DBVersion)).ContinueWith(task =>
            {
                task.Exception.Handle(ex =>
                {
                    IARLogStatic.Error("Exception in Event", "For Aurender.OnDatabaseUpdated", ex);
                    return true;
                });

            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected String DBFileName(string targetFolder = "")
        {
            String dbFolder = StoragePath(targetFolder);
            var targetFilePath = $"{dbFolder}aurender.db";

            return targetFilePath;
        }

        protected String StoragePath(string targetFolder = "")
        {
            if (targetFolder.Length == 0)
                targetFolder = FileSystemUtility.GetDataFolderPath();

            String targetFilePath;

            if (MAC == null || MAC.Length == 0)
                targetFilePath = $"{targetFolder}/{Name}/";
            else
                targetFilePath = $"{targetFolder}/{MAC.Replace(":", "_")}/";

            if (FileSystemUtility.PathConverter != null)
                targetFilePath = FileSystemUtility.PathConverter(targetFilePath);

            return targetFilePath;
        }

        protected abstract IPlayableItem GetPlayableItem(String path);

        protected String WebURL(String rest)
        {
            return $"http://{ConnectionInfo.IPV4Address}:{ConnectionInfo.WebPort}{rest}";
        }

        protected AurenderPlayer(IAurenderEndPoint endPoint)
        {
            this.ConnectionInfo = endPoint;
            this.FileBrowser = new Data.Folder.FileBrowser(endPoint);

            this.SystemSoftwareChecker = new SystemSWVersionChecker(WebURL("/aurender/upgrade"), INTERVAL_FOR_SYSTEM_VERSION_CHECK);
            //this.SystemSoftwareChecker = new SystemSWVersionChecker(WebURL("/aurender/upgrade"), 10);


            this.OnConnect += AurenderPlayer_OnConnect;
            this.AfterDisconnect += AurenderPlayer_AfterDisconnect;
        }

        private void CleanUpBeforeDisconnect()
        {
            SystemSoftwareChecker.OnVersionChecked -= SystemSoftwareChecker_OnVersionChecked;
            SystemSoftwareChecker.StopChecking();
            if (this.Database != null)
            {
                this.Database.StopChecking();
                this.Database.Close();
            }
        }

        private async void AurenderPlayer_AfterDisconnect(object sender, EventArgs e)
        {
            // save settings
            UserSetting.Setting.Save();
            await DisconnectAsync().ConfigureAwait(false);
        }

        private void PrepareStatus()
        {
            lock (this)
            {
                if (this.statusMonitor == null)
                    statusMonitor = new MPDStatusMonitor(this.ConnectionInfo);
            }
        }

        private async Task CheckNamdAndHWAddr()
        {
            if (this.MAC == null || this.MAC.Length == 0)
            {
                var url = WebURL("/php/hwAddr");
                try
                {
                    var result = await WebUtil.DownloadContentsAsync(url).ConfigureAwait(false);

                    if (result.Item1)
                    {
                        this.MAC = result.Item2.Replace("\n", string.Empty);
                        this.Info("Aurender", $"MAC updated to {this.MAC}");
                    }

                }
                catch (Exception ex)
                {
                    this.E("Failed to get MAC ", ex);
                }
            }

            try
            {
                string newName = await AurenderFactory.GetAurenderName(this.ConnectionInfo.IPV4Address).ConfigureAwait(false);

                if (this.Name != newName)
                {
                    //this.Info("Aurender", $"Name updated to {newName} from : {this.Name}");
                    ((AurenderEndPoint)this.ConnectionInfo).UpdateName(newName);
                }
            }
            catch (Exception ex)
            {
                this.E("Failed to get Aurender name ", ex);
            }
        }


        private void AurenderPlayer_OnConnect(object sender, EventArgs e)
        {
            IARLogStatic.Info("Aurender", $"{Name} is connected.");


            Task.Run(() =>
            {
                OpenDatabase();

                ScheduleVersionCheckers();
            });

            Task.Run(() =>
            {
                this.FileBrowser.MoveToRoot();
                LoadSystemSWVersion();
            });
        }

        private void ScheduleVersionCheckers()
        {
            this.SystemSoftwareChecker.OnVersionChecked += SystemSoftwareChecker_OnVersionChecked;
        }

        private void LoadSystemSWVersion()
        {
            var url = WebURL("/aurender/.aupv");

            var result = Utility.WebUtil.DownloadContentsAsync(url);
            result.Wait();

            if (result.Result.Item1)
            {
                var regex = new Regex("auRender_AllIn1-(.*)-");
                var version = regex.Match(result.Result.Item2);
                this.SystemSoftwareVersion = version.Groups[1].Value;
                this.LP("Aurender", $"{Name}'s SSW Ver is {SystemSoftwareVersion}");
            }
            else
            {
                this.SystemSoftwareVersion = "";
            }
        }


        private void SystemSoftwareChecker_OnVersionChecked(object sender, string e)
        {
            if (e != this.SystemSoftwareVersion)
            {
                SystemSoftwareVersion = e;
                this.LP("Aurender.Version", $"{Name} SSW Version = {e}");

                Task.Run(() => OnFindNewSystemSW?.Invoke(this, e)).ContinueWith(task =>
                {
                    task.Exception.Handle(ex =>
                    {
                        IARLogStatic.Error("Exception in Event", "For Aurender.OnFindNewSystemSW.", ex);
                        return true;
                    });
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public void OnDownloadStart(string e)
        {
            this.OnDBDownloadStarted?.Invoke(this.Database, e);
        }

        protected void NotifyProgress(Tuple<string, long, long> e)
        {
            this.OnDBDownloadProgress?.Invoke(this.Database, e);
        }

        private void CheckConditionForConnect()
        {
            if (FileSystemUtility.GetDataFolderPath == null)
            {
                // test : origin source is app.xaml.cs
                FileSystemUtility.GetDataFolderPath = () => "failed to get path from Aurender forms";
            }

            var task = this.CheckNamdAndHWAddr();
            task.Wait();

            if (this.MAC != null && this.MAC.Length == 17)
            {
                this.deviceSetting = Setting.UserSetting.Setting.ConfigForAurenderMAC(this.MAC);

            }
            else
            {
                this.deviceSetting = Setting.UserSetting.Setting.ConfigForAurenderMAC("00:00:00:00:00:00");
            }
            this.deviceSetting[Setting.FieldsForDeviceConfig.MAC] = this.MAC;
            this.deviceSetting[Setting.FieldsForDeviceConfig.Name] = this.Name;
            this.deviceSetting[Setting.FieldsForDeviceConfig.LastIP] = this.ConnectionInfo.IPV4Address;
            //deviceSetting.Get(LastConnectedList, new List<String>());
            //deviceSetting[LastConnectedList] = this
        }


        private async Task<bool> IsCorrectAurender()
        {
            bool result = false;

            var url = WebURL("/php/hwAddr");

            string macAddress = await AurenderRegister.GetAurenderMACAddress(url).ConfigureAwait(false);
            if (macAddress != null && macAddress.Length > 0)
            {
                //TODO: Needs to check MAC in settings.

                result = true;
            }

            return result;
        }

        public async Task<AurenderPassCodeInfo> GetPasscodeInfo()
        {
            var url = WebURL("/php/register");
            var registInfo = await AurenderRegister.GetPasscode(url).ConfigureAwait(false);

            return (registInfo);
        }

        internal Task UpdateRating(String path, (String pKey, bool isUpdate) updateInfo, int rating)
        {
            String cmd = updateInfo.isUpdate ? "R" : "A";
            String url;
            
            if (updateInfo.isUpdate)
                url = WebURL($"/php/rate?cmd={cmd}&rate={rating}&pork={updateInfo.pKey}");
            else 
                url = WebURL($"/php/rate?cmd={cmd}&rate={rating}&pork={path.URLEncodedString()}");
            return Task.Run(async () =>
            {
                var result = await WebUtil.DownloadContentsAsync(url);

                if (result.Item1)
                {
                    this.LP("[SongRate]", $"Rating for {path} updated properly {result.Item2}");
                }
                else
                {
                    this.EP("[SongRate]", $"Failed to add/update rating for {path}");
                }
            }); 
        }

        public void CallRatingUpdated(Tuple<byte, int> rating)
        {
            this.RatingUpdated?.Invoke(this, rating);
        }

        public async void NotifyDisconnect()
        {
            await DisconnectAsync().ConfigureAwait(false);
        }
    }
}
