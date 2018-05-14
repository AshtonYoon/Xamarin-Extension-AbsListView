using Xamarin.Forms;

namespace ExtensionListView.Droid
{
    internal class Item : Java.Lang.Object
    {
        public ImageSource Icon { get; internal set; }
        public string Name { get; internal set; }
        public string Detail { get; internal set; }
    }
}