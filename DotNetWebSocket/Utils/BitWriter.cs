using System.IO;

namespace DotNetWebSocket.Utils
{
    internal class BitWriter
    {
        private readonly MemoryStream stream;
        private byte data;
        private int occuppiedDataBits;

        public BitWriter(byte[] buffer)
        {
            stream = new MemoryStream(buffer);
        }

        public long WrittenBytes
        {
            get { return stream.Position; }
        }

        public bool IsByteAligned
        {
            get { return occuppiedDataBits == 0; }
        }

        public void WriteBit(int bit)
        {
            data = (byte)((data << 1) | (bit & 1));
            occuppiedDataBits++;
            if (occuppiedDataBits == sizeof(byte) * 8)
            {
                stream.WriteByte(data);
                occuppiedDataBits = 0;
                data = 0;
            }
        }

        public void WriteRepeat(int bit, int count)
        {
            for (int i = 0; i < count; i++)
            {
                WriteBit(bit);
            }
        }

        public void WriteBits(int bits, int bitCount)
        {
            for (int i = bitCount - 1; i >= 0; i--)
            {
                WriteBit(bits >> i);
            }
        }

        public void WriteBytes(byte[] bytes)
        {
            foreach (var b in bytes)
            {
                WriteBits(b, 8);
            }
        }
    }
}