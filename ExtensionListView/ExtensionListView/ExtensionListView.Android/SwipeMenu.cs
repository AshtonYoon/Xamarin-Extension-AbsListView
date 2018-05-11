using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace ExtensionListView.Droid
{
    class SwipeMenu
    {
        private Context mContext;
        private IList<SwipeMenuItem> mItems;
        private int mViewType;

        public SwipeMenu(Context context)
        {
            mContext = context;
            mItems = new List<SwipeMenuItem>();
        }

        public Context GetContext()
        {
            return mContext;
        }

        public void AddMenuItem(SwipeMenuItem item)
        {
            mItems.Add(item);
        }

        public void RemoveMenuItem(SwipeMenuItem item)
        {
            mItems.Remove(item);
        }

        public IList<SwipeMenuItem> GetMenuItems()
        {
            return mItems;
        }

        public SwipeMenuItem GetMenuItem(int index)
        {
            return mItems[index];
        }
        
        public int ViewType
        {
            get => mViewType;
            set => mViewType = value;
        }
    }
}