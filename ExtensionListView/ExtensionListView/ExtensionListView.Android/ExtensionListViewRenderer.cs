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

            string[] values = new string[]
            {
                "Android List View",
                "Adapter implementation",
                "Simple List View In Android",
                "Create List View Android",
                "Android Example",
                "List View Source Code",
                "List View Array Adapter",
                "Android Example List View",
                "Android List View",
                "Adapter implementation",
                "Simple List View In Android",
                "Create List View Android",
                "Android Example",
                "List View Source Code",
                "List View Array Adapter",
                "Android Example List View",
                "Android List View",
                "Adapter implementation",
                "Simple List View In Android",
                "Create List View Android",
                "Android Example",
                "List View Source Code",
                "List View Array Adapter",
                "Android Example List View",
                "Android List View",
                "Adapter implementation",
                "Simple List View In Android",
                "Create List View Android",
                "Android Example",
                "List View Source Code",
                "List View Array Adapter",
                "Android Example List View",
                "Android List View",
                "Adapter implementation",
                "Simple List View In Android",
                "Create List View Android",
                "Android Example",
                "List View Source Code",
                "List View Array Adapter",
                "Android Example List View",
                "Android List View",
                "Adapter implementation",
                "Simple List View In Android",
                "Create List View Android",
                "Android Example",
                "List View Source Code",
                "List View Array Adapter",
                "Android Example List View"
            };

            listView.Adapter = new ArrayAdapter<string>(Context, Resource.Layout.simplerow, values);

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

            SetNativeControl(listView);
        }
        
        public class OnMenuItemClickListener : A.IOnMenuItemClickListener
        {
            public bool OnMenuItemClick(int position, A.SwipeMenu menu, int index)
            {
                switch (index)
                {
                    case 0:
                        App.Current.MainPage.DisplayAlert("Title", "Delete button clicked", "OK");
                        break;
                    case 1: 
                        break;
                }
                return false;
            }
        }
    }
}