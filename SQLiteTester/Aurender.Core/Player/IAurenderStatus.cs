using System;
using System.ComponentModel;

namespace Aurender.Core.Player
{
    public interface IAurenderStatus : INotifyPropertyChanged
    {
        PlayState State { get; }
        OptionForRepeat RepeatMode { get; }
        Boolean IsRandom { get; }
        Boolean IsConsume { get; }

        Int32 PlaylistVersion { get; }
        Int32 PlaylistLength { get; }

        Int32 CurrentTrackIndex { get; }
        Int32 NextTrackIndex { get; }

        IPlayableItem CurrentSong { get; }

        Int32 Duration { get; }
        Int32 Elapsed { get; }
        Int32 Bitrate { get; }

        SamplingRate SamplingRate { get; }

        OptionsMQADecodingStatus MQADecodingStatus { get; }
    }
}
