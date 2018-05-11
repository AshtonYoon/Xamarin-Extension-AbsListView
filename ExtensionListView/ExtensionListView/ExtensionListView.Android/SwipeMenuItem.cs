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
using Android.Graphics.Drawables;

namespace ExtensionListView.Droid
{
    class SwipeMenuItem
    {
        private int id;
        private Context mContext;
        private String title;
        private Drawable icon;
        private Drawable background;
        private int titleColor;
        private int titleSize;
        private int width;

        public SwipeMenuItem(Context context)
        {
            mContext = context;
        }

        public int Id
        {
            get => id;
            set => this.id = value;
        }

        public int TitleColor
        {
            get => titleColor;
            set => this.titleColor = value;
        }

        public int TitleSize
        {
            get => titleSize;
            set => this.titleSize = value;
        }

        public String Title
        {
            get => title;
            set => this.title = value;
        }

        public void SetTitle(int resId)
        {
            Title = mContext.GetString(resId);
        }

        public Drawable Icon
        {
            get => icon;
            set => this.icon = value;
        }

        public void SetIcon(int resId)
        {
            icon = mContext.Resources.GetDrawable(resId);
        }

        public Drawable Background
        {
            get => background;
            set => this.background = value;
        }

        public void SetBackground(int resId)
        {
            background = mContext.Resources.GetDrawable(resId);
        }

        public int Width
        {
            get => width;
            set => this.width = value;
        }
    }
}