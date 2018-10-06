using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ThumbnailCreator
{
    /// <summary>
    /// A custom (and more extensive) version of the configuration settings provided natively
    /// </summary>
    public class Config : IDictionary<string, string>
    {
        #region Interal data

        private SortedDictionary<string, string> data = new SortedDictionary<string, string>();

        #endregion

        #region Constructor

        public Config(string configFile) => data = ParseFile(configFile);

        #endregion

        #region Public accessor methods

        /// <summary>
        /// Retrieve a configuration property (throws exception if not found)
        /// </summary>
        /// <param name="key">Setting to read</param>
        /// <returns>The value for that setting</returns>
        public string Get(params string[] key)
        {
            string fullKey = string.Join('/', key);
            if (data.ContainsKey(fullKey))
                return data[fullKey];
            throw new KeyNotFoundException(string.Format("There was no value specified for \"{0}\"", fullKey));
        }

        /// <summary>
        /// Retrieve a configuration property, or a fallback if the key does not exist
        /// </summary>
        /// <param name="fallback">Value to return if key not present</param>
        /// <param name="key">Setting to read</param>
        /// <returns>The value for that setting (if found)</returns>
        public string GetSafe(string fallback, params string[] key)
        {
            string fullKey = string.Join('/', key);
            if (data.ContainsKey(fullKey))
                return data[fullKey];
            return fallback;
        }

        /// <summary>
        /// Try to retrieve a configuration property (returns false if not found)
        /// </summary>
        /// <param name="value">The value for that setting, if found</param>
        /// <param name="key">Setting to read</param>
        /// <returns>If the setting was present</returns>
        public bool TryGet(out string value, params string[] key)
        {
            string fullKey = string.Join('/', key);
            if (data.ContainsKey(fullKey))
            {
                value = data[fullKey];
                return true;
            }
            value = null;
            return false;
        }

        #endregion

        #region Static parsing methods

        /// <summary>
        /// Parse a config from an XElement
        /// </summary>
        /// <param name="root">The root configuration node</param>
        /// <param name="allowParent">If the parent attribute is allowed on the root</param>
        /// <param name="tree">The list of files already searched</param>
        /// <returns>The data parsed from the node</returns>
        private static SortedDictionary<string, string> ParseElement(XElement root, bool allowParent = false, List<string> tree = null)
        {
            SortedDictionary<string, string> result;

            // Ensure it's a configuration node
            if (!root.Name.LocalName.Equals("configuration"))
                throw new InvalidDataException("Config must be encapsulated by a \"configuration\" tag");

            // Check for parent file
            if (allowParent && root.HasAttributes && root.Attribute("parent") is XAttribute parent)
            {
                // Get path to parent file
                string parentFile = parent.Value.ToOS();
                if (!parentFile.ToLower().EndsWith(".config"))
                    parentFile += ".config";
                parentFile = Path.Combine(Path.GetDirectoryName(tree[0]), parentFile);

                // Check for circular references
                string fullParentFile = Path.GetFullPath(parentFile);
                if (tree.Contains(fullParentFile))
                    throw new InvalidOperationException("Circular config structure detected");
                tree.Add(fullParentFile);

                // Pull the contents of the parent file
                result = ParseFile(parentFile, tree);
            }
            else
                result = new SortedDictionary<string, string>();

            // Read the entries from this file
            var currentKeys = new HashSet<string>();
            foreach (var element in root.Elements())
            {
                if (!element.Name.LocalName.Equals("add"))
                    throw new InvalidDataException("All entries must be \"add\" tags");

                // Read the key
                string key;
                if (element.HasAttributes && element.Attribute("key") is XAttribute keyAttr)
                    key = keyAttr.Value;
                else
                    throw new InvalidDataException("Did not specify a the key");
                
                //// Validate key
                //if (key.Contains('/'))
                //    throw new InvalidDataException("Forward slashes are used to denote nested configurations and cannot be used in a key name directly");

                // Read the value
                bool isNested = element.HasAttributes && element.Attribute("nested")?.Value == "true";
                string value;
                if (element.HasAttributes && element.Attribute("value") is XAttribute valueAttr)
                    if (isNested)
                        throw new InvalidOperationException("Cannot specify both \"value\" and \"nested\"");
                    else
                        value = valueAttr.Value;
                else if (isNested)
                {
                    var children = element.Elements().ToList();
                    if (children.Count != 1)
                        throw new InvalidDataException("Must supply a child configuration when \"nested\" is specified");
                    var childData = ParseElement(children[0], false, null);
                    foreach (var entry in childData)
                    {
                        string entryKey = $"{key}/{entry.Key}";
                        if (result.ContainsKey(entryKey))
                            if (currentKeys.Contains(entryKey))
                                throw new InvalidOperationException(string.Format(
                                    "Unable to specify the same key twice (unless overriding parent value): \"{0}\"", key
                                ));
                            else
                                result.Remove(entryKey);
                        currentKeys.Add(entryKey);
                        result.Add(entryKey, entry.Value);
                    }
                    continue;
                }
                else if (!string.IsNullOrWhiteSpace(element.Value))
                    value = element.Value;
                else
                    throw new InvalidDataException("Did not specify a the value");

                // Store into the list (overwriting parent data)
                if (result.ContainsKey(key))
                    if (currentKeys.Contains(key))
                        throw new InvalidOperationException(string.Format(
                            "Unable to specify the same key twice (unless overriding parent value): \"{0}\"", key
                        ));
                    else
                        result.Remove(key);
                currentKeys.Add(key);
                result.Add(key, value);
            }

            return result;
        }

        /// <summary>
        /// Parse a config file (attempts to catch circular references)
        /// </summary>
        /// <param name="configFile">Path to config file</param>
        /// <param name="tree">List of files already searched</param>
        /// <returns>The data parsed from the file</returns>
        private static SortedDictionary<string, string> ParseFile(string configFile, List<string> tree = null)
        {
            // Read the file
            var root = XElement.Parse(File.ReadAllText(configFile).FixBOM());

            // Parse it
            if (tree == null)
                tree = new List<string>()
                {
                    Path.GetFullPath(configFile)
                };
            return ParseElement(root, true, tree);
        }

        #endregion

        #region IDictionary members

        public ICollection<string> Keys => data.Keys;
        public ICollection<string> Values => data.Values;
        public int Count => data.Count;
        public bool IsReadOnly => true;
        public string this[string key]
        {
            get => data[key];
            set => throw new InvalidOperationException("Collection is read-only");
        }

        public void Add(string key, string value) => throw new InvalidOperationException("Collection is read-only");
        public bool ContainsKey(string key) => data.ContainsKey(key);
        public bool Remove(string key) => throw new InvalidOperationException("Collection is read-only");
        public bool TryGetValue(string key, out string value) => data.TryGetValue(key, out value);
        public void Add(KeyValuePair<string, string> item) => throw new InvalidOperationException("Collection is read-only");
        public void Clear() => throw new InvalidOperationException("Collection is read-only");
        public bool Contains(KeyValuePair<string, string> item) => data.TryGetValue(item.Key, out string value) && value == item.Value;
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => data.CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<string, string> item) => throw new InvalidOperationException("Collection is read-only");
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

        #endregion
    }
}