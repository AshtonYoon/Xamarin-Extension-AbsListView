using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using System.Threading.Tasks;
using Android.Graphics;
using Xamarin.Forms.Platform.Android;
using Android.Content.Res;
using ExtensionListView.Droid.SwipeMenuListView;

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
            
            if (position % 2 == 1)
            {
                int[] attrs = new int[] { Android.Resource.Attribute.SelectableItemBackground };
                TypedArray typedArray = Forms.Context.ObtainStyledAttributes(attrs);
                int backgroundResource = typedArray.GetResourceId(0, 0);
                convertView.SetBackgroundResource(backgroundResource);
                typedArray.Recycle();
            }
            else
            {
                int[] attrs = new int[] { Android.Resource.Attribute.SelectableItemBackgroundBorderless };
                TypedArray typedArray = Forms.Context.ObtainStyledAttributes(attrs);
                int backgroundResource = typedArray.GetResourceId(0, 0);
                convertView.SetBackgroundResource(backgroundResource);
                typedArray.Recycle();
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