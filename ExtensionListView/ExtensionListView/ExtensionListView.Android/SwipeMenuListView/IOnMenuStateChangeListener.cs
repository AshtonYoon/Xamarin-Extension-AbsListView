namespace ExtensionListView.Droid.SwipeMenuListView
{
    public interface IOnMenuStateChangeListener
    {
        void OnMenuOpen(int position);

        void OnMenuClose(int position);
    }}