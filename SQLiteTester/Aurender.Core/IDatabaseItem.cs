using System;
using System.Collections.Generic;
using System.IO;
using Aurender.Core.Utility;

namespace Aurender.Core
{
    [Flags]
    public enum IInformationAvailablity {
        NONE                = 0,
        TRACK               = 1 << 1,
        SONG                = 1 << 2,
        ALBUM               = 1 << 3,
        GENRE               = 1 << 4,
        ARTIST              = 1 << 5,
        COMPOSER            = 1 << 6,
        CONDUCTOR           = 1 << 7,
        ALBUM_COVER         = 1 << 8,
        ADD_TO_MY_LIB       = 1 << 9,
        MULTIPLE_SELECTION  = 1 << 10,
        SHAREABLE           = 1 << 11,
        CAN_REMOVE_FROM_LIB = 1 << 12,

    }

    public interface IDataWithList
    {
        IList<Object> data {get; set;}
    }

	public interface IDatabaseItem 
	{
		Int32 dbID { get; }
        String Key { get; }

        IInformationAvailablity GetAvailability(); 
	}


    public static class IDataWithListExt
    {
        public static object GetImageFromIndex(this IDataWithList item, int index)
        {
            System.Diagnostics.Debug.Assert(index < item.data.Count, "Index out of bounds");

            object o = item.data[index];
            var imageData = o as Byte[];

            if (o != null) {
                var image = ImageUtility.GetImageSourceFromStream(new MemoryStream(imageData));
                return image;
            }
            else
            {
                return null;
            }
        }

        public static byte[] GetImageDataFromIndex(this IDataWithList item, int index)
        {
            object o = item.data[index];
            Byte[] imageData = null; 

            if (o != null)
            {
                imageData = o as Byte[];
            }

            return imageData;
        }
    }
}