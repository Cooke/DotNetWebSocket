using System;
using System.Diagnostics;
using System.Text;
using DotNetWebSocket.Utils;

namespace DotNetWebSocket.Framing
{
    internal class DataFrame
    {
        private static readonly Random Random = new Random();

        private readonly string message;

        public DataFrame(string message)
        {
            this.message = message;
        }

        public enum OpCode
        {
            Text = 0x1
        }

        public string Message
        {
            get 
            {
                return message;
            }
        }

        public static bool TryReadFrom(byte[] buffer, int size, out DataFrame frame, out int consumedBytes)
        {
            frame = null;
            consumedBytes = 0;

            if (size < 2)
            {
                return false;
            }

            var stream = new BitReader(buffer, size);

            int fragmented = stream.ReadBit();
            if (fragmented == 0)
            {
                throw new NotSupportedException("Fragmented messages are currently not supported");
            }

            // Reserved
            stream.ReadBits(3);

            // Op code
            var operationCode = (OpCode)stream.ReadBits(4);
            if (operationCode != OpCode.Text)
            {
                throw new NotSupportedException("Only text frames are currently supported");
            }

            // Masking
            bool isMasked = stream.ReadBit() != 0;
            if (!isMasked)
            {
                throw new NotSupportedException("Only masked frames are supported");
            }

            // Payload
            var payloadSize = (ulong)stream.ReadBits(7);
            consumedBytes = 2;
            if (payloadSize == 126)
            {
                byte[] payloadBytes = stream.ReadBytes(2);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(payloadBytes);
                }

                payloadSize = BitConverter.ToUInt16(payloadBytes, 0);
                consumedBytes = 4;
            }
            else if (payloadSize == 127)
            {
                byte[] payloadSizeBytes = stream.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(payloadSizeBytes);
                }

                payloadSize = BitConverter.ToUInt64(payloadSizeBytes, 0);
                consumedBytes = 10;
            }

            if (payloadSize > int.MaxValue)
            {
                throw new NotSupportedException("Only frames with a payload smaller than int.MaxValue are currently supported");
            }

            byte[] maskBytes = stream.ReadBytes(4);
            consumedBytes += 4;            

            for (int i = 0; i < (int)payloadSize; i++)
            {
                buffer[consumedBytes + i] = (byte)(buffer[consumedBytes + i] ^ maskBytes[i % 4]);
            }

            consumedBytes += (int)payloadSize;

            string message = Encoding.UTF8.GetString(buffer, consumedBytes - (int)payloadSize, (int)payloadSize);
            frame = new DataFrame(message);
            return true;
        }

        public void WriteTo(byte[] buffer, out int frameSize)
        {
            int payloadLength = Encoding.UTF8.GetByteCount(message);            

            var bitStream = new BitWriter(buffer);

            // Fin
            bitStream.WriteBit(1);
            
            // Reserved bits
            bitStream.WriteRepeat(0, 3);

            // Text frame
            bitStream.WriteBits((int)OpCode.Text, 4);

            // Should mask
            bitStream.WriteBit(0);

            // Payload length
            if (payloadLength < 126)
            {
                bitStream.WriteBits(payloadLength, 7);
            }
            else if (payloadLength < ushort.MaxValue)
            {
                bitStream.WriteBits(126, 7);
                byte[] payloadBytes = BitConverter.GetBytes((ushort)payloadLength);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(payloadBytes);
                }

                bitStream.WriteBytes(payloadBytes);
            }
            else
            {
                bitStream.WriteBits(127, 7);
                byte[] payloadBytes = BitConverter.GetBytes((ulong)payloadLength);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(payloadBytes);
                }

                bitStream.WriteBytes(payloadBytes);
            }

            // Create mask value
            //var mask = new byte[4];
            //Random.NextBytes(mask);
            
            //// Write mask
            //bitStream.WriteBytes(mask);

            Debug.Assert(bitStream.IsByteAligned, "Data frame header must occupy an even number of bytes");

            var headerLength = (int)bitStream.WrittenBytes;
            Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, headerLength);

            // Mask payload
            for (int i = 0; i < payloadLength; i++)
            {
                // buffer[i + headerLength] = (byte)(buffer[i + headerLength] ^ mask[i % 4]);
                buffer[i + headerLength] = buffer[i + headerLength];
            }

            frameSize = headerLength + payloadLength;
        }
    }
}