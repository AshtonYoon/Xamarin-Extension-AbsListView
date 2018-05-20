using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace SQLiteTester
{
    public class SongViewHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; set; }
        public TextView Detail { get; set; }
        public TextView Duration { get; set; }
        public ImageView Cover { get; set; }

        public SongViewHolder(View view) : base(view)
        {
            Title = view.FindViewById<TextView>(Resource.Id.title);
            Detail = view.FindViewById<TextView>(Resource.Id.detail);
            Duration = view.FindViewById<TextView>(Resource.Id.duration);
            Cover = view.FindViewById<ImageView>(Resource.Id.cover);
        }
    }
}