using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Aurender.Core.Utility
{
    public static class UUEncodeDecode
    {
        public static Byte[] UUDecode(StreamReader input)
        {

            var result = new MemoryStream();

            for (String ascii; (ascii = input.ReadLine()) != null;)
            {
                var encodedBuffer = Encoding.GetEncoding("us-ascii").GetBytes(ascii);

                if (encodedBuffer[0] == 0x60) break;

                var nrDecoded = encodedBuffer[0] - 32;

                var decoded = new Byte[nrDecoded];


                for (Int32 i = 1, j = 0; i + 3 < encodedBuffer.Length ; i += 4)
                {
                    var tmp = encodedBuffer.Skip(i).Take(4).Select(b => (Byte)((b - 0x20) & 0x3F)).ToArray();

                    decoded[j++] = (Byte)(tmp[0] << 2 | tmp[1] >> 4);

                    if (j == nrDecoded) break;

                    decoded[j++] = (Byte)(tmp[1] << 4 | tmp[2] >> 2);

                    if (j == nrDecoded) break;

                    decoded[j++] = (Byte)(tmp[2] << 6 | tmp[3]);

                    if (j == nrDecoded) break;

                }

                result.Write(decoded, 0, nrDecoded);
            }


            return result.ToArray();

        }

    }
}
