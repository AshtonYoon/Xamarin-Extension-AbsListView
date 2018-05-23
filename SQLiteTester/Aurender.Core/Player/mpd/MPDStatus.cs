using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using GalaSoft.MvvmLight;

namespace Aurender.Core.Player.mpd
{
    [DebuggerDisplay("[Status {State} {Elapsed}/{Duration}")]
    public class MPDStatus : ObservableObject, IAurenderStatus, IARLog, INotifyPropertyChanged
    {
        public MPDStatus()
        {
        }

        internal IPlaylist Queue;

        private PlayState _state;
        private OptionForRepeat _repeat;
        private bool _isRandom;
        private bool _isConsume;
        private int _playlistVersion;
        private int _playlistLength;
        private int _currentTrackIndex;
        private int _nextTrackIndex;
        private int _duration;
        private int _elapsed;
        private int _bitrate;
        private SamplingRate _samplingRate;
        private OptionsMQADecodingStatus _mqaDecodingStatus;

        protected PlayState state { get => _state; set => this.Set<PlayState>(ref _state, value, "State"); }
        protected OptionForRepeat repeat { get => _repeat; set => this.Set<OptionForRepeat>(ref _repeat, value, "Repeat"); }
        protected bool isRandom { get => _isRandom; set => Set<bool>(ref _isRandom, value, "IsRandom"); }
        protected bool isConsume { get => _isConsume; set => Set<bool>(ref _isConsume, value, "IsConsume"); }
        protected int playlistVersion
        {
            get => _playlistVersion;
            set => Set<int>(ref _playlistVersion, value, "PlaylistVersion");
        }
        protected int playlistLength { get => _playlistLength; set => Set<int>(ref _playlistLength, value, "PlaylistLength"); }
        protected int currentTrackIndex
        {
            get => _currentTrackIndex;
            set
            {
                if (_currentTrackIndex != value)
                {
                    Set<int>(ref _currentTrackIndex, value, "CurrentTrackIndex");
                    this.RaisePropertyChanged("CurrentSong");
                }
            }
        }
        
        internal void ResetPlaylistVersion()
        {
            _playlistVersion = 0;
        }

        protected int nextTrackIndex { get => _nextTrackIndex; set => Set<int>(ref _nextTrackIndex, value, "NextTrackIndex"); }
        protected int duration { get => _duration; set => Set<int>(ref _duration, value, "Duration"); }
        protected int elapsed { get => _elapsed; set => Set<int>(ref _elapsed, value, "Elapsed"); }
        protected int bitrate { get => _bitrate; set => Set<int>(ref _bitrate, value, "Bitrate"); }
        protected SamplingRate samplingRate { get => _samplingRate; set => Set<SamplingRate>(ref _samplingRate, value, "SamplingRate"); }
        protected OptionsMQADecodingStatus mqaDecodingStatus { get => _mqaDecodingStatus; set => Set<OptionsMQADecodingStatus>(ref _mqaDecodingStatus, value, "MQADecodingStatus"); }


        PlayState IAurenderStatus.State => state;

        OptionForRepeat IAurenderStatus.RepeatMode => repeat;

        bool IAurenderStatus.IsRandom => isRandom;

        public bool IsConsume
        {
            get => isConsume;
            set
            {

            }
        }

        int IAurenderStatus.PlaylistVersion => playlistVersion;

        int IAurenderStatus.PlaylistLength => playlistLength;

        int IAurenderStatus.CurrentTrackIndex => currentTrackIndex;

        int IAurenderStatus.NextTrackIndex => nextTrackIndex;

        IPlayableItem IAurenderStatus.CurrentSong
        {
            get
            {
                int idx = this.currentTrackIndex;
                if (idx == -1) idx = 0;

                if (Queue != null && idx < Queue.Count)
                {
                    return this.Queue[idx];
                }
                return null;
            }
        }

        int IAurenderStatus.Duration => duration;

        int IAurenderStatus.Elapsed => elapsed;

        int IAurenderStatus.Bitrate => bitrate;

        SamplingRate IAurenderStatus.SamplingRate => samplingRate;

        OptionsMQADecodingStatus IAurenderStatus.MQADecodingStatus => mqaDecodingStatus;


        internal void UpdateStatus(string responseString)
        {
            var matches = StatusParser.Matches(responseString);

            bool newSingle = false;
            var newRepeat = OptionForRepeat.Off;

            foreach (Match m in matches)
            {
                if (m.Groups.Count == 3)
                {
                    ProcessData(m.Groups[1].Value, m.Groups[2].Value.Replace('\r', ' ').Trim(), ref newSingle, ref newRepeat);
                }
            }

            if (newSingle)
            {
                if (newRepeat == OptionForRepeat.All)
                {
                    this.repeat = OptionForRepeat.Once;
                }
                else
                {
                    this.repeat = OptionForRepeat.Single;
                }
            }
            else
            {
                this.repeat = newRepeat;
            }
        }

        internal void ProcessData(String type, String value, ref bool newSingle, ref OptionForRepeat newRepeat)
        {
            Debug.Assert(value != null, "Value shoudn't be null");
            this.LP("MPD status", $"{type} =====> [{value}]");

            switch (type)
            {
                case "repeat":
                    newRepeat = "1".Equals(value) ? OptionForRepeat.All : OptionForRepeat.Off;
                    break;

                case "random":
                    isRandom = "1".Equals(value);
                    break;

                case "consume":
                    isConsume = "1".Equals(value);
                    break;

                case "single":
                    newSingle = "1".Equals(value);
                    break;

                case "playlist":
                    playlistVersion = Int32.Parse(value);
                    break;

                case "playlistlength":
                    playlistLength = Int32.Parse(value);
                    break;

                case "state":
                    if ("play".Equals(value))
                        state = PlayState.Play;
                    else if ("pause".Equals(value))
                        state = PlayState.Paused;
                    else if ("stop".Equals(value))
                    {
                        state = PlayState.Stopped;
                        elapsed = 0;
                        duration = 1;
                        currentTrackIndex = -1;
                    }
                    else
                    {
                        this.E($"Failed to get playstate : {value}");
                        state = PlayState.Unknown;
                    }
                    break;

                case "song":
                    currentTrackIndex = Int32.Parse(value);
                    break;

                case "time":
                    var timeInfo = StatusUtil.ParseTimeInfo(value);
                    elapsed = timeInfo.Item1;
                    duration = timeInfo.Item2;
                    break;

                case "bitrate":
                    bitrate = Int32.Parse(value);
                    break;

                case "audio":
                    samplingRate = StatusUtil.ParseAudioInfo(value);
                    break;

                case "nextsong":
                    nextTrackIndex = Int32.Parse(value);
                    break;

                case "MQA":
                    mqaDecodingStatus = StatusUtil.ParseForMQA(value);
                    break;


                default:
                    /// we ignore other than above
                    break;
            }
        }

        /// <summary>
        /// Windows : (\\S*):[ ](.*)\r | Android : (\\S*):[ ](.*)
        /// </summary>
        static readonly Regex StatusParser = new Regex(@"(\S*):[ ](.*)[\s]*");

        #region IARLog
        private bool LogAll = false;

        bool IARLog.IsARLogEnabled { get { return LogAll; } set { LogAll = value; } }

        #endregion
    }

    internal static class StatusUtil
    {
        internal static Tuple<Int32, Int32> ParseTimeInfo(String line)
        {
            Int32 elapsed = 0;
            Int32 duration = 0;
            var match = sTimeInfoRegex.Match(line);
            if (match.Groups.Count == 3)
            {
                elapsed = Int32.Parse(match.Groups[1].Value);
                duration = Int16.Parse(match.Groups[2].Value);
            }

            return new Tuple<Int32, Int32>(elapsed, duration);
        }

        internal static SamplingRate ParseAudioInfo(String line)
        {
            SamplingRate sampleRate = SamplingRate.BRSRF_44100;
            Int32 bitWidth = 16;

            var match = sTimeInfoRegex.Match(line);
            if (match.Groups.Count == 3)
            {
                Int32 tSampleRate = Int32.Parse(match.Groups[1].Value);

                switch (tSampleRate)
                {
                    case 44100:
                        sampleRate = SamplingRate.BRSRF_44100;
                        break;
                    case 48000:
                        sampleRate = SamplingRate.BRSRF_48000;
                        break;
                    case 88200:
                        sampleRate = SamplingRate.BRSRF_88200;
                        break;
                    case 96000:
                        sampleRate = SamplingRate.BRSRF_96000;
                        break;
                    case 176400:
                        sampleRate = SamplingRate.BRSRF_176400;
                        break;
                    case 192000:
                        sampleRate = SamplingRate.BRSRF_192000;
                        break;
                    case 352800:
                        sampleRate = SamplingRate.BRSRF_352800;
                        break;
                    case 384000:
                        sampleRate = SamplingRate.BRSRF_384000;
                        break;

                    default:
                        IARLogStatic.Error("MPDStatus", $"Failed to parse sampling rate: {tSampleRate}");
                        break;
                }

                if (!int.TryParse(match.Groups[2].Value, out bitWidth))
                {
                    bitWidth = 16;
                }

                // Exception
                //bitWidth = Int16.Parse(match.Groups[2].Value);

                switch (bitWidth)
                {
                    case 16:
                        sampleRate |= SamplingRate.BRSRF_16Bit;
                        break;
                    case 24:
                        sampleRate |= SamplingRate.BRSRF_24Bit;
                        break;
                    case 32:
                        sampleRate |= SamplingRate.BRSRF_32Bit;
                        break;

                    default:
                        IARLogStatic.Error("MPDStatus", "Failed to parse bit-width");
                        break;

                }
            }

            return sampleRate;
        }

        internal static OptionsMQADecodingStatus ParseForMQA(String line)
        {
            OptionsMQADecodingStatus mqa = OptionsMQADecodingStatus.None;

            var match = sMQAInfoRegex.Match(line);
            if (match.Groups.Count == 3)
            {
                var studio = match.Groups[1].Value;
                var quality = Int16.Parse(match.Groups[2].Value);

                if ("STUDIO".Equals(studio))
                {
                    mqa = OptionsMQADecodingStatus.Studdio;
                }
                else if ("MASTER".Equals(studio))
                {
                    mqa = OptionsMQADecodingStatus.Master;
                }

                switch (quality)
                {
                    case 1:
                        mqa = mqa | OptionsMQADecodingStatus.MQA_1X;
                        break;

                    case 2:
                        mqa = mqa | OptionsMQADecodingStatus.MQA_2X;
                        break;

                    case 3:
                        mqa = mqa | OptionsMQADecodingStatus.MQA_3X;
                        break;

                    case 4:
                        mqa = mqa | OptionsMQADecodingStatus.MQA_4X;
                        break;

                    default:
                        break;
                }
            }

            return mqa;
        }

        static readonly Regex sMQAInfoRegex = new Regex("([A-Z]*) \\[[\\d]\\]");
        static readonly Regex sTimeInfoRegex = new Regex("(\\d*):(\\d*)");
        static readonly Regex sAudioInfoRegex = new Regex("(\\d*):(\\d*):(\\d*)");
    }
}
