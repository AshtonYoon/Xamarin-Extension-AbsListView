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
using Android.Support.V4.View;
using static Android.Views.GestureDetector;
using Android.Graphics;
using Android.Support.V4.Widget;

namespace ExtensionListView.Droid
{
    class SimepleOnGestureListener : Android.Views.GestureDetector.SimpleOnGestureListener
    {
        private SwipeMenuLayout context;
        public SimepleOnGestureListener(SwipeMenuLayout context)
        {
            this.context = context;
        }

        public override bool OnDown(MotionEvent e)
        {
            context.isFling = false;
            return true;
        }

        public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (Math.Abs(e1.GetX() - e2.GetX()) > context.MIN_FLING && velocityX < context.MAX_VELOCITYX)
            {
                context.isFling = true;
            }

            return base.OnFling(e1, e2, velocityX, velocityY);
        }
    }

    class SwipeMenuLayout : FrameLayout
    {
        private static readonly int CONTENT_VIEW_ID = 1;
        private static readonly int MENU_VIEW_ID = 2;

        private static readonly int STATE_CLOSE = 0;
        private static readonly int STATE_OPEN = 1;

        private int mSwipeDirection;

        private View mContentView;
        private SwipeMenuView mMenuView;
        private int mDownX;
        private int state = STATE_CLOSE;
        private GestureDetectorCompat mGestureDetector;
        private IOnGestureListener mGestureListener;
        public bool isFling;
        public int MIN_FLING;
        public int MAX_VELOCITYX;
        private ScrollerCompat mOpenScroller;
        private ScrollerCompat mCloseScroller;
        private int mBaseX;
        private int position;
        private Android.Views.Animations.IInterpolator mCloseInterpolator;
        private Android.Views.Animations.IInterpolator mOpenInterpolator;


        private bool mSwipEnable = true;
        public SwipeMenuLayout(Context context) : base(context)
        {
            MIN_FLING = Dp2px(15);
            MAX_VELOCITYX = -Dp2px(500);
        }

        public SwipeMenuLayout(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            MIN_FLING = Dp2px(15);
            MAX_VELOCITYX = -Dp2px(500);
        }

        public SwipeMenuLayout(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            MIN_FLING = Dp2px(15);
            MAX_VELOCITYX = -Dp2px(500);
        }

        public SwipeMenuLayout(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            MIN_FLING = Dp2px(15);
            MAX_VELOCITYX = -Dp2px(500);
        }

        protected SwipeMenuLayout(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            MIN_FLING = Dp2px(15);
            MAX_VELOCITYX = -Dp2px(500);
        }


        public int Position
        {
            get => position;
            set
            {
                this.position = value;
                mMenuView.Position = value;
            }
        }

        public void SetSwipeDirection(int swipeDirection)
        {
            mSwipeDirection = swipeDirection;
        }

        private void Init()
        {
            LayoutParameters = (new AbsListView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            mGestureListener = new SimepleOnGestureListener(this);
            mGestureDetector = new GestureDetectorCompat(Context, mGestureListener);

            if (mCloseInterpolator != null)
            {
                mCloseScroller = ScrollerCompat.Create(Context,
                        mCloseInterpolator);
            }
            else
            {
                mCloseScroller = ScrollerCompat.Create(Context);
            }
            if (mOpenInterpolator != null)
            {
                mOpenScroller = ScrollerCompat.Create(Context,
                        mOpenInterpolator);
            }
            else
            {
                mOpenScroller = ScrollerCompat.Create(Context);
            }

            LayoutParams contentParams = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            mContentView.LayoutParameters = contentParams;
            if (mContentView.Id < 1)
            {
                mContentView.Id = CONTENT_VIEW_ID;
            }

            mMenuView.Id = MENU_VIEW_ID;
            mMenuView.LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

            AddView(mContentView);
            AddView(mMenuView);

        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
        }

        public bool OnSwipe(MotionEvent e)
        {
            mGestureDetector.OnTouchEvent(e);
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    mDownX = (int)e.GetX();
                    isFling = false;
                    break;
                case MotionEventActions.Move:
                    // Log.i("byz", "downX = " + mDownX + ", moveX = " + event.getX());
                    int dis = (int)(mDownX - e.GetX());
                    if (state == STATE_OPEN)
                    {
                        dis += mMenuView.Width * mSwipeDirection; ;
                    }
                    Swipe(dis);
                    break;
                case MotionEventActions.Up:
                    if ((isFling || Math.Abs(mDownX - e.GetX()) > (mMenuView.Width / 2)) && Math.Sign(mDownX - e.GetX()) == mSwipeDirection)
                    {
                        // open
                        SmoothOpenMenu();
                    }
                    else
                    {
                        // close
                        SmoothCloseMenu();
                        return false;
                    }
                    break;
            }
            return true;
        }

        public bool IsOpen()
        {
            return state == STATE_OPEN;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            return base.OnTouchEvent(e);
        }

        private void Swipe(int dis)
        {
            if (!mSwipEnable)
            {
                return;
            }
            if (Math.Sign(dis) != mSwipeDirection)
            {
                dis = 0;
            }
            else if (Math.Abs(dis) > mMenuView.Width)
            {
                dis = mMenuView.Width * mSwipeDirection;
            }

            mContentView.Layout(-dis, mContentView.Top, mContentView.Width - dis, MeasuredHeight);

            if (mSwipeDirection == SwipeMenuListView.DIRECTION_LEFT)
            {
                mMenuView.Layout(mContentView.Width - dis, mMenuView.Top,
                        mContentView.Width + mMenuView.Width - dis,
                        mMenuView.Bottom);
            }
            else
            {
                mMenuView.Layout(-mMenuView.Width - dis, mMenuView.Top,
                        -dis, mMenuView.Bottom);
            }
        }

        public override void ComputeScroll()
        {
            if (state == STATE_OPEN)
            {
                if (mOpenScroller.ComputeScrollOffset())
                {
                    Swipe(mOpenScroller.CurrX * mSwipeDirection);
                    PostInvalidate();
                }
            }
            else
            {
                if (mCloseScroller.ComputeScrollOffset())
                {
                    Swipe((mBaseX - mCloseScroller.CurrX) * mSwipeDirection);
                    PostInvalidate();
                }
            }
        }

        public void SmoothCloseMenu()
        {
            state = STATE_CLOSE;
            if (mSwipeDirection == SwipeMenuListView.DIRECTION_LEFT)
            {
                mBaseX = -mContentView.Left;
                mCloseScroller.StartScroll(0, 0, mMenuView.Width, 0, 350);
            }
            else
            {
                mBaseX = mMenuView.Right;
                mCloseScroller.StartScroll(0, 0, mMenuView.Width, 0, 350);
            }
            PostInvalidate();
        }

        public void SmoothOpenMenu()
        {
            if (!mSwipEnable)
            {
                return;
            }
            state = STATE_OPEN;
            if (mSwipeDirection == SwipeMenuListView.DIRECTION_LEFT)
            {
                mOpenScroller.StartScroll(-mContentView.Left, 0, mMenuView.Width, 0, 350);
            }
            else
            {
                mOpenScroller.StartScroll(mContentView.Left, 0, mMenuView.Width, 0, 350);
            }
            PostInvalidate();
        }

        public void CloseMenu()
        {
            if (mCloseScroller.ComputeScrollOffset())
            {
                mCloseScroller.AbortAnimation();
            }
            if (state == STATE_OPEN)
            {
                state = STATE_CLOSE;
                Swipe(0);
            }
        }

        public void openMenu()
        {
            if (!mSwipEnable)
            {
                return;
            }
            if (state == STATE_CLOSE)
            {
                state = STATE_OPEN;
                Swipe(mMenuView.Width * mSwipeDirection);
            }
        }

        public View getContentView()
        {
            return mContentView;
        }

        public SwipeMenuView getMenuView()
        {
            return mMenuView;
        }

        private int Dp2px(int dp)
        {
            return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Context.Resources.DisplayMetrics);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            mMenuView.Measure(MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified), MeasureSpec.MakeMeasureSpec(MeasuredHeight, MeasureSpecMode.Exactly));
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            mContentView.Layout(0, 0, MeasuredWidth, mContentView.MeasuredHeight);
            if (mSwipeDirection == SwipeMenuListView.DIRECTION_LEFT)
            {
                mMenuView.Layout(MeasuredWidth, 0, MeasuredWidth + mMenuView.MeasuredWidth, mContentView.MeasuredHeight);
            }
            else
            {
                mMenuView.Layout(-mMenuView.MeasuredWidth, 0, 0, mContentView.MinimumHeight);
            }
        }

        public void SetMenuHeight(int measuredHeight)
        {
            Log.Info("byz", "pos = " + position + ", height = " + measuredHeight);
            LayoutParams layoutParams = (LayoutParams)mMenuView.LayoutParameters;
            if (layoutParams.Height != measuredHeight)
            {
                layoutParams.Height = measuredHeight;
                mMenuView.LayoutParameters = mMenuView.LayoutParameters;
            }
        }

        public void SetSwipEnable(bool swipEnable)
        {
            mSwipEnable = swipEnable;
        }

        public bool SwipeEnable
        {
            get => mSwipEnable;
            set => mSwipEnable = value;
        }
    }
}