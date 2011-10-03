using System.Collections.Generic;
using DotNetWebSocket.Collections;

namespace DotNetWebSocket
{
    internal class Request : IRequest
    {
        private readonly string resource;
        private readonly IFieldCollection cookies;
        private readonly IFieldCollection allFields;

        public Request(string resource, IEnumerable<KeyValuePair<string, string>> cookies, IEnumerable<KeyValuePair<string, string>> allFields)
        {
            this.resource = resource;
            this.cookies = new FieldCollection(cookies);
            this.allFields = new FieldCollection(allFields);
        }

        public IFieldCollection AllFields
        {
            get { return allFields; }
        }

        public string Resource
        {
            get { return resource; }
        }

        public IFieldCollection Cookies
        {
            get { return cookies; }
        }
    }
}