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
            newWebSocket.SendMessage("Welcome to this simple web socket server!");
            newWebSocket.NewMessagesAvailable += HandleNewMessage;

            Console.WriteLine("New connection");
        }

        private static void HandleNewMessage(object sender, EventArgs e)
        {
            var webSocketClient = (IWebSocketClient)sender;
            string receivedMessage = webSocketClient.GetNewMessageOrDefault();            
            webSocketClient.SendMessage("Echo: " + receivedMessage);

            Console.WriteLine("Received message: {0}", receivedMessage);
        }
    }
}
