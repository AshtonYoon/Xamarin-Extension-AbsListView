using System;
using System.Diagnostics;

namespace Aurender.Core.Player
{
    public static class PlayerHelper
    { 

        public static String ToTimeFormatString(Int32 totalSec)
        {
            Int32 sec = totalSec % 60;
            Int32 hour = totalSec / 3600;
            Int32 minute = (totalSec % 3600) / 60;
            return $"{hour}:{minute:d2}:{sec:d2}";
        }

        public static OptionForRepeat NextRepeatMode(this OptionForRepeat current)
        {
            if (current != OptionForRepeat.Single)
            {
                current++;
                return current;
            }
            else
            {
                return OptionForRepeat.Off;
            }
            
            
        }

        public static String CommandForMpd(this OptionForRepeat repeat)
        {
            String command = "";
            switch (repeat)
            {
                case OptionForRepeat.Off:
                    command = "repeat 0\nsingle 0";
                    break;

                case OptionForRepeat.All:
                    command = "repeat 1\nsingle 0";
                    break;

                case OptionForRepeat.Once:
                    command = "repeat 1\nsingle 1";
                    break;

                case OptionForRepeat.Single:
                    command = "repeat 0\nsingle 1";
                    break;
            }

            return command;
        }

        public static Int32 PositionToBeAdded(this OptionForAddPosition position, IAurenderStatus status)
        {
            Int32 next = status.CurrentTrackIndex + 1;

            switch (position)
            {
                case OptionForAddPosition.End:
                    next = status.PlaylistLength;
                    break;

                case OptionForAddPosition.Next:
                    // it's already set
                    break;

                case OptionForAddPosition.Now:
                    next = status.CurrentTrackIndex;
                    break;

                case OptionForAddPosition.UserDefault:
                    Debug.Assert(false, "This shouldn't be happen. UI layer should change proper option for position");
                    break;
            }
            return next;
        }

        public static String BitWidth(this SamplingRate rate)
        {
            String result;

            switch (rate)
            {
                
                case SamplingRate.BRSRF_16Bit:
                    result = "16bit";
                    break;
                case SamplingRate.BRSRF_24Bit:
                    result = "24bit";
                    break;
                case SamplingRate.BRSRF_32Bit:
                    result = "32bit";
                    break;
                case SamplingRate.BRSRF_DSD64X:
                    result = "DSD64";
                    break;
                case SamplingRate.BRSRF_DSD128X:
                    result = "DSD128";
                    break;
                case SamplingRate.BRSRF_DSD256X:
                    result = "DSD256";
                    break;
                default:
                    result = "";
                    break;
            }

            return result;
        }

        public static String GetSamplingRate(this SamplingRate rate)
        {
            String result;

            switch(rate)
            {
                case SamplingRate.BRSRF_44100:
                    result = "44.1kHz";
                    break;
                case SamplingRate.BRSRF_48000:
                    result = "48kHz";
                    break;
                case SamplingRate.BRSRF_88200:
                    result = "88.2kHz";
                    break;
                case SamplingRate.BRSRF_96000:
                    result = "96kHz";
                    break;
                case SamplingRate.BRSRF_176400:
                    result = "176.4kHz";
                    break;
                case SamplingRate.BRSRF_192000:
                    result = "192kHz";
                    break;
                case SamplingRate.BRSRF_352800:
                    result = "352.8kHz";
                    break;
                case SamplingRate.BRSRF_384000:
                    result = "384kHz";
                    break;

                default:
                    result = "";
                    break;
            }
            return result;
        }

        public static String GetMQASamplingRate(this SamplingRate rate)
        {
            String result;

            switch (rate)
            {
                case SamplingRate.BRSRF_MQA_44100:
                    result = "M44.1kHz";
                    break;
                case SamplingRate.BRSRF_MQA_48000:
                    result = "M48kHz";
                    break;
                case SamplingRate.BRSRF_MQA_88200:
                    result = "M88.2kHz";
                    break;
                case SamplingRate.BRSRF_MQA_96000:
                    result = "M96kHz";
                    break;
                case SamplingRate.BRSRF_MQA_176400:
                    result = "M176.4kHz";
                    break;
                case SamplingRate.BRSRF_MQA_192000:
                    result = "M192kHz";
                    break;
                case SamplingRate.BRSRF_MQA_352800:
                    result = "M352.8kHz";
                    break;
                case SamplingRate.BRSRF_MQA_384000:
                    result = "M384kHz";
                    break;
                default:
                    Debug.Assert(false, "Unexpected case fror sampling rate");
                    result = "Err";
                    break;
            }

            return result;
        }
 

        public static String ConvertToString(this SamplingRate rate)
        {
            String result;
            if (rate == SamplingRate.BRSRF_NONE)
            {
                result = "None";
            }
            if (rate < SamplingRate.BRSRF_MQA)
            {
                result = rate.BitWidth();
            }
            else if (rate == SamplingRate.BRSRF_MQA)
            {
                result = "MQA";
            }
            else if (rate < SamplingRate.BRSRF_MQA_44100)
            {
                result = rate.GetSamplingRate();
            }
            else if (rate < SamplingRate.BRSRF_ALL)
            {
            result = rate.GetMQASamplingRate();
            }
            else
            {
                result = "All";
            }
        return result;
        }

    }
}
