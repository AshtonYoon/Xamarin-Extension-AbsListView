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
using Xamarin.Forms;
using static Android.Support.V7.Widget.ActionMenuView;

using Android.Views.Animations;

namespace ExtensionListView.Droid
{
    class SwipeMenuListView : Android.Widget.ListView
    {
        private static readonly int TOUCH_STATE_NONE = 0;
        private static readonly int TOUCH_STATE_X = 1;
        private static readonly int TOUCH_STATE_Y = 2;

        public static readonly int DIRECTION_LEFT = 1;
        public static readonly int DIRECTION_RIGHT = -1;
        private int mDirection = 1;//swipe from right to left by default

        private int MAX_Y = 5;
        private int MAX_X = 3;
        private float mDownX;
        private float mDownY;
        private int mTouchState;
        private int mTouchPosition;
        private SwipeMenuLayout mTouchView;
        private IOnSwipeListener mOnSwipeListener;

        private ISwipeMenuCreator mMenuCreator;
        private IOnMenuItemClickListener mOnMenuItemClickListener;
        private IOnMenuStateChangeListener mOnMenuStateChangeListener;
        private IInterpolator mCloseInterpolator;
        private IInterpolator mOpenInterpolator;

        public SwipeMenuListView(Context context) : base(context)
        {
        }

        public SwipeMenuListView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public SwipeMenuListView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public SwipeMenuListView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected SwipeMenuListView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Init()
        {
            MAX_X = Dp2px(MAX_X);
            MAX_Y = Dp2px(MAX_Y);
            mTouchState = TOUCH_STATE_NONE;
        }

        public override IListAdapter Adapter
        {
            get
            {
                return Adapter;
            }
            set
            {
                var adapter = new SwipeMenuAdapter(Forms.Context, value);
                Adapter = adapter;
            }
        }

        public IInterpolator OpenInterpolator
        {
            get => mOpenInterpolator;
            set => mOpenInterpolator = value;
        }

        public IInterpolator CloseInterpolator
        {
            get => mCloseInterpolator;
            set => mCloseInterpolator = value;
        }

        public override bool OnInterceptTouchEvent(MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    mDownX = e.GetX();
                    mDownY = e.GetY();

                    bool handled = base.OnInterceptTouchEvent(e);

                    mTouchState = TOUCH_STATE_NONE;
                    mTouchPosition = PointToPosition((int)e.GetX(), (int)e.GetY());

                    Android.Views.View view = GetChildAt(mTouchPosition - FirstVisiblePosition);

                    if (view is SwipeMenuLayout)
                    {
                        if (mTouchView != null && mTouchView.IsOpen() && !InRangeOfView(mTouchView.getMenuView(), e))
                        {
                            return true;
                        }
                        mTouchView = (SwipeMenuLayout)view;
                        mTouchView.SetSwipeDirection(mDirection);
                    }

                    if (mTouchView != null && mTouchView.IsOpen() && view != mTouchView)
                    {
                        handled = true;
                    }

                    if (mTouchView != null)
                    {
                        mTouchView.OnSwipe(e);
                    }
                    return handled;
                case MotionEventActions.Move:
                    float dy = Math.Abs((e.GetY() - mDownY));
                    float dx = Math.Abs((e.GetX() - mDownX));
                    if (Math.Abs(dy) > MAX_Y || Math.Abs(dx) > MAX_X)
                    {
                        if (mTouchState == TOUCH_STATE_NONE)
                        {
                            if (Math.Abs(dy) > MAX_Y)
                            {
                                mTouchState = TOUCH_STATE_Y;
                            }
                            else if (dx > MAX_X)
                            {
                                mTouchState = TOUCH_STATE_X;
                                if (mOnSwipeListener != null)
                                {
                                    mOnSwipeListener.OnSwipeStart(mTouchPosition);
                                }
                            }
                        }
                        return true;
                    }
                    return base.OnInterceptTouchEvent(e);
            }
            return base.OnInterceptTouchEvent(e);
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            if (ev.Action != MotionEventActions.Down && mTouchView == null)
                return base.OnTouchEvent(ev);

            var action = ev.Action;
            switch (action)
            {
                case MotionEventActions.Down:
                    int oldPos = mTouchPosition;
                    mDownX = ev.GetX();
                    mDownY = ev.GetY();
                    mTouchState = TOUCH_STATE_NONE;

                    mTouchPosition = PointToPosition((int)ev.GetX(), (int)ev.GetY());

                    if (mTouchPosition == oldPos && mTouchView != null
                            && mTouchView.IsOpen())
                    {
                        mTouchState = TOUCH_STATE_X;
                        mTouchView.OnSwipe(ev);
                        return true;
                    }

                    Android.Views.View view = GetChildAt(mTouchPosition - FirstVisiblePosition);

                    if (mTouchView != null && mTouchView.IsOpen())
                    {
                        mTouchView.SmoothCloseMenu();
                        mTouchView = null;
                        // return super.onTouchEvent(ev);
                        // try to cancel the touch event
                        MotionEvent cancelEvent = MotionEvent.Obtain(ev);
                        cancelEvent.Action = MotionEventActions.Cancel;
                        OnTouchEvent(cancelEvent);
                        if (mOnMenuStateChangeListener != null)
                        {
                            mOnMenuStateChangeListener.OnMenuClose(oldPos);
                        }
                        return true;
                    }
                    if (view is SwipeMenuLayout swipeMenuLayout) {
                        mTouchView = swipeMenuLayout;
                        mTouchView.SetSwipeDirection(mDirection);
                    }
                    if (mTouchView != null)
                    {
                        mTouchView.OnSwipe(ev);
                    }
                    break;
                case MotionEventActions.Move:
                    mTouchPosition = PointToPosition((int)ev.GetX(), (int)ev.GetY()) - HeaderViewsCount;
                    if (!mTouchView.SwipeEnable || mTouchPosition != mTouchView.Position)
                    {
                        break;
                    }

                    float dy = Math.Abs((ev.GetY() - mDownY));
                    float dx = Math.Abs((ev.GetX() - mDownX));

                    if (mTouchState == TOUCH_STATE_X)
                    {
                        if (mTouchView != null)
                        {
                            mTouchView.OnSwipe(ev);
                        }
                        Selector.SetState(new int[] { 0 });
                        ev.Action = MotionEventActions.Cancel;

                        base.OnTouchEvent(ev);
                        return true;
                    }
                    else if (mTouchState == TOUCH_STATE_NONE)
                    {
                        if (Math.Abs(dy) > MAX_Y)
                        {
                            mTouchState = TOUCH_STATE_Y;
                        }
                        else if (dx > MAX_X)
                        {
                            mTouchState = TOUCH_STATE_X;
                            if (mOnSwipeListener != null)
                            {
                                mOnSwipeListener.OnSwipeStart(mTouchPosition);
                            }
                        }
                    }
                    break;
                case MotionEventActions.Up:
                    if (mTouchState == TOUCH_STATE_X)
                    {
                        if (mTouchView != null)
                        {
                            bool isBeforeOpen = mTouchView.IsOpen();
                            mTouchView.OnSwipe(ev);
                            bool isAfterOpen = mTouchView.IsOpen();
                            if (isBeforeOpen != isAfterOpen && mOnMenuStateChangeListener != null)
                            {
                                if (isAfterOpen)
                                {
                                    mOnMenuStateChangeListener.OnMenuOpen(mTouchPosition);
                                }
                                else
                                {
                                    mOnMenuStateChangeListener.OnMenuClose(mTouchPosition);
                                }
                            }
                            if (!isAfterOpen)
                            {
                                mTouchPosition = -1;
                                mTouchView = null;
                            }
                        }
                        if (mOnSwipeListener != null)
                        {
                            mOnSwipeListener.OnSwipeEnd(mTouchPosition);
                        }
                        ev.Action = MotionEventActions.Cancel;
                        base.OnTouchEvent(ev);
                        return true;
                    }
                    break;
            }
            return base.OnTouchEvent(ev);
        }

        public void SmoothOpenMenu(int position)
        {
            if (position >= FirstVisiblePosition
                    && position <= LastVisiblePosition)
            {
                Android.Views.View view = GetChildAt(position - FirstVisiblePosition);
                if (view is SwipeMenuLayout) {
                    mTouchPosition = position;
                    if (mTouchView != null && mTouchView.IsOpen())
                    {
                        mTouchView.SmoothCloseMenu();
                    }
                    mTouchView = (SwipeMenuLayout)view;
                    mTouchView.SetSwipeDirection(mDirection);
                    mTouchView.SmoothOpenMenu();
                }
            }
        }

        public void SmoothCloseMenu()
        {
            if (mTouchView != null && mTouchView.IsOpen())
            {
                mTouchView.SmoothCloseMenu();
            }
        }

        private int Dp2px(int dp)
        {
            return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Context.Resources.DisplayMetrics);
        }

        public void SetMenuCreator(ISwipeMenuCreator menuCreator)
        {
            this.mMenuCreator = menuCreator;
        }

        public void SetOnMenuItemClickListener(IOnMenuItemClickListener onMenuItemClickListener)
        {
            this.mOnMenuItemClickListener = onMenuItemClickListener;
        }

        public void SetOnSwipeListener(IOnSwipeListener onSwipeListener)
        {
            this.mOnSwipeListener = onSwipeListener;
        }

        public void SetOnMenuStateChangeListener(IOnMenuStateChangeListener onMenuStateChangeListener)
        {
            mOnMenuStateChangeListener = onMenuStateChangeListener;
        }

        public interface IOnMenuItemClickListener
        {
            bool OnMenuItemClick(int position, SwipeMenu menu, int index);
        }

        public interface IOnSwipeListener
        {
            void OnSwipeStart(int position);

            void OnSwipeEnd(int position);
        }

        public interface IOnMenuStateChangeListener
        {
            void OnMenuOpen(int position);

            void OnMenuClose(int position);
        }

        public void SetSwipeDirection(int direction)
        {
            mDirection = direction;
        }

        public static bool InRangeOfView(Android.Views.View view, MotionEvent ev)
        {
            int[] location = new int[2];
            view.GetLocationOnScreen(location);

            int x = location[0];
            int y = location[1];

            if (ev.RawX < x || ev.RawX > (x + view.Width) || ev.RawY < y || ev.RawY > (y + view.Height))
            {
                return false;
            }

            return true;
        }
    }
}