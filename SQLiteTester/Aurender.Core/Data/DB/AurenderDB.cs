using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aurender.Core.Contents;

namespace Aurender.Core.Data.DB
{
    public class AurenderDB : AurenderDBDownloader, IDB
    {
        //internal event EventHandler<bool> DatabseOpened;
        //internal event EventHandler<IDB> DatabseUpdated;

        public static Func<Stream> GetDefaultDBAsStream;

        IWindowedDataWatingDelegate IDB.popupDelegate => this.popupDelegate;

        public IWindowedDataWatingDelegate popupDelegate { get; set; }

        public AurenderDB(String filePath, String dbFolder, Func<String, String> urlGetter, String lastDB, String lastRate) : base(urlGetter, dbFolder)
        {
            this.DBVersion = lastDB;
            this.RateVersion = lastRate;
            this.filePath = filePath;
            isOpen = false;
        }

        Stream GetDefaultDB()
        {
            var assembly = typeof(AurenderDB).GetTypeInfo().Assembly;
            Stream s = assembly.GetManifestResourceStream("Aurender.Core.master.sql");

            return s;
        }

        private async Task PrepareDefaultDBAsync()
        {
            if (File.Exists(filePath))
            {
                this.EP("AurenderDB", $"{filePath} is already exists.");
                return;
            }

            // read data from default db
            byte[] data;
            using (var stream = GetDefaultDB())
            using (var reader = new BinaryReader(stream))
            {
                data = reader.ReadBytes((int)reader.BaseStream.Length);
            }

            // if directory doesn't exist, create directory
            var folderPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // copy data to new file
            using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
        }

        private String _file;
        private String filePath
        {
            get => _file; set
            {
                _file = value;
            }
        }

        public bool IsOpen() => isOpen;

        public IList<string> FolderFilters { get; protected set; }

        public async Task<bool> OpenAsync()
        {
            try
            {
                if (filePath == null)
                {
                    this.isOpen = false;
                    this.EP("DB", "File path for db is not set");
                    return false;
                }

                this.LP("DB", $"Open() -> {filePath}");
                bool result = false;

                if (isOpen)
                {
                    this.LP("DB", "Already open");
                    result = true;
                }
                else
                {
                    result = File.Exists(filePath);

                    if (!result)
                    {
                        this.LP("DB", "No, DB so prepare DB again.");
                        await PrepareDefaultDBAsync();
                        result = true;
                    }
                    this.isOpen = true;
                    LoadFolderFilters();

                    CheckUpgradePeriodically();
                }
                this.isOpen = result;
            }
            catch (Exception ex)
            {
                this.isOpen = false;
                this.EP("DB", "Failed to open DB", ex);
            }

            return this.isOpen;
        }

        public void Close()
        {
            this.filePath = null;
            isOpen = false;
            StopChecking();
            /// Aurender will call BeginInvoke
        }

        public void ResetDBVersion()
        {
            this.DBVersion = String.Empty;
            this.DBVersionChecker.ClearVersion();
        }

        public SQLite.SQLiteConnection CreateConnection()
        {
            if (!isOpen)
            {
                return null;
            }

            return new SQLite.SQLiteConnection(filePath, SQLite.SQLiteOpenFlags.ReadOnly);
        }
        public SQLite.SQLiteConnection CreateRatingConnection()
        {
            if (!isOpen)
            {
                return null;
            }

            return new SQLite.SQLiteConnection($"{this.targetPath}rate.db", SQLite.SQLiteOpenFlags.ReadOnly);
        }
        private SQLite.SQLiteConnection CreateConnectionForSync()
        {
            if (!isOpen)
            {
                return null;
            }

            return new SQLite.SQLiteConnection(this.filePath + ".tmp", SQLite.SQLiteOpenFlags.ReadWrite);
        }

        protected override async Task ReOpenDatabaseAsync()
        {
            StopChecking();
            bool needsToCallOnDatabaseOpened = false;
            try
            {
                this.LP("ReOpenDatabase", "ReOpendatabase enter");

                bool dbUpdated = false;
                bool rateUpdated = false;

                if (this.DBVersion != this.DBVersionChecker.CurrentVersion && this.IsDBDownloading == DownloadStatus.Downloaded)
                {
                    dbUpdated = await MoveDatabaseAsync("aurender.db.new", "aurender.db.tmp").ConfigureAwait(false);

                    if (dbUpdated)
                        this.LP("ReOpenDatabase", "Found changes for aurender.db");
                }

                if (this.RateVersion != this.RateVersionChecker.CurrentVersion && this.IsRatingDownloading == DownloadStatus.Downloaded)
                {
                    rateUpdated = await MoveDatabaseAsync("rate.db.new", "rate.db.tmp").ConfigureAwait(false);

                    if (rateUpdated)
                    {
                        this.LP("ReOpenDatabase", "Found changes for aurender.db");
                        await ApplyRateToDBAsync();
                    }
                }

                if (dbUpdated || rateUpdated)
                {
                    this.LP("ReOpenDatabase", "We need to reopen enter");
                    var tmpFileName = this.filePath;

                    lock (this)
                    {
                        this.Close();
                        var updated = MoveDatabaseAsync("aurender.db.tmp", "aurender.db");
                        updated.Wait();
                    }

                    this.filePath = tmpFileName;

                    await OpenAsync();

                    if (rateUpdated)
                    {
                        this.RateVersion = this.RateVersionChecker.CurrentVersion;
                        this.DBVersion = this.DBVersionChecker.CurrentVersion;
                    }
                    else if (dbUpdated)
                    {
                        this.DBVersion = this.DBVersionChecker.CurrentVersion;
                    }

                    needsToCallOnDatabaseOpened = true;
                }
                else
                {
                    this.LP("ReOpenDatabase", "No changes, so skip to open.");
                }

                this.LP("ReOpenDatabase", $">>>DB      download status = {this.IsDBDownloading}");
                this.LP("ReOpenDatabase", $">>>Rate    download status = {this.IsRatingDownloading}");
                if (this.IsDBDownloading == DownloadStatus.Downloaded)
                    this.IsDBDownloading = DownloadStatus.Idle;
                if (this.IsRatingDownloading == DownloadStatus.Downloaded)
                    this.IsRatingDownloading = DownloadStatus.Idle;

                ResumeCheckers();
            }
            catch (Exception ex)
            {
                this.EP("Download DB", "****************** Err ", ex);
            }

            if (needsToCallOnDatabaseOpened)
            {
                /// Aurender will call BeginInvoke
                CallOnDBReopen();
            }
        }

        SemaphoreSlim mutax = new SemaphoreSlim(1, 1);
        async Task ApplyRateToDBAsync()
        {
            this.LP("ApplyRateToDB", $">>> Start update song rating");

            try
            {
                await mutax.WaitAsync();

                string baseDBName = "aurender.db.new";

                var tempFilePath = Path.Combine(targetPath, "aurender.db.tmp");
                if (!File.Exists(tempFilePath))
                {
                    baseDBName = "aurender.db";
                    await this.MoveDatabaseAsync("aurender.db", "aurender.db.tmp");
                }

                using (SQLite.SQLiteConnection con = CreateConnectionForSync())
                {
                    var filePath = Path.Combine(targetPath, "rate.db.tmp");
                    if (File.Exists(filePath))
                    {
                        this.LP("ApplyRateToDB", $" rate file exists");
                    }
                    try
                    {
                        //Type[] types = new Type[] {
                        //       typeof(String),
                        //       typeof(String),
                        //};

                        //if (false)
                        //{
                        //    var cmd2 = con.CreateCommand("select type, name from sqlite_master where type = 'table';");
                        //    var resultReader = cmd2.ExecuteDeferredQuery(types);
                        //    foreach (var obj in resultReader)
                        //    {
                        //        this.L($"   new db : {obj[0].ToString()} {obj[1].ToString()}");
                        //    }
                        //}
                        var cmd = con.CreateCommand($"attach database '{filePath}' as rateDB;");

                        var result = cmd.ExecuteNonQuery();
                        //if (false)
                        //{
                        //    var cmd2 = con.CreateCommand("select type, name from rateDB.sqlite_master where type = 'table';");
                        //    var resultReader = cmd2.ExecuteDeferredQuery(types);
                        //    foreach (var obj in resultReader)
                        //    {
                        //        this.L($"   new rate db : {obj[0].ToString()} {obj[1].ToString()}");
                        //    }
                        //}

                        cmd = con.CreateCommand("update songs set songRate " +
                            "   = (select rateDB.track_rate.rate from rateDB.track_rate " +
                            "     where songs.song_id = rateDB.track_rate.song_id) " +
                            " where songs.song_id in (select song_id from rateDB.track_rate);");

                        result = cmd.ExecuteNonQuery();

                        this.LP("ApplyRateToDB", $"   {result} tracks updated for rating");


                        cmd = con.CreateCommand("detach database rateDB;");
                        result = cmd.ExecuteNonQuery();
                        con.Close();

                        await this.MoveDatabaseAsync("rate.db.tmp", "rate.db");
                    }
                    catch (Exception ex)
                    {
                        con?.Close();

                        this.E("Update failed for sync rate and db", ex);
                        if (baseDBName.Equals("aurender.db.new"))
                            await this.MoveDatabaseAsync("aurender.db.new", "aurender.db.tmp");
                        else
                            await this.MoveDatabaseAsync("aurender.db", "aurender.db.tmp");
                    }
                }
            }
            finally
            {
                mutax.Release();
            }

            this.LP("ApplyRateToDB", $">>> end update song rating");
        }

        private void LoadFolderFilters()
        {
            var folders = new List<String>(12);
            try
            {
                using (SQLite.SQLiteConnection con = CreateConnection())
                {
                    var cmd = con.CreateCommand("select filter_id, path, filter_order from filters order by filter_order");

                    var et = cmd.ExecuteDeferredQuery<TableMapping.FolderFilterRow>();

                    foreach (var row in et)
                    {
                        folders.Add(row.FolderName);
                    }
                }
            }
            catch (SQLite.SQLiteException ex)
            {
                this.EP("AurenderDB", "Failed to create db connection", ex);
                PrepareDefaultDBAsync().Wait();
            }
            finally
            {
                CleanupFolderFilter(folders);
            }
        }

        private void CleanupFolderFilter(List<string> folders)
        {
            if (folders.Count == 0)
            {
                var names = new string[] { "Classic", "Jazz", "Pop", "Local", "Etc" };

                folders.AddRange(names);
            }
            else if (folders.Count == 5)
            {
                folders.Add("Misc.");
            }
            else if (folders.Count > 6)
            {
                folders.Add("Misc.");
            }

            if (folders.Contains("HasOnlyFiveFolderInRoot"))
            {
                folders.Remove("HasOnlyFiveFolderInRoot");
            }

            this.FolderFilters = folders;
        }


        internal (String pKey, bool isUpdate) UpdateLocalDB(String songPath)
        {
            (String pKey, bool isUpdate) result = (string.Empty, false);

            String query = "select pKey from track_rate where path = ?";

            lock (this)
            {
                using (SQLite.SQLiteConnection con = CreateConnection())
                {
                    var cmd = con.CreateCommand(query, songPath);
                    var e = cmd.ExecuteDeferredQuery<String>().GetEnumerator();
                    if (e.MoveNext())
                    {
                        result.pKey = e.Current;
                        result.isUpdate = true;
                    }

                    this.LP("ApplyRateToDB", $" track updated for rating isUpdate:{result.isUpdate}, key[{result.pKey}]");
                }
            }

            return result;
        }
    }
}
