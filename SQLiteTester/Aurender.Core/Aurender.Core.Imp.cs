using System;
using System.Collections.Generic;
using System.IO;

using Aurender.Core.Contents;
using Aurender.Core.Contents.Streaming;
using Aurender.Core.Data.DB;
using Aurender.Core.Data.DB.Managers;
using Aurender.Core.Data.Services;
using Aurender.Core.Player;
using Aurender.Core.Player.mpd;
using Aurender.Core.Setting;

namespace Aurender.Core
{

    public class Aurender : AurenderPlayer
    {
        private readonly object mLock = new object();

        internal Aurender(IAurenderEndPoint e) : base(e)
        {
            this.deviceSetting = Setting.UserSetting.Setting.ConfigForAurenderByName(e.Name);
        }

        public override IWindowedDataWatingDelegate waitingPopupDelegate
        {
            get => this._waitingPopupDelegate;

            set
            {
                this._waitingPopupDelegate = value;
                if (this.Database != null)
                {
                    AurenderDB db = this.Database as AurenderDB;

                    db.popupDelegate = value;
                }
            }
        }

        internal AurenderDB db { get => (AurenderDB)this.Database; }

        protected override IPlayableItem GetPlayableItem(string path)
        {
            IPlayableItem playableItem = null;

            ContentType type = StreamingSerivceTypeMethods.ServiceForPrefix(path);
            if (type == ContentType.Local)
            {
                if (this.Database != null && this.Database?.IsOpen() == true)
                {
                    playableItem = this.SongManager.PlayableItemByPath(path);
                }
            }
            else
            {
                var service = ServiceManager.it[type];
                if (service != null)
                {
                    var item = service.GetTrackWithPath(path);
                    if (item != null)
                    {
                        playableItem = new PlayingStreamingTrack(item);
                    }
                }
            }

            return playableItem ?? new PlayableFile(path);
        }

        protected override void OpenDatabase()
        {
            var path = DBFileName();
            this.LP("DB", $"Opendatabase  => {path}");
            String dbVersion = this.deviceSetting.Get(Setting.FieldsForDeviceConfig.DBVersion, "");
            String rateVersion = this.deviceSetting.Get(Setting.FieldsForDeviceConfig.RateDBVersion, "");

            var newDB = new AurenderDB(path, StoragePath(), WebURL, dbVersion, rateVersion);

            if (newDB.OpenAsync().Result)
            {
                this.L($"Opendatabase  => Now open managers");

                this.Database = newDB;
                newDB.OnDBDownloadStarted += NewDB_OnDBDownloadStarted;
                newDB.OnDBDownloadProgress += NewDB_OnDBDownloadProgress;

                newDB.OnDBReOpened += NewDB_OnDBReOpened;

                if (dbVersion != "")
                    this.NewDB_OnDBReOpened(newDB, newDB.DBVersion);

            }
            else
            {
                this.Info("DB", "Failed to open database.");
            }

        }

        private void NewDB_OnDBDownloadStarted(object sender, string e)
        {
            OnDownloadStart(e);
        }

        private void NewDB_OnDBDownloadProgress(object sender, Tuple<string, long, long> e)
        {
            NotifyProgress(e);
        }

        private void NewDB_OnDBReOpened(object sender, string e)
        {
            AurenderDB db = sender as AurenderDB;

            if (db == null)
            {
                throw new InvalidDataException("Sender must be AurenderDB");
            }

            DataFilter filter = null;
            if (this.SongManager != null)
            {
                filter = this.SongManager.Filter;
            }
            bool dbChanged = false;

            try
            {
                dbChanged = (db.DBVersion != deviceSetting.Get<String>(FieldsForDeviceConfig.DBVersion, ""))
                    || (db.RateVersion != deviceSetting.Get<String>(FieldsForDeviceConfig.RateDBVersion, ""));

                deviceSetting[Setting.FieldsForDeviceConfig.DBVersion] = db.DBVersion;
                deviceSetting[Setting.FieldsForDeviceConfig.RateDBVersion] = db.RateVersion;

                UserSetting.Setting.Save();

                this.SongManager = new SongManager(Database);
                this.AlbumManager = new AlbumManager(this.Database);
                this.ArtistManager = new ArtistManager(this.Database);
                this.ComposerManager = new ComposerManager(this.Database);
                this.ConductorManager = new ConductorManager(this.Database);
                this.GenreManager = new GenreManager(this.Database);

                this.Managers = new IDataManager<IDatabaseItem>[] { SongManager, ArtistManager, AlbumManager, GenreManager, ComposerManager, ConductorManager };

                if (filter == null)
                    filter = new DataFilter();

                this.SongManager.FilterWith(filter);
                this.ArtistManager.FilterWith(filter);
                this.AlbumManager.FilterWith(filter);
                this.GenreManager.FilterWith(filter);
                this.ComposerManager.FilterWith(filter);
                this.ConductorManager.FilterWith(filter);
            }
            catch (Exception ex)
            {
                this.EP("Aurender DB", "Looks like there is a problem on DB", ex);

                this.SongManager = null;
                this.AlbumManager = null;
                this.ArtistManager = null;
                this.ComposerManager = null;
                this.ConductorManager = null;
                this.GenreManager = null;

                this.Managers = null;
            }

            this.CallOnDatabaseOpened();

            if (dbChanged)
            {
                CallOnDatabaseUpdated();
            }
        }
    }


    public static class AurenderInitiator
    {
        public static void InitFactory()
        {
            if (AurenderDB.GetDefaultDBAsStream == null)
            {
                throw new InvalidOperationException("Please set AurenderDB.GetDefaultDBAsStream");
            }

            //   AurenderFactory.Instantiator = (IAurenderEndPoint end) => new Aurender(end);
        }
    }
}
