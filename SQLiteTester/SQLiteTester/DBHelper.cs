using Android.Content;
using Android.Content.Res;
using Android.Database.Sqlite;
using System.Diagnostics;
using System;
using Android.Arch.Lifecycle;
using Java.IO;
using Android.Util;
using Aurender.Core.Data.DB;
using Aurender.Core.Data.DB.Managers;
using Aurender.Core.Contents;

namespace SQLiteTester
{
    internal class DataBaseHelper : SQLiteOpenHelper
    {
        private static String TAG = "DataBaseHelper"; //Logcat에 출력할 태그이름 
                                                      //디바이스 장치에서 데이터베이스의 경로
        private static String DB_PATH = "";
        private static String DB_NAME = "aurender.db"; // 데이터베이스 이름 
        private SQLiteDatabase mDataBase;
        private AurenderDB aurenderDB;
        private readonly Context mContext;

        public SongManager SongManager { get; set; }

        public DataBaseHelper(Context context) : base(context, DB_NAME, null, 1)
        {
            DB_PATH = "/data/data/" + context.PackageName + "/databases/";

            this.mContext = context;
        }

        public void createDataBase()
        {
            //if not exist then copy from assets

            bool mDataBaseExist = checkDataBase();
            if (!mDataBaseExist)
            {
                var database = this.ReadableDatabase;
                this.Close();
                try
                {
                    //Copy the database from assests
                    copyDataBase();
                    Log.Error(TAG, "createDatabase database created");
                }
                catch (IOException mIOException)
                {
                }
            }
        }

        ///check at data/data/your package/databases/{db name} 
        private bool checkDataBase()
        {
            File dbFile = new File(DB_PATH + DB_NAME);
            //Log.v("dbFile", dbFile + "   "+ dbFile.exists());
            return dbFile.Exists();
        }

        //assets폴더에서 데이터베이스를 복사한다. 
        private void copyDataBase()
        {
            var mInput = mContext.Assets.Open(DB_NAME);
            String outFileName = DB_PATH + DB_NAME;
            OutputStream mOutput = new FileOutputStream(outFileName);
            byte[] mBuffer = new byte[4096];
            int mLength;
            while ((mLength = mInput.Read(mBuffer, 0, mBuffer.Length)) > 0)
            {
                mOutput.Write(mBuffer, 0, mLength);
            }
            mOutput.Flush();
            mOutput.Close();
            mInput.Close();
        }

        public bool openDataBase()
        {
            String mPath = DB_PATH + DB_NAME;
            //Log.v("mPath", mPath);
            aurenderDB = new AurenderDB(mPath, DB_PATH, null, string.Empty, string.Empty);
            aurenderDB.OpenAsync().Wait();

            SongManager = new SongManager(aurenderDB);

            DataFilter filter = new DataFilter();
            this.SongManager.FilterWith(filter);

            var count = SongManager.TotalItemCount;

            //mDataBase = SQLiteDatabase.OpenDatabase(mPath, null, DatabaseOpenFlags.CreateIfNecessary);
            //mDataBase = SQLiteDatabase.openDatabase(mPath, null, SQLiteDatabase.NO_LOCALIZED_COLLATORS);
            return mDataBase != null;
        }

        public override void Close()
        {
            if (mDataBase != null)
                mDataBase.Close();

            base.Close();
        }

        public override void OnCreate(SQLiteDatabase db)
        {
        }

        public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
        }
    }
}