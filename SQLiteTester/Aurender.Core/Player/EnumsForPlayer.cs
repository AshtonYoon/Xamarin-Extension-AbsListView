using System;

namespace Aurender.Core.Player
{
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum OptionForAddPosition : short
    {
        Now, //next and play
        Next,
        End,
        UserDefault
    }

    public enum OptionForRepeat : short
    {
        Off,
        All,
        Once,
        Single,
    }

    public enum PlayState : short
    {
        Unknown,
        Play,
        Paused,
        Stopped,

    }



    [Flags]
    public enum OptionsMQADecodingStatus
    {
        None,
        Master,
        Studdio,
        MQA_1X,
        MQA_2X,
        MQA_3X,
        MQA_4X
    }

    [Flags]
    public enum SamplingRate : Int32
    {
        BRSRF_NONE       = 00000_0000,
        BRSRF_16Bit      = 00000_0001,
        BRSRF_24Bit      = 00000_0010,
        BRSRF_32Bit      = 00000_0100,
        BRSRF_DSD64X     = 00000_1000,
        BRSRF_DSD128X    = 00001_0000,
        BRSRF_DSD256X    = 00010_0000,
						   
        BRSRF_MQA        = 2^7,
						  
        BRSRF_44100      = 2^8,
        BRSRF_48000      = 2^9,
        BRSRF_88200      = 2^10,
        BRSRF_96000      = 2^11,
        BRSRF_176400     = 2^12,
        BRSRF_192000     = 2^13,
        BRSRF_352800     = 2^14,
        BRSRF_384000     = 2^15,
						   
        BRSRF_MQA_44100  = 2^20,
        BRSRF_MQA_48000  = 2^21,
        BRSRF_MQA_88200  = 2^22,
        BRSRF_MQA_96000  = 2^23,
        BRSRF_MQA_176400 = 2^24,
        BRSRF_MQA_192000 = 2^25,
        BRSRF_MQA_352800 = 2^26,
        BRSRF_MQA_384000 = 2^27,

        BRSRF_ALL = -1,
    }


    public enum ScannerScanningMode
    {
        Update,

        CleanScanWithImageClear,
        CleanScanWithDataClear,
        CleanScanWithCacheClear,
    }

    public enum ScannerStatus
    {
        Unknown,
        Puased,
        Scanning,
        ProcessingISO,
        Finalizing,
        Finished,
    }




}
