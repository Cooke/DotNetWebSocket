using System;
using System.Net;
using DotNetWebSocket;

namespace WebSocketsTest
{
    public class Program
    {
        private static readonly WebSocketServer Server = new WebSocketServer(IPAddress.Any, 8181);

        public static void Main(string[] args)
        {
            Server.Start();
            Server.BeginAccept(HandleAcceptSocketCompleted, null);            
            Console.ReadKey(true);
        }

        private static void HandleAcceptSocketCompleted(IAsyncResult ar)
        {
            Console.WriteLine("New connection, starting handshake process..");

            var socket = Server.EndAccept(ar);
            Server.BeginHandshake(socket, HandleHandshakeCompleted, null);

            Server.BeginAccept(HandleAcceptSocketCompleted, null);
        }

        private static void HandleHandshakeCompleted(IAsyncResult ar)
        {
            WebSocket webSocket;
            try
            {
                webSocket = Server.EndHandshake(ar);
                Console.WriteLine("Handshake completed");
            }
            catch (HandshakeException ex)
            {
                Console.WriteLine("Handshake failed: {0}", ex);
                return;
            }

            webSocket.BeginSendMessage("Welcome to the web socket server", SendMessageFinished, webSocket);
            webSocket.BeginReceiveMessage(HandleReceiveMessageCompleted, webSocket);
        }

        private static void SendMessageFinished(IAsyncResult ar)
        {
            var webSocket = (WebSocket)ar.AsyncState;            
            webSocket.EndSendMessage(ar);   
        }

        private static void HandleReceiveMessageCompleted(IAsyncResult ar)
        {
            var webSocket = (WebSocket)ar.AsyncState;            
            var receiveMessage = webSocket.EndReceiveMessage(ar);

            Console.WriteLine("Message received: {0}", receiveMessage);
            
            webSocket.BeginReceiveMessage(HandleReceiveMessageCompleted, webSocket);
            webSocket.BeginSendMessage("ECHO: " + receiveMessage, SendMessageFinished, webSocket);
        }
    }
}
