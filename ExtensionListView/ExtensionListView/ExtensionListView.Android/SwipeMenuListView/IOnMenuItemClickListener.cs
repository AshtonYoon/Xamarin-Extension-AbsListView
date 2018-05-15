namespace ExtensionListView.Droid.SwipeMenuListView
{
    public interface IOnMenuItemClickListener
    {
        bool OnMenuItemClick(int position, SwipeMenu menu, int index);
    }
}