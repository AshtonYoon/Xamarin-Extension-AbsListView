using Xamarin.Forms.Platform.Android;
using ExtensionListView.Droid;

using A = Wahid.SwipemenuListview;
using System.Collections.Generic;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Widget;
using Xamarin.Forms;

using AColor = Android.Graphics.Color;
using Android.Content;
using System.Linq;

[assembly: Xamarin.Forms.ExportRenderer(typeof(ExtensionListView.SwipeMenuListView), typeof(ExtensionListViewRenderer))]
namespace ExtensionListView.Droid
{
    public class ExtensionListViewRenderer : ListViewRenderer
    {
        private A.SwipeMenuListView listView;
        public Context CurrentContext => Context;

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.ListView> e)
        {
            base.OnElementChanged(e);

            if (Control == null) return;

            listView = new A.SwipeMenuListView(Context);

            var values = new List<Item>();
            foreach(var element in Enumerable.Range(0, 1000))
            {
                values.Add(new Item
                {
                    Name = "Xamarin 2.2.0",
                    Detail = "Microsoft",
                    Icon = "icon.png"
                });
            }

            listView.Adapter = new SongCellAdapter(Context, Resource.Layout.SongCell, values);

            listView.SetMenuItems(menu =>
            {
                return new List<A.SwipeMenuItem>()
                {
                    new A.SwipeMenuItem(Context)
                    {
                        Width = 200,
                        Background = new ColorDrawable(AColor.Red),
                        Title = "Delete",
                        TitleSize = 14
                    }
                };
            });

            listView.MenuItemClickListener = new OnMenuItemClickListener();
            listView.FastScrollEnabled = true;

            SetNativeControl(listView);
        }
        
        public class OnMenuItemClickListener : A.IOnMenuItemClickListener
        {
            public bool OnMenuItemClick(int position, A.SwipeMenu menu, int index)
            {
                switch (index)
                {
                    case 0:
                        Application.Current.MainPage.DisplayAlert("Title", "Delete button clicked", "OK");
                        break;
                    case 1: 
                        break;
                }
                return false;
            }
        }
    }
}