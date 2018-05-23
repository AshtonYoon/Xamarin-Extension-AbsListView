﻿using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using System.Collections.Generic;
using System;
using Android.Database;
using Aurender.Core;

namespace SQLiteTester
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        //view
        private RecyclerView recyclerView;
        //layout manager
        private RecyclerView.LayoutManager layoutManager;
        //adapter
        private SongAdapter adapter;
        //dataset
        private IReadOnlyList<ISongFromDB> songs;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            recyclerView.SetItemAnimator(new DefaultItemAnimator());
            
            Retrieve();
        }

        private void Retrieve()
        {
            songs = null;

            DBAdapter dbAdapter = new DBAdapter(ApplicationContext);
            dbAdapter.createDatabase();
            dbAdapter.open();

            songs = dbAdapter.GetSongs();
            adapter = new SongAdapter(this, songs);

            recyclerView.SetAdapter(adapter);
            //LoadByAndroidSQLite(mDbHelper);
        }

        [Obsolete("please use sqlite net std instead")]
        private void LoadByAndroidSQLite(DBAdapter mDbHelper)
        {
            ICursor testdata = mDbHelper.GetTestData();

            if (testdata == null) return;

            //LOOP AND ADD TO ARRAYLIST
            while (testdata.MoveToNext())
            {
                string title = testdata.GetString(0);
                string detail = testdata.GetString(1);
                string duration = testdata.GetString(2);
                var cover = testdata.GetBlob(3);

                Song song = new Song()
                {
                    Title = title,
                    Detail = detail,
                    Duration = duration,
                    Cover = cover
                };

                //ADD TO ARRAYLIS
                //songs.Add(song);
            }

            //CHECK IF ARRAYLIST ISNT EMPTY
            if (!(songs.Count < 1))
            {
                recyclerView.SetAdapter(adapter);
            }

            mDbHelper.Close();
        }
    }
}

