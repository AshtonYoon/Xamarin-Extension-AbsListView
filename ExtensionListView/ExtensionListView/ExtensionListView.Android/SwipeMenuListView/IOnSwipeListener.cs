namespace ExtensionListView.Droid.SwipeMenuListView
{
    public interface IOnSwipeListener
    {
        void OnSwipeStart(int position);

        void OnSwipeEnd(int position);
    }
}