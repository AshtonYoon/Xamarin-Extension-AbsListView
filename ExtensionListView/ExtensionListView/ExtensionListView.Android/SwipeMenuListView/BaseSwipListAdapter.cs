
using Android.Widget;

namespace ExtensionListView.Droid.SwipeMenuListView
{
    public abstract class BaseSwipeAdapter : BaseAdapter
    {
        public bool GetSwipEnableByPosition(int position)
        {
            return true;
        }
    }
}