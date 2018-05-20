using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Android.Util;
using System;
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
    }
}