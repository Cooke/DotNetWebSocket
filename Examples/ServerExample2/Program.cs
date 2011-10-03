using System;
using System.Net;
using DotNetWebSocket;

namespace Example2
{
    public class Program
    {
        public static readonly WebSocketListener Server = new WebSocketListener(IPAddress.Any, 8181);

        public static void Main(string[] args)
        {
            Server.Start();
            Server.NewClientAvailable += HandleNewConnection;

            Console.ReadLine();
        }

        private static void HandleNewConnection(object sender, EventArgs eventArgs)
        {
            var newWebSocket = Server.GetNewClientOrDefault();

            newWebSocket.BeginSendMessage("Welcome to this simple web socket server!", HandleSendMessageCompleted, newWebSocket);
            newWebSocket.BeginReceiveMessage(HandleReceiveMessageCompleted, newWebSocket);

            Console.WriteLine("New connection");
        }

        private static void HandleReceiveMessageCompleted(IAsyncResult ar)
        {
            var webSocketSession = (WebSocket)ar.AsyncState;
            string receivedMessage = webSocketSession.EndReceiveMessage(ar);

            Console.WriteLine("Received message: {0}", receivedMessage);
            webSocketSession.BeginSendMessage("Echo: " + receivedMessage, HandleSendMessageCompleted, webSocketSession);

            webSocketSession.BeginReceiveMessage(HandleReceiveMessageCompleted, webSocketSession);
        }

        private static void HandleSendMessageCompleted(IAsyncResult ar)
        {
            var webSocketSession = (WebSocket)ar.AsyncState;
            webSocketSession.EndSendMessage(ar);
        }
    }
}
