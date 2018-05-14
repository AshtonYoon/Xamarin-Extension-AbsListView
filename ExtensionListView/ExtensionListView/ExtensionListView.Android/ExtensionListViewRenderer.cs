using Xamarin.Forms.Platform.Android;
using ExtensionListView.Droid;

using A = Wahid.SwipemenuListview;
using System.Collections.Generic;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Widget;
using Xamarin.Forms;

[assembly: Xamarin.Forms.ExportRenderer(typeof(ExtensionListView.SwipeMenuListView), typeof(ExtensionListViewRenderer))]
namespace ExtensionListView.Droid
{
    public class ExtensionListViewRenderer : ListViewRenderer
    {
        private A.SwipeMenuListView listView;

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
                        Background = new ColorDrawable(Resources.GetColor(Resource.Color.abc_hint_foreground_material_dark)),
                        IconRes = menu.GetViewType() == 0 ? Resource.Drawable.ic_mr_button_connected_00_light : Resource.Drawable.ic_mr_button_connected_10_light
                    }
                };
            });

            SetNativeControl(listView);
        }
    }
}