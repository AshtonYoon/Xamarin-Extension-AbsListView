using Android.Content;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Aurender.Core;
using System;
using System.Collections.Generic;

namespace SQLiteTester
{
    internal class SongAdapter : RecyclerView.Adapter
    {
        private Context context;
        public IReadOnlyList<ISongFromDB> songs;
        
        public SongAdapter(Context context, IReadOnlyList<ISongFromDB> songs)
        {
            this.context = context;
            this.songs = songs;
        }

        public override int ItemCount => songs.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if(holder is SongViewHolder songViewHolder)
            {
                songViewHolder.Title.Text = songs[position].Title;
                songViewHolder.Detail.Text = songs[position].ArtistName;
                songViewHolder.Duration.Text = songs[position].Duration.ToString();

                //songViewHolder.Cover.SetImageBitmap(ReadImageWithSampling(songs[position].FrontCover));
            }

            if (position % 2 == 1)
            {
                holder.ItemView.SetBackgroundColor(Color.ParseColor("#FFFFFF"));
                //  holder.imageView.setBackgroundColor(Color.parseColor("#FFFFFF"));
            }
            else
            {
                holder.ItemView.SetBackgroundColor(Color.ParseColor("#EEEEEE"));
                //  holder.imageView.setBackgroundColor(Color.parseColor("#FFFAF8FD"));
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.song_cell, parent, false);

            SongViewHolder viewHolder = new SongViewHolder(view);
            return viewHolder;
        }

        public static Bitmap ReadImageWithSampling(byte[] array)
        {
            // Get the dimensions of the bitmap
            BitmapFactory.Options bmOptions = new BitmapFactory.Options
            {
                InDither = true,
                InSampleSize = 2
            };

            var image = BitmapFactory.DecodeByteArray(array, 0, array.Length, bmOptions);

            return image;
        }
    }
}