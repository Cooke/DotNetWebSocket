using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DotNetWebSocket.Handshaking
{
    internal class ServerHandshake
    {
        private readonly string key;

        public ServerHandshake(string key)
        {
            this.key = key;
        }

        public byte[] ToByteArray()
        {
            var concat = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            var sha1 = SHA1.Create();
            var hashed = sha1.ComputeHash(Encoding.UTF8.GetBytes(concat));
            var accept = Convert.ToBase64String(hashed);

            var memStream = new MemoryStream();
            var writer = new StreamWriter(memStream);
            writer.WriteLine("HTTP/1.1 101 Switching Protocols");
            writer.WriteLine("Upgrade: websocket");
            writer.WriteLine("Connection: Upgrade");
            writer.WriteLine("Sec-WebSocket-Accept: {0}", accept);
            writer.WriteLine();
            writer.Flush();

            return memStream.ToArray();
        }
    }
}