using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Common
{
    public class QueryString : IDictionary<string, string>
    {
        private readonly Uri _uri;
        private readonly Dictionary<string, string> _queryTable;

        public QueryString(string url)
        {
            this._uri = new Uri(url);
            this._queryTable = new Dictionary<string, string>();
            this.Breakdown(url);
        }

        private void Breakdown(string url)
        {
            // grab the query substring from the url
            string queryToken = "?";
            var tokens = queryToken.Split(new string[] { queryToken }, StringSplitOptions.RemoveEmptyEntries);

            // grab queries
            string queryString = tokens.Last();
            var queries = queryString.Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var query in queries)
            {
                tokens = query.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                string queryKey = tokens[0];
                string queryValue = tokens[1];
                this._queryTable.Add(queryKey, queryValue);
            }
        }

        public void Add(string key, string value)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            return _queryTable.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return _queryTable.Keys; }
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out string value)
        {
            return _queryTable.TryGetValue(key, out value);
        }

        public ICollection<string> Values
        {
            get { return _queryTable.Values;  }
        }

        public string this[string key]
        {
            get
            {
                return _queryTable[key];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _queryTable.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _queryTable.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _queryTable.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _queryTable.GetEnumerator();
        }
    }
}
