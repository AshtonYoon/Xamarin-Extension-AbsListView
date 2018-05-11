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
using Xamarin.Forms.Platform.Android;
using Android.Graphics;
using Xamarin.Forms;

namespace ExtensionListView.Droid
{
    class ExtensionListViewRenderer : ListViewRenderer
    {
        public ExtensionListViewRenderer()
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.ListView> e)
        {
            base.OnElementChanged(e);

            if (Control == null) return;
        }
    }
}