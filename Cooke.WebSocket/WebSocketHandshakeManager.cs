using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Cooke.WebSocket.Utils;

namespace Cooke.WebSocket
{
    public class WebSocketHandshakeManager : IDisposable
    {
        private Regex resourcePattern;
        private Regex originPattern;
        private Regex hostnamePattern;
        private int timeout = 3000;
        private Queue<HandshakeState> timeoutQueue = new Queue<HandshakeState>();
        private object timeoutQueueLock = new object();
        private Timer timeoutTimer;
        private MD5 md5 = MD5.Create();

        public WebSocketHandshakeManager(string origin, string hostname, string resource)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                throw new ArgumentException("Origin argument can not be null, empty or white space. Use another constructor to accept connections from arbitrary locations.", "origin");
            }

            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentException("Hostname argument can not be null, empty or white space. Use another constructor to accept connection to arbitrary hostnames.", "hostname");
            }

            if (string.IsNullOrWhiteSpace(resource))
            {
                throw new ArgumentException("Resource argument can not be null, empty or white space. Use another constructor to accept connection to arbitrary hostnames.", "resource");
            }

            var tempOrigin = new Regex("^" + Regex.Escape(origin) + "$", RegexOptions.IgnoreCase);
            var tempHostname = new Regex("^" + Regex.Escape(hostname) + "$", RegexOptions.IgnoreCase);
            var tempResource = new Regex("^" + Regex.Escape(resource) + "$", RegexOptions.IgnoreCase);
            InitializeMembers(tempOrigin, tempHostname, tempResource);
        }

        public WebSocketHandshakeManager(Regex origin, Regex hostname, Regex resource)
        {
            if (origin == null)
            {
                throw new ArgumentNullException("origin");
            }

            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }

            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }

            InitializeMembers(origin, hostname, resource);
        }

        public WebSocketHandshakeManager()
        {
            InitializeMembers(new Regex(string.Empty), new Regex(string.Empty), new Regex(string.Empty));
        }

        public int Timeout
        {
            get { return timeout; }
        }

        private void InitializeMembers(Regex origin, Regex hostname, Regex resource)
        {
            resourcePattern = resource;
            originPattern = origin;
            hostnamePattern = hostname;
            timeoutTimer = new Timer(TimeoutElapsed);            
            timeoutQueue = new Queue<HandshakeState>();
            timeoutQueueLock = new object();
            md5 = MD5.Create();
            timeout = 5000;
        }

        public IAsyncResult BeginHandshake(Socket socket, AsyncCallback asyncCallback, object state)
        {
            // Create new handshake state object to handle the handshake process
            var handshakeState = new HandshakeState
            {
                webSocketAsyncResult = new AsyncResult<WebSocketSession>(asyncCallback, state),
                socket = socket,
                timeoutAt = DateTime.Now.AddMilliseconds(timeout)
            };

            // Add the state object to the timeout queue so that the handshake can be aborted
            // if the timeout is exceeded
            lock (timeoutQueueLock)
            {
                timeoutQueue.Enqueue(handshakeState);

                // If the state object is alone in the queue (and the queue was previously empty) 
                // the timeout timer has to be started
                if (timeoutQueue.Count == 1)
                {
                    StartTimeoutTimer(handshakeState.timeoutAt);
                }
            }

            // Start receiving the client handshake
            socket.BeginReceive(handshakeState.inputBuffer, 0, handshakeState.inputBuffer.Length, SocketFlags.None, ClientHandshakeCallback, handshakeState);

            return handshakeState.webSocketAsyncResult;
        }

        public WebSocketSession EndHandshake(IAsyncResult asyncResult)
        {
            return ((AsyncResult<WebSocketSession>)asyncResult).EndInvoke();
        }

        private void ClientHandshakeCallback(IAsyncResult ar)
        {
            var handshakeState = (HandshakeState)ar.AsyncState;

            try
            {
                // Get the number of received bytes, add to internal field and parse the received data
                int receivedBytes = handshakeState.socket.EndReceive(ar);
                handshakeState.clientHandshakeReceived += receivedBytes;
                handshakeState.ParseClientHandshake();

                // Check if the client handshake has been fully received yet
                if (handshakeState.state != HandshakeState.ClientHandshakeParseState.Finished)
                {
                    // Continue receiving the client handshake
                    handshakeState.socket.BeginReceive(handshakeState.inputBuffer, handshakeState.clientHandshakeReceived, handshakeState.inputBuffer.Length - handshakeState.clientHandshakeReceived, SocketFlags.None, ClientHandshakeCallback, handshakeState);
                }
                else
                {
                    //Console.WriteLine(UTF8Encoding.UTF8.GetString(handshakeState.inputBuffer, 0, handshakeState.receiveBufferOffset));
                    ValidateClientHandshakeAndSendServerHandshake(handshakeState);
                }
            }
            catch (ObjectDisposedException)
            {
                // The socket has been closed which is because of handshake timeout (or some external code)
                handshakeState.webSocketAsyncResult.SetCompleted(new TimeoutException("Handshake timeout exceeded"), false);
            }
            catch (Exception ex)
            {
                handshakeState.webSocketAsyncResult.SetCompleted(ex, false);
            }
        }

        private void HandshakeCallback(IAsyncResult ar)
        {
            var handshakeState = (HandshakeState)ar.AsyncState;

            try
            {
                // Get the number of sent bytes and calculate how much is left to send
                int bytesSent = handshakeState.socket.EndSend(ar);
                handshakeState.serverHandshakeSent += bytesSent;
                int bytesLeft = handshakeState.serverHandshakeSize - handshakeState.serverHandshakeSent;

                // Check if there are bytes left to send 
                if (bytesLeft > 0)
                {
                    handshakeState.socket.BeginSend(handshakeState.outputBuffer, handshakeState.serverHandshakeSent, bytesLeft, SocketFlags.None, HandshakeCallback, handshakeState);
                }
                else
                {
                    // Set the handshake state to 'finished handshake' and check that the timeout didn't kick in in the meanwhile
                    var finished = Interlocked.Exchange(ref handshakeState.finished, 1);

                    if (finished == 0)
                    {
                        var webSocketSession = new WebSocketSession(handshakeState.socket, handshakeState.inputBuffer, handshakeState.outputBuffer, handshakeState.Cookies, handshakeState.resource);
                        handshakeState.webSocketAsyncResult.SetCompleted(webSocketSession, false);
                    }
                    else
                    {
                        handshakeState.webSocketAsyncResult.SetCompleted(new TimeoutException("Handshake timeout exceeded"), false);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Socket was closed because of timeout or some external code
                handshakeState.webSocketAsyncResult.SetCompleted(new TimeoutException("Handshake timeout exceeded"), false);
            }
            catch (Exception ex)
            {
                handshakeState.webSocketAsyncResult.SetCompleted(ex, false);
            }
        }

        public void Dispose()
        {
            md5.Clear();
            timeoutTimer.Dispose();
        }

        private void TimeoutElapsed(object dummy)
        {
            // Timeout has elapsed for the first handshake in the timeout queue
            lock (timeoutQueueLock)
            {
                // Get handshake state object for the first handshake in queue
                var handshakeState = timeoutQueue.Dequeue();

                // Set the handshake to finished (finished because of timeout exceeded)
                var finished = Interlocked.Exchange(ref handshakeState.finished, 1);

                // Check that the handshake process didn't finish before the finished variable was set
                if (finished == 0)
                {
                    // Abruptly kill the connection
                    handshakeState.socket.Close();
                }

                // Start the timeout timer for the next handshake in timeout queue
                if (timeoutQueue.Count > 0)
                {
                    var nextHandshakeState = timeoutQueue.Peek();
                    StartTimeoutTimer(nextHandshakeState.timeoutAt);
                }
            }
        }

        private void ValidateClientHandshakeAndSendServerHandshake(HandshakeState handshakeState)
        {
            // Validate the client handshake and construct server response (handshake) according to protocol specification

            string resource = handshakeState.resource;
            if (!resourcePattern.IsMatch(handshakeState.resource))
            {
                var ex = new HandshakeException("Unavilable resource was requested");
                ex.Data["Resource"] = handshakeState.resource;
                throw ex;
            }

            if (!handshakeState.fields.ContainsKey("Upgrade"))
            {
                throw new HandshakeException("Missing header field 'Upgrade' in client handshake");
            }

            string upgrade = handshakeState.fields["Upgrade"];
            if (!string.Equals(upgrade, "WebSocket", StringComparison.OrdinalIgnoreCase))
            {
                throw new HandshakeException("The 'Upgrade' header field should be 'WebSocket'");
            }

            if (!handshakeState.fields.ContainsKey("Connection"))
            {
                throw new HandshakeException("Missing header field 'Connection' in client handshake");
            }

            string connection = handshakeState.fields["Connection"];
            if (!string.Equals(connection, "Upgrade", StringComparison.OrdinalIgnoreCase))
            {
                throw new HandshakeException("The 'Connection' header field should be 'Upgrade'");
            }

            if (!handshakeState.fields.ContainsKey("Host"))
            {
                throw new HandshakeException("Missing header field 'Host' in client handshake");
            }

            string hostname = handshakeState.fields["Host"];
            if (!hostnamePattern.IsMatch(hostname))
            {
                var ex = new HandshakeException("The 'Host' header field does not match accepted hostnames.");
                ex.Data["Host"] = hostname;
                ex.Data["AccepctedHostRegexPattern"] = hostnamePattern.ToString();
                throw ex;
            }

            if (!handshakeState.fields.ContainsKey("Origin"))
            {
                throw new HandshakeException("Missing header field 'Origin' in client handshake");
            }

            string origin = handshakeState.fields["Origin"];
            if (!originPattern.IsMatch(origin))
            {
                var ex = new HandshakeException("The 'Origin' header field does not match accepted origins");
                ex.Data["Origin"] = origin;
                ex.Data["AcceptedOriginRegexPattern"] = originPattern;
                throw ex;
            }

            string key1 = handshakeState.fields["Sec-WebSocket-Key1"];
            string key2 = handshakeState.fields["Sec-WebSocket-Key2"];
            byte[] key3 = handshakeState.challange;

            long keyNum1 = long.Parse(key1.Where(Char.IsDigit).Aggregate(new StringBuilder(), (res, x) => res.Append(x)).ToString());
            long keyNum2 = long.Parse(key2.Where(Char.IsDigit).Aggregate(new StringBuilder(), (res, x) => res.Append(x)).ToString());

            long spaces1 = key1.Count(x => x == ' ');
            long spaces2 = key2.Count(x => x == ' ');

            if (keyNum1 % spaces1 != 0)
            {
                throw new HandshakeException("Bognus client handshake. key-number_1 is not an integral multiple of spaces_1");
            }

            if (keyNum2 % spaces2 != 0)
            {
                throw new HandshakeException("Bognus client handshake. key-number_2 is not an integral multiple of spaces_2");
            }

            int part1 = (int)(keyNum1 / spaces1);
            int part2 = (int)(keyNum2 / spaces2);

            byte[] part1Bytes = BitConverter.GetBytes(part1);
            byte[] part2Bytes = BitConverter.GetBytes(part2);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(part1Bytes);
                Array.Reverse(part2Bytes);
            }

            var challange = new byte[16];
            Array.Copy(part1Bytes, 0, challange, 0, 4);
            Array.Copy(part2Bytes, 0, challange, 4, 4);
            Array.Copy(key3, 0, challange, 8, 8);

            var response = md5.ComputeHash(challange);

            var memStream = new MemoryStream(handshakeState.outputBuffer, true);
            var writer = new StreamWriter(memStream);
            writer.WriteLine("HTTP/1.1 101 WebSocket Protocol Handshake");
            writer.WriteLine("Upgrade: WebSocket");
            writer.WriteLine("Connection: Upgrade");
            writer.WriteLine("Sec-WebSocket-Origin: {0}", origin);
            writer.WriteLine("Sec-WebSocket-Location: ws://{0}{1}", hostname, resource);
            writer.WriteLine(string.Empty);
            writer.Flush();
            memStream.Write(response, 0, response.Length);
            handshakeState.serverHandshakeSize = (int)memStream.Position;

            handshakeState.socket.BeginSend(handshakeState.outputBuffer, 0, handshakeState.serverHandshakeSize, SocketFlags.None, HandshakeCallback, handshakeState);
        }

        private void StartTimeoutTimer(DateTime timeoutAt)
        {
            // Set the timeout timer to trigger according to timeoutAt input parameter
            // if the timeout moment has passed then trigger the callback immediatly by setting "countdown" to zero
            var timeToDue = timeoutAt - DateTime.Now;

            if (timeToDue < TimeSpan.Zero)
            {
                timeToDue = TimeSpan.Zero;
            }

            // Always set the period to -1 (never reoccur)
            timeoutTimer.Change(timeToDue, TimeSpan.FromMilliseconds(-1));
        }
    }
}
