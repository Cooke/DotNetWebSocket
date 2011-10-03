using System;
using System.IO;

namespace DotNetWebSocket.Utils
{
    internal class BitReader
    {
        private readonly MemoryStream stream;
        private byte data;
        private int dataBits;

        public BitReader(byte[] buffer, int count)
        {
            stream = new MemoryStream(buffer, 0, count, false);
        }

        public int ReadBit()
        {
            if (dataBits == 0)
            {
                int readByte = stream.ReadByte();
                if (readByte == -1)
                {
                    throw new InvalidOperationException("Tried to read beyond the end of the bit stream");
                }

                data = (byte)readByte;
                dataBits = sizeof(byte) * 8;
            }

            dataBits--;
            return (data >> dataBits) & 1;
        }

        public int ReadBits(int bitCount)
        {
            int intData = 0;
            for (int i = 0; i < bitCount; i++)
            {
                intData = (intData << 1) | ReadBit();
            }

            return intData;
        }

        public byte[] ReadBytes(int byteCount)
        {
            var bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] = (byte)ReadBits(8);
            }

            return bytes;
        }
    }
}