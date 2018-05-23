using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Aurender.Core.Contents.Streaming;

namespace Aurender.Core.Contents
{
    /// <summary>
    /// Interface for highlight current track in queue.
    /// </summary>
    public interface IPlayingTrack : INotifyPropertyChanged
    {
        bool IsPlaying { get; set; }
    }

    [DebuggerDisplay("File : {Title}")]
    public class PlayableFile : IPlayableItem, IPlayingTrack
    {
        public static readonly Regex RegexForStreamingPrefix = new Regex("^([a-zA-Z]*)://([0-9]*?)");
        public static readonly Regex RegexForStreamingID = new Regex("^[a-zA-Z]*://([0-9]*)[:]{0,2}");

        private String filePath;

        public PlayableFile(String path)
        {
            filePath = path;
        }

        public string Title
        {
            get
            {
                String result;
                Regex rg = PlayableFile.RegexForStreamingPrefix;

                var matches = rg.Matches(filePath);

                if (matches.Count > 0)
                {
                    result = "Streaming Contents";
                }
                else
                {
                    result = System.IO.Path.GetFileName(filePath);
                }
                return result;
            }
        }
        
        public string ArtistName => "Unknown";

        public string ItemPath => filePath;

        public string AlbumTitle => "Unknown";

        public object FrontCover => null;

        public object BackCover => null;

        public ulong FileSize => 0;

        public int Duration => 0;
        
        public string ContainerFormat 
        {
            get 
            {
                string result;

                if (filePath.Contains("://"))
                {
                    var streamingType = StreamingSerivceTypeMethods.ServiceForPrefix(filePath);

                    result = streamingType.ToString();                        
                }
                else {
                
	                string ext = System.IO.Path.GetExtension(filePath);

	                if (ext != null) 
	                {
                        result = ext.ToUpper();
	                }
	                else 
	                {
	                    result = "";
	                }
                }
                return result;
            }
        }

        public byte BitWidth => 0;

        public int SamplingRate => 0;

        public int Bitrate => 0;

        public byte Rating => 0;

        public ContentType ServiceType => StreamingSerivceTypeMethods.ServiceForPrefix(filePath);

        public event PropertyChangedEventHandler PropertyChanged;
        bool isPlaying;
        public bool IsPlaying
        {
            get => isPlaying; set
            {
                isPlaying = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            }
        }

        public Credits GetAlbumCredits()
        {
            return null;
        }

        public Credits GetSongCredits()
        {
            return null;
        }
    }
}
