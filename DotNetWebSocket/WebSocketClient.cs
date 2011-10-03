using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotNetWebSocket.Utils;

namespace DotNetWebSocket
{
    public class WebSocketClient : IWebSocketClient
    {
        private readonly ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> outgoingMessages = new ConcurrentQueue<string>();
        private readonly SimpleLock sendLock = new SimpleLock();
        private readonly IWebSocket webSocket;

        public WebSocketClient(IWebSocket webSocket)
        {
            this.webSocket = webSocket;

            webSocket.BeginReceiveMessage(HandleBeginReceiveMessageCompleted, null);
        }

        public event EventHandler<EventArgs> ConnectionClosed;

        public event EventHandler<EventArgs> ConnectionAborted;

        public event EventHandler<EventArgs> NewMessagesAvailable;

        public bool Connected
        {
            get { return webSocket.Connected; }
        }

        public IRequest Request
        {
            get { return webSocket.Request; }
        }

        public void Dispose()
        {
            webSocket.Dispose();
        }

        public void Close()
        {
            webSocket.Close();
        }

        public void SendMessage(string message)
        {
            outgoingMessages.Enqueue(message);
            ProcessOutgoingMessages();
        }

        public string GetNewMessageOrDefault()
        {
            string message;
            incomingMessages.TryDequeue(out message);
            return message;
        }

        public IEnumerable<string> GetNewMessages()
        {
            List<string> newClients = null;

            string socketSession;
            while (incomingMessages.TryDequeue(out socketSession))
            {
                if (newClients == null)
                {
                    newClients = new List<string>();
                }

                newClients.Add(socketSession);
            }

            return newClients ?? Enumerable.Empty<string>();
        }

        private void ProcessOutgoingMessages()
        {
            while (outgoingMessages.Count > 0 && !sendLock.Locked)
            {
                if (sendLock.TryAcquire())
                {
                    string message;
                    if (outgoingMessages.TryDequeue(out message))
                    {
                        SendMessageInternal(message);
                    }
                    else
                    {
                        sendLock.Release();
                    }
                }
            }
        }

        private void SendMessageInternal(string message)
        {
            webSocket.BeginSendMessage(message, HandleSendMessageCompleted, null);
        }

        private void HandleSendMessageCompleted(IAsyncResult ar)
        {
            try
            {
                webSocket.EndSendMessage(ar);
                sendLock.Release();
                ProcessOutgoingMessages();
            }
            catch (Exception)
            {
                webSocket.Abort();
                OnConnectionAborted();
            }
        }

        private void HandleBeginReceiveMessageCompleted(IAsyncResult ar)
        {
            try
            {
                string message = webSocket.EndReceiveMessage(ar);
                if (message == null)
                {
                    OnConnectionClosed();
                }
                else
                {
                    incomingMessages.Enqueue(message);
                    OnNewMessagesAvailable();
                    webSocket.BeginReceiveMessage(HandleBeginReceiveMessageCompleted, null);
                }
            }
            catch (Exception)
            {
                webSocket.Abort();
                OnConnectionAborted();
            }
        }

        private void OnNewMessagesAvailable()
        {
            EventHandler<EventArgs> newMessagesAvailable = NewMessagesAvailable;
            if (newMessagesAvailable != null)
            {
                newMessagesAvailable(this, EventArgs.Empty);
            }
        }

        private void OnConnectionClosed()
        {
            EventHandler<EventArgs> connectionClosed = ConnectionClosed;
            if (connectionClosed != null)
            {
                connectionClosed(this, EventArgs.Empty);
            }
        }

        private void OnConnectionAborted()
        {
            EventHandler<EventArgs> connectionAborted = ConnectionAborted;
            if (connectionAborted != null)
            {
                connectionAborted(this, EventArgs.Empty);
            }
        }
    }
}
