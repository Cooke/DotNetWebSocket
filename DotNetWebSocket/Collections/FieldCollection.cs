using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotNetWebSocket.Collections
{
    internal class FieldCollection : IFieldCollection
    {
        private readonly Dictionary<string, string> fields;

        public FieldCollection(IEnumerable<KeyValuePair<string, string>> fields)
        {
            this.fields = fields.ToDictionary(x => x.Key, x => x.Value);
        }

        public string this[string index]
        {
            get { return fields[index]; }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(string key)
        {
            return fields.ContainsKey(key);
        }
    }
}