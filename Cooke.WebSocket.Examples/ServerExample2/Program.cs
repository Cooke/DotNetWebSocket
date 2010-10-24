using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cooke.WebSocket;

namespace Example2
{
    class Program
    {
        private static WebSocketHandshakeManager handshaker = new WebSocketHandshakeManager();
        private static List<WebSocket> sockets = new List<WebSocket>();
        private static TcpListener listener = new TcpListener(IPAddress.Any, 8181);
        private static object socketsLock = new object();

        static void Main(string[] args)
        {
            try
            {
                listener.Start();
                listener.BeginAcceptSocket(AcceptCallback, null);

                while (true)
                {
                    lock (socketsLock)
                    {
                        foreach (var socket in sockets.ToArray())
                        {
                            try
                            {
                                socket.BeginSendMessage("PING", SendCallback, socket);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                sockets.Remove(socket);
                            }
                        }
                    }

                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.ReadKey(true);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            var socket = (WebSocket)ar.AsyncState;

            try
            {
                socket.EndSendMessage(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                lock (socketsLock)
                {
                    sockets.Remove(socket);
                }
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                var socket = listener.EndAcceptSocket(ar);
                handshaker.BeginHandshake(socket, HandshakeCallback, socket);
                Console.WriteLine("Accept connection. Started handshake.");
                listener.BeginAcceptSocket(AcceptCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Accept failed.");
                Console.WriteLine(ex.Message);
            }
        }

        private static void HandshakeCallback(IAsyncResult ar)
        {
            WebSocket socket = null;
            try
            {
                socket = handshaker.EndHandshake(ar);

                lock (socketsLock)
                {
                    sockets.Add(socket);
                }

                Console.WriteLine("Handshake completed.");
                socket.BeginReceiveMessage(ReceiveCallback, socket);
            }                
            catch (Exception ex)
            {
                Console.WriteLine("Handshake failed.");
                Console.WriteLine(ex.Message);

                if(ex is TimeoutException)
                {
                    Console.WriteLine(
                        "Timeout exception will happen when a websocket client compatible to version 75 tries to connect to a web socket server compatible to version 76 (which this server is).");
                }

                if (socket != null)
                {
                    lock (socketsLock)
                    {
                        sockets.Remove(socket);
                    }
                }
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            var socket = (WebSocket)ar.AsyncState;
            try
            {
                var clientMessage = socket.EndReceiveMessage(ar);
                Console.WriteLine("CLIENT: " + clientMessage);
                socket.BeginSendMessage("ECHO: " + clientMessage, SendCallback, socket);
                socket.BeginReceiveMessage(ReceiveCallback, socket);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Receive failed.");
                Console.WriteLine(ex.Message);
                lock (socketsLock)
                {
                    sockets.Remove(socket);
                }
            }
        }
    }
}
