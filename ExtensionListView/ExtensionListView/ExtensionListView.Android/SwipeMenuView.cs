using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using static Android.Views.View;
using Android.Text;

namespace ExtensionListView.Droid
{
    class SwipeMenuView : LinearLayout, IOnClickListener
    {
        private SwipeMenuListView mListView;
        private SwipeMenuLayout mLayout;
        private SwipeMenu mMenu;
        private IOnSwipeItemClickListener onItemClickListener;
        private int position;

        public SwipeMenuView(Context context) : base(context)
        {
        }

        public SwipeMenuView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public SwipeMenuView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public SwipeMenuView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected SwipeMenuView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public int Position
        {
            get => position;
            set => position = value;
        }

        public SwipeMenuView(SwipeMenu menu, SwipeMenuListView listView) :  base(menu.GetContext())
        {
            mListView = listView;
            mMenu = menu;

            IList<SwipeMenuItem> items = menu.GetMenuItems();

            int id = 0;
            foreach(var item in items)
            {
                AddItem(item, id++);
            }
        }

        private void AddItem(SwipeMenuItem item, int id)
        {
            LayoutParams layoutParams = new LayoutParams(item.Width, ViewGroup.LayoutParams.MatchParent);
            LinearLayout parent = new LinearLayout(Context)
            {
                Id = id
            };
            parent.SetGravity(GravityFlags.Center);
            parent.Orientation = Orientation.Vertical;
            parent.LayoutParameters = layoutParams;
            parent.SetBackgroundDrawable(item.Background);
            parent.SetOnClickListener(this);

            AddView(parent);

            if (item.Icon!= null)
            {
                parent.AddView(CreateIcon(item));
            }
            if (!TextUtils.IsEmpty(item.Title))
            {
                parent.AddView(CreateTitle(item));
            }
        }

        private ImageView CreateIcon(SwipeMenuItem item)
        {
            ImageView imageView = new ImageView(Context);
            imageView.SetImageDrawable(item.Icon);

            return imageView;
        }

        private TextView CreateTitle(SwipeMenuItem item)
        {
            TextView tv = new TextView(Context);
            tv.Text = item.Title;
            tv.Gravity = GravityFlags.Center;
            tv.TextSize = item.TitleSize;
            tv.SetTextColor(Resources.GetColorStateList(Resource.Color.abc_primary_text_material_light));
            return tv;
        }

        public IOnSwipeItemClickListener GetOnSwipeItemClickListener()
        {
            return onItemClickListener;
        }

        public void SetOnSwipeItemClickListener(IOnSwipeItemClickListener onItemClickListener)
        {
            this.onItemClickListener = onItemClickListener;
        }

        public void setLayout(SwipeMenuLayout mLayout)
        {
            this.mLayout = mLayout;
        }

        public void OnClick(View v)
        {
            if (onItemClickListener != null && mLayout.IsOpen())
            {
                onItemClickListener.OnItemClick(this, mMenu, v.Id);
            }
        }

        public interface IOnSwipeItemClickListener
        {
            void OnItemClick(SwipeMenuView view, SwipeMenu menu, int index);
        }
    }
}