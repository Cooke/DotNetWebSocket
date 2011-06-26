using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Cooke.WebSocket.Utils
{
    internal class HandshakeState
    {
        internal enum ClientHandshakeParseState
        {
            NotStarted,
            Failed,
            Fields,
            Challange,
            Finished
        }

        internal AsyncResult<WebSocketSession> webSocketAsyncResult;
        internal Socket socket;
        internal byte[] inputBuffer = new byte[10 * 1024];
        internal byte[] outputBuffer = new byte[10 * 1024];
        internal int clientHandshakeReceived = 0;
        internal int clientHandshakeParsed = 0;
        internal int serverHandshakeSize = 0;
        internal int serverHandshakeSent = 0;
        internal DateTime timeoutAt;
        internal int finished = 0;
        internal string resource;
        internal Dictionary<string, string> fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal ClientHandshakeParseState state = ClientHandshakeParseState.NotStarted;
        internal byte[] challange = new byte[8];

        internal void ParseClientHandshake()
        {
            // Keep track of how much that was parsed during last round of the while-loop
            var tempOffset = -1;

            // Keep parsing if last round in while-loop was "successful" 
            // AND
            // all received bytes hasnät been parsed
            // AND
            // the client's entire handshake hasn't been parsed
            while (tempOffset != clientHandshakeParsed && clientHandshakeReceived - clientHandshakeParsed != 0 && state != ClientHandshakeParseState.Finished && state != ClientHandshakeParseState.Failed)
            {
                tempOffset = clientHandshakeParsed;

                var buffer = new ArraySegment<byte>(inputBuffer, clientHandshakeParsed, clientHandshakeReceived - clientHandshakeParsed);

                switch (state)
                {
                    case ClientHandshakeParseState.NotStarted:
                        for (int i = buffer.Offset; i < buffer.Offset + buffer.Count - 1; i++)
                        {
                            // Search for CR (0x0D) LF (0x0A)
                            if (buffer.Array[i] == 0x0D && buffer.Array[i + 1] == 0x0A)
                            {
                                // Decode from UTF-8 to internal representation
                                string first = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, i - buffer.Offset);

                                // TODO improve parsing and validation of http request line
                                // Check some ad-hoc stuff to validate request line
                                if (!first.StartsWith("GET /") || (first.Length < 6) || first.Count(x => x == ' ') != 2)
                                {
                                    state = ClientHandshakeParseState.Failed;
                                    throw new Exception("Invalid client handshake");
                                }

                                // Parse out the resource part of the request string
                                resource = first.Substring(4, first.IndexOf(' ', 5) - 4);

                                // Change state and increment number of parsed bytes
                                state = ClientHandshakeParseState.Fields;
                                clientHandshakeParsed = i + 2;
                                break;
                            }
                        }

                        break;
                    case ClientHandshakeParseState.Fields:
                        // Check if a "new field" starts with CR LF then this marks the end of the fields section of the client handshake
                        if (buffer.Array[buffer.Offset] == 0x0D && buffer.Array[buffer.Offset + 1] == 0x0A)
                        {
                            state = ClientHandshakeParseState.Challange;
                            clientHandshakeParsed += 2;
                        }
                        else
                        {                            
                            for (int i = buffer.Offset; i < buffer.Offset + buffer.Count - 1; i++)
                            {
                                // Search for CR LF which marks the end of a new field
                                if (buffer.Array[i] == 0x0D && buffer.Array[i + 1] == 0x0A)
                                {
                                    // Decode UTF-8 field string to internal string representation
                                    string first = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, i - buffer.Offset);

                                    // Split field into key and value part
                                    string[] split = first.Split(new[] { ": " }, 2, StringSplitOptions.None);

                                    if (split.Length != 2)
                                    {
                                        state = ClientHandshakeParseState.Failed;
                                        throw new Exception("Invalid field in client handshake");
                                    }

                                    // Add received field to handshake state
                                    fields.Add(split[0], split[1]);
                                    clientHandshakeParsed = i + 2;
                                    break;
                                }
                            }
                        }
                        
                        break;
                    case ClientHandshakeParseState.Challange:
                        // Last 8 bytes of clienthandshake
                        if (buffer.Count >= 8)
                        {
                            Array.Copy(buffer.Array, buffer.Offset, challange, 0, 8);
                            state = ClientHandshakeParseState.Finished;
                            clientHandshakeParsed += 8;
                        }

                        break;
                    default:
                        break;
                }
            }
        }
    }
}
