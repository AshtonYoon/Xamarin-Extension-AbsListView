using Android.Content;
using ListView.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(ListView.Droid.ListViewRenderer), typeof(Xamarin.Forms.ListView))]
namespace ListView.Droid
{
    public class ListViewRenderer : Xamarin.Forms.Platform.Android.ListViewRenderer
    {
        public ListViewRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.ListView> e)
        {
            base.OnElementChanged(e);

            if (Element == null) return;

            if (Control == null) return;
        }
    }
}