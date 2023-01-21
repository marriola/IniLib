using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IniLib.Wrappers
{
    public class KeyMapWrapper
    {
        private Options _options;
        private Configuration.Configuration _state;
        private string _sectionName;
        private Configuration.KeyMap _keyMap;
        private Action<Configuration.Configuration> _replaceState;

        /// <summary>
        /// Gets or sets the last inserted value of a key.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <returns>The last value of the key.</returns>
        public string this[string key]
        {
            get => Configuration.get(_sectionName, key, _state);
            set => _replaceState(Configuration.add(_options, _sectionName, key, value, _state));
        }

        /// <summary>
        /// Gets the number of keys contained in the section.
        /// </summary>
        public int Count => _keyMap.Item.Count;

        /// <summary>
        /// Gets an <c>ICollection&lt;string&gt;</c> containing the keys in the section.
        /// </summary>
        public ICollection<string> Keys => _keyMap.Item.Keys.ToList();

        /// <summary>
        /// Gets an <c>ICollection&lt;string&gt;</c> containing the values in the section.
        /// </summary>
        public ICollection<string> Values => _keyMap.Item.Values.Select(pairs => pairs.Select(t => t.Item1).FirstOrDefault()).ToList();

        private KeyMapWrapper() { }

        internal KeyMapWrapper(
            Options options,
            Configuration.Configuration configuration,
            string sectionName,
            Configuration.KeyMap keyMap,
            Action<Configuration.Configuration> replaceState)
        {
            _options = options;
            _state = configuration;
            _sectionName = sectionName;
            _keyMap = keyMap;
            _replaceState = replaceState;
        }

        /// <summary>
        /// Adds or replaces the value of a key.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <param name="value">The new value.</param>
        public void Add(string key, string value) => _replaceState(Configuration.add(_options, _sectionName, key, value, _state));
        
        /// <summary>
        /// Determines whether the section contains a key with the specified name.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <returns><c>true</c> if the key exists in the section, otherwise <c>false</c>.</returns>
        public bool ContainsKey(string key) => _keyMap.Item.ContainsKey(key);

        /// <summary>
        /// Removes a key from the section.
        /// </summary>
        /// <param name="key">The key name.</param>
        public void Remove(string key) => _replaceState(Configuration.removeKey(_options, _sectionName, key, _state));

        /// <summary>
        /// Gets the last integer value of a key.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="FormatException"></exception>
        public int GetInt(string key)
        {
            if (_keyMap.Item.ContainsKey(key))
            {
                return Configuration.getInt(_sectionName, key, _state);
            }
            else
            {
                throw new KeyNotFoundException(key);
            }
        }

        /// <summary>
        /// Gets the first inserted value of a key.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <returns>The first value of the key.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public string GetFirstValue(string key)
        {
            if (_keyMap.Item.ContainsKey(key))
            {
                return Configuration.getFirst(_sectionName, key, _state);
            }
            else
            {
                throw new KeyNotFoundException(key);
            }
        }

        /// <summary>
        /// Gets the first integer value of a key.
        /// </summary>
        /// <param name="key">The key name.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="FormatException"></exception>
        public int GetFirstInt(string key)
        {
            if (_keyMap.Item.ContainsKey(key))
            {
                return Configuration.getFirstInt(_sectionName, key, _state);
            }
            else
            {
                throw new KeyNotFoundException(key);
            }
        }

        /// <summary>
        /// Gets all keys by the specified name and their associated nodes in the syntax tree.
        /// </summary>
        /// <param name="keyName">The name of the key to get.</param>
        /// <returns>A list of <see cref="NodeWrapper"/></returns>
        public List<NodeWrapper> TryGetNodes(string keyName)
        {
            if (!_keyMap.Item.ContainsKey(keyName))
            {
                return null;
            }

            return _keyMap.Item[keyName]
                .Select(n => new NodeWrapper(n.Item2, _options, _state, _replaceState))
                .ToList();
        }

        /// <summary>
        /// Gets the last key by the specified name and its associated node in the syntax tree.
        /// </summary>
        /// <param name="keyName">The name of the key to get.</param>
        /// <returns>A <see cref="NodeWrapper"/></returns>
        public NodeWrapper TryGetNode(string keyName)
        {
            if (!_keyMap.Item.ContainsKey(keyName))
            {
                return null;
            }

            var (_, node) = _keyMap.Item[keyName].Last();
            return new NodeWrapper(node, _options, _state, _replaceState);
        }

        /// <summary>
        /// Gets all comments on the previous lines and on the same line as the key.
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns>A list of <see cref="NodeWrapper"/></returns>
        public List<NodeWrapper> TryGetComments(string keyName)
        {
            if (!_keyMap.Item.ContainsKey(keyName))
            {
                return null;
            }

            return Configuration.tryGetKeyComments(_sectionName, keyName, _state).Value
                .Select(x => new NodeWrapper(x.Item2, _options, _state, _replaceState))
                .ToList();
        }

        /// <summary>
        /// Gets the last inserted value of a key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the last value associated with the specified key, if the key is found;
        /// otherwise, <c>null</c>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the section contains a key with the specified name; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(string key, out string value)
        {
            if (_keyMap.Item.ContainsKey(key))
            {
                value = Configuration.get(_sectionName, key, _state);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the first inserted value of a key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the first value associated with the specified key, if the key is found;
        /// otherwise, <c>null</c>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the section contains a key with the specified name; otherwise, <c>false</c>.</returns>
        public bool TryGetFirstValue(string key, out string value)
        {
            if (_keyMap.Item.ContainsKey(key))
            {
                value = Configuration.getFirst(_sectionName, key, _state);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Get all values associated with a key.
        /// </summary>
        /// <param name="key">The key whose values to get.</param>
        /// <returns>A list of the values associated with the key.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public List<string> GetValues(string key)
        {
            if (!_keyMap.Item.ContainsKey(key))
            {
                throw new KeyNotFoundException(key);
            }

            return Configuration.getMultiValues(_sectionName, key, _state).ToList();
        }

        /// <summary>
        /// Get all values associated with a key.
        /// </summary>
        /// <param name="key">The key whose values to get.</param>
        /// <param name="value">When this method returns, a list of all values associated with the specified key, if the key is found;
        /// otherwise, <c>null</c>. This parameter is passed uninitialized.</param>
        /// <returns>A list of the values associated with the key.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public bool TryGetValues(string key, out IEnumerable<string> values)
        {
            if (!_keyMap.Item.ContainsKey(key))
            {
                values = null;
                return false;
            }

            values = Configuration.getMultiValues(_sectionName, key, _state).ToList();
            return true;
        }
    }
}
