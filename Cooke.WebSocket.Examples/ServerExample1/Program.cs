using System;
using System.Net.Sockets;
using System.Net;
using Cooke.WebSocket;

namespace WebSocketsTest
{
    class Program
    {
        private static WebSocketHandshakeManager handshaker = new WebSocketHandshakeManager();
        private static TcpListener listener = new TcpListener(IPAddress.Any, 8181);

        static void Main(string[] args)
        {
            listener.Start();
            listener.BeginAcceptSocket(AcceptSocketFinished, null);            
            Console.ReadKey(true);
        }

        static void AcceptSocketFinished(IAsyncResult ar)
        {
            var socket = listener.EndAcceptSocket(ar);
            handshaker.BeginHandshake(socket, HandshakeFinished, null);
            listener.BeginAcceptSocket(AcceptSocketFinished, null);
        }

        static void HandshakeFinished(IAsyncResult ar)
        {
            var webSocket = handshaker.EndHandshake(ar);
            webSocket.BeginSendMessage("Welcome to the web socket server", SendMessageFinished, webSocket);
            webSocket.BeginReceiveMessage(ReceiveMessageFinished, webSocket);
        }

        static void SendMessageFinished(IAsyncResult ar)
        {
            var webSocket = (WebSocketSession)ar.AsyncState;            
            webSocket.EndSendMessage(ar);   
        }

        static void ReceiveMessageFinished(IAsyncResult ar)
        {
            var webSocket = (WebSocketSession)ar.AsyncState;
            var receiveMessage = webSocket.EndReceiveMessage(ar);
            webSocket.BeginReceiveMessage(ReceiveMessageFinished, webSocket);
            webSocket.BeginSendMessage("ECHO: " + receiveMessage, SendMessageFinished, webSocket);
        }
    }
}
