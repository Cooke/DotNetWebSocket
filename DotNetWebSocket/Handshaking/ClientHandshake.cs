using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetWebSocket.Handshaking
{
    internal class ClientHandshake
    {
        private readonly string resource;
        private readonly Dictionary<string, string> allFields;
        private readonly string key;
        private readonly string host;
        private readonly string origin;
        private readonly int version;
        private readonly IEnumerable<KeyValuePair<string, string>> cookies;
        private readonly string upgrade;
        private readonly string connection;

        private ClientHandshake(byte[] clientHandshake, int size)
        {
            try
            {
                var handshake = Encoding.UTF8.GetString(clientHandshake, 0, size);
                var strings = handshake.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                resource = ParseResource(strings[0]);
                allFields = ParseFields(strings.Skip(1));
                cookies = allFields.ContainsKey("cookie") ? ParseCookies(allFields["cookie"]) : Enumerable.Empty<KeyValuePair<string, string>>();
                key = allFields["sec-websocket-key"];
                host = allFields["host"];
                upgrade = allFields["upgrade"];
                connection = allFields["connection"];
                allFields.TryGetValue("origin", out origin);
                version = int.Parse(allFields["sec-websocket-version"]);
            }
            catch (Exception ex)
            {
                throw new HandshakeException("Malformed client handshake message", ex);
            }

            if (!Upgrade.Equals("websocket", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new HandshakeException("Client handshake has incorrect upgrade field");
            }

            if (!Connection.Equals("Upgrade", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new HandshakeException("Client handshake has incorrect connection field");
            }
        }

        public string Resource
        {
            get { return resource; }
        }

        public string Key
        {
            get { return key; }
        }

        public string Host
        {
            get { return host; }
        }

        public string Origin
        {
            get { return origin; }
        }

        public int Version
        {
            get { return version; }
        }

        public IEnumerable<KeyValuePair<string, string>> Cookies
        {
            get { return cookies; }
        }

        public string Upgrade
        {
            get { return upgrade; }
        }

        public string Connection
        {
            get { return connection; }
        }

        public IEnumerable<KeyValuePair<string, string>> AllFields
        {
            get 
            {
                return allFields;
            }
        }

        public static ClientHandshake Parse(byte[] buffer, int count)
        {
            return new ClientHandshake(buffer, count);
        }

        private static Dictionary<string, string> ParseFields(IEnumerable<string> lines)
        {
            var fields = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var split = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
                fields.Add(split[0].ToLowerInvariant(), split[1]);
            }

            return fields;
        }

        private static string ParseResource(string firstLine)
        {
            var match = Regex.Match(firstLine, @"^GET (/.*) HTTP");
            if (!match.Success)
            {
                throw new HandshakeException("Failed to parse resource from client hanshake");
            }

            return match.Groups[1].Value;
        }

        private static IEnumerable<KeyValuePair<string, string>> ParseCookies(string field)
        {
            var cookies = new Dictionary<string, string>();

            var cookieKeyValuePairs = field.Split(';');            
            foreach (var cookieKeyValuePair in cookieKeyValuePairs)
            {
                var keyValueArray = cookieKeyValuePair.Split('=');
                cookies.Add(keyValueArray[0].TrimStart(), keyValueArray[1]);
            }

            return cookies;
        }
    }
}