using System.Collections.Generic;

namespace DotNetWebSocket.Collections
{
    public interface IFieldCollection : IEnumerable<KeyValuePair<string, string>>
    {
        string this[string index] { get; }

        bool ContainsKey(string key);
    }
}