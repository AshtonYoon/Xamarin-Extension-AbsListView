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
using Java.Lang;
using Wahid.SwipemenuListview;
using Xamarin.Forms;
using System.Threading.Tasks;
using Android.Graphics;
using Xamarin.Forms.Platform.Android;

namespace ExtensionListView.Droid
{
    class SongCellAdapter : BaseSwipeAdapter
    {
        private LayoutInflater inflater;
        private IList<Item> data;
        private int layout;

        public SongCellAdapter(Context context, int layout, IList<Item> data)
        {
            inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            this.data = data;
            this.layout = layout;
        }

        public override int Count => data.Count;

        public override Java.Lang.Object GetItem(int position)
        {
            return data[position];
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Android.Views.View GetView(int position, Android.Views.View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                convertView = inflater.Inflate(layout, parent, false);
            }

            Item item = data[position];
            //ImageView icon = (ImageView)convertView.FindViewById(Resource.Id.imageview);
            //Device.BeginInvokeOnMainThread(async () =>
            //{
            //    var image = await GetBitmap(item.Icon);
            //    icon.SetImageBitmap(image);
            //});

            TextView name = (TextView)convertView.FindViewById(Resource.Id.title);
            name.Text = item.Name;

            TextView detail = (TextView)convertView.FindViewById(Resource.Id.detail);
            detail.Text = item.Detail;

            return convertView;
        }

        private Task<Bitmap> GetBitmap(ImageSource source)
        {
            var handler = new ImageLoaderSourceHandler();
            return handler.LoadImageAsync(source, Forms.Context);
        }
    }
}