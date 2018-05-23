using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Android.Util;
using Aurender.Core;
using Aurender.Core.Contents;
using Aurender.Core.Data.DB;
using Aurender.Core.Data.DB.Managers;
using Aurender.Core.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SQLiteTester
{
    internal class DBAdapter
    {
        protected static readonly String TAG = "DataAdapter";

        private readonly Context mContext;
        private SQLiteDatabase mDb;
        private DataBaseHelper mDbHelper;

        public DBAdapter(Context context)
        {
            this.mContext = context;
            mDbHelper = new DataBaseHelper(mContext);
        }

        public DBAdapter createDatabase()
        {
            try
            {
                mDbHelper.createDataBase();
            }
            catch (IOException mIOException)
            {
                Log.Error(TAG, mIOException.ToString() + "  UnableToCreateDatabase");
            }
            return this;
        }

        public DBAdapter open()
        {
            try
            {
                mDbHelper.openDataBase();
                mDbHelper.Close();
                mDb = mDbHelper.ReadableDatabase;
            }
            catch (SQLException mSQLException)
            {
                Log.Error(TAG, "open >>" + mSQLException.ToString());
                throw mSQLException;
            }
            return this;
        }

        public void Close()
        {
            mDbHelper.Close();
        }

        public ICursor GetTestData()
        {
            try
            {
                string sql = "SELECT song_title, artistNames, duration, albums.cover_m FROM 'songs' LEFT JOIN albums ON albums.album_id = songs.album_id LIMIT 0, 1000;";

                ICursor mCur = mDb.RawQuery(sql, null);
                if (mCur != null)
                {
                    mCur.MoveToNext();
                }
                return mCur;
            }
            catch (SQLException mSQLException)
            {
                Log.Error(TAG, "getTestData >>" + mSQLException.ToString());
                throw mSQLException;
            }
        }

        public IReadOnlyList<ISongFromDB> GetSongs()
        {
            return mDbHelper.SongManager.GetRange(0, 150);
        }
    }

    //public class AurenderDB : IDB
    //{
    //    public SQLiteDatabase DB { get; set; }

    //    public string DBVersion => string.Empty;

    //    public string RateVersion => string.Empty;

    //    public IList<string> FolderFilters => new List<string>();

    //    public IWindowedDataWatingDelegate popupDelegate => null;

    //    public IVersionChecker DBVersionChecker => throw new NotImplementedException();

    //    public IVersionChecker RateVersionChecker => throw new NotImplementedException();

    //    internal DataBaseHelper Helper { get; set; }

    //    public void Close()
    //    {
    //        Helper.Close();
    //    }

    //    public SQLite.SQLiteConnection CreateConnection()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public SQLite.SQLiteConnection CreateRatingConnection()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool IsOpen()
    //    {
    //        return false;
    //    }

    //    public void ResetDBVersion()
    //    {

    //    }

    //    public void StopChecking()
    //    {

    //    }
    //}
}