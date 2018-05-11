using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace ExtensionListView.Droid
{
    class SwipeMenuAdapter : IWrapperListAdapter, IOnSwipeItemClickListener
    {
        private IListAdapter mAdapter;
        private Context mContext;
        private SwipeMenuListView.IOnMenuItemClickListener onMenuItemClickListener;

        public IListAdapter WrappedAdapter => mAdapter;

        public int Count => mAdapter.Count;

        public bool HasStableIds => mAdapter.HasStableIds;

        public bool IsEmpty => mAdapter.IsEmpty;

        public int ViewTypeCount => mAdapter.ViewTypeCount;

        public IntPtr Handle => mAdapter.Handle;

        public SwipeMenuAdapter(Context context, IListAdapter adapter)
        {
            mAdapter = adapter;
            mContext = context;
        }

        public void CreateMenu(SwipeMenu menu)
        {
            // Test Code
            SwipeMenuItem item = new SwipeMenuItem(mContext)
            {
                Title = "Item 1",
                Background = new ColorDrawable(Color.Gray),
                Width = 300
            };
            menu.AddMenuItem(item);

            item = new SwipeMenuItem(mContext)
            {
                Title = "Item 2",
                Background = new ColorDrawable(Color.Red),
                Width = 300
            };
            menu.AddMenuItem(item);
        }

        public void SetOnSwipeItemClickListener(SwipeMenuListView.IOnMenuItemClickListener onMenuItemClickListener)
        {
            this.onMenuItemClickListener = onMenuItemClickListener;
        }

        public bool AreAllItemsEnabled()
        {
            return mAdapter.AreAllItemsEnabled();
        }

        public bool IsEnabled(int position)
        {
            return mAdapter.IsEnabled(position);
        }

        public Java.Lang.Object GetItem(int position)
        {
            return mAdapter.GetItem(position);
        }

        public long GetItemId(int position)
        {
            return mAdapter.GetItemId(position);
        }

        public int GetItemViewType(int position)
        {
            return mAdapter.GetItemViewType(position);
        }

        public View GetView(int position, View convertView, ViewGroup parent)
        {
            SwipeMenuLayout layout = null;
            if (convertView == null)
            {
                View contentView = mAdapter.GetView(position, convertView, parent);
                SwipeMenu menu = new SwipeMenu(mContext)
                {
                    ViewType = GetItemViewType(position)
                };
                CreateMenu(menu);
                SwipeMenuView menuView = new SwipeMenuView(menu,
                        (SwipeMenuListView)parent);
                menuView.SetOnSwipeItemClickListener(this);
                SwipeMenuListView listView = (SwipeMenuListView)parent;
                layout = new SwipeMenuLayout(contentView, menuView,
                        listView.CloseInterpolator,
                        listView.OpenInterpolator);
                layout.Position = position;
            }
            else
            {
                layout = (SwipeMenuLayout)convertView;
                layout.CloseMenu();
                layout.Position = position;
                View view = mAdapter.GetView(position, layout.getContentView(),
                        parent);
            }
            if (mAdapter instanceof BaseSwipListAdapter) {
                bool swipeEnable = (((BaseSwipListAdapter)mAdapter).getSwipEnableByPosition(position));
                layout.setSwipEnable(swipeEnable);
            }
            return layout;
        }

        public void RegisterDataSetObserver(DataSetObserver observer)
        {
            mAdapter.RegisterDataSetObserver(observer);
        }

        public void UnregisterDataSetObserver(DataSetObserver observer)
        {
            mAdapter.UnregisterDataSetObserver(observer);
        }

        public void Dispose()
        {

        }

        public void OnItemClick(SwipeMenuView view, SwipeMenu menu, int index)
        {
            if (onMenuItemClickListener != null)
            {
                onMenuItemClickListener.OnMenuItemClick(view.Position, menu, index);
            }
        }
    }
}