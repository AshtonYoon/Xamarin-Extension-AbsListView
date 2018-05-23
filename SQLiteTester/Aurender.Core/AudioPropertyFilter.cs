using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurender.Core
{
    [Flags]
    public enum AudioPropertyFilter
    {
        F_NONE = 0,

        F_16Bit = 1 << 0,
        F_24Bit = 1 << 1,
        F_32Bit = 1 << 2,

        F_DSD64 = 1 << 3,
        F_DSD128 = 1 << 4,
        F_DSD256 = 1 << 5,
        F_DSD512 = 1 << 6,

        F_MQA = 1 << 7,

        F_44100 = 1 << 8,
        F_48000 = 1 << 9,
        F_88200 = 1 << 10,
        F_96000 = 1 << 11,
        F_176400 = 1 << 12,
        F_192000 = 1 << 13,
        F_352800 = 1 << 14,
        F_384000 = 1 << 15,

        F_MQA_44100 = 1 << 20,
        F_MQA_48000 = 1 << 21,
        F_MQA_88200 = 1 << 22,
        F_MQA_96000 = 1 << 23,
        F_MQA_176400 = 1 << 24,
        F_MQA_192000 = 1 << 25,
        F_MQA_352800 = 1 << 26,
        F_MQA_384000 = 1 << 27,

        F_44100_X = F_44100 | F_88200 | F_176400 | F_352800,
        F_48000_X = F_48000 | F_96000 | F_192000 | F_384000,

        F_NORMAL_SAMPLING_RATE = F_44100_X | F_48000_X,

        F_MQA_44100_X = F_MQA_44100 | F_MQA_88200 | F_MQA_176400 | F_MQA_352800,
        F_MQA_48000_X = F_MQA_48000 | F_MQA_96000 | F_MQA_192000 | F_MQA_384000,

        F_MQA_SAMPLING_RATE = F_MQA_44100_X | F_MQA_48000_X,


        F_ALL_SAMPLING_RATE = F_NORMAL_SAMPLING_RATE | F_MQA_SAMPLING_RATE,


        F_DSD_X = F_DSD64 | F_DSD128 | F_DSD256 | F_DSD512,
        F_NON_DSD_X = F_16Bit | F_24Bit | F_32Bit,

        F_BIT_WIDTH = F_NON_DSD_X| F_DSD_X,
    }
}
