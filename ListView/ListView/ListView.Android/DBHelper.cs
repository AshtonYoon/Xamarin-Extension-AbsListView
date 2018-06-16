using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Aurender.Core;
using ListView.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(DBHelper))]
namespace ListView.Droid
{
    class DBHelper : IDBHelper
    {
        private SQLiteDBHelper helper;

        public IReadOnlyList<ISongFromDB> SongManger => helper.SongManager;

        public void Open()
        {
            helper = new SQLiteDBHelper(Forms.Context);

            helper.createDataBase();
            helper.openDataBase();
        }
    }
}