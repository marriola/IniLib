using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IniLib.Wrappers
{
    public class ConfigurationWrapper
    {
        private Options _options;
        private Configuration.Configuration _state;

        private ConfigurationWrapper() { }

        /// <summary>
        /// Instantiates a new configuration.
        /// </summary>
        /// <param name="options">Optional. Overrides the default configuration options.</param>
        /// <param name="configuration">Optional. A configuration.</param>
        public ConfigurationWrapper(Options options = null, Configuration.Configuration configuration = null)
        {
            _options = options ?? Options.defaultOptions;
            _state = configuration ?? Configuration.empty;
        }

        /// <summary>
        /// Instantiates a new configuration.
        /// </summary>
        /// <param name="configuration">Optional. A configuration.</param>
        /// <param name="options">Optional. Overrides the default configuration options.</param>
        public ConfigurationWrapper(Configuration.Configuration configuration = null, Options options = null)
            : this(options, configuration)
        { }

        /// <summary>
        /// Gets a section.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <returns>The last inserted value of the key</returns>
        public KeyMapWrapper this[string section]
        {
            get => new KeyMapWrapper(_options, _state, section, Configuration.tryGetSection(section, _state)?.Value.Item1, this.ReplaceState);
        }

        /// <summary>
        /// Removes a section from the configuration.
        /// </summary>
        /// <param name="sectionName">The name of the section to remove.</param>
        public void RemoveSection(string sectionName)
        {
            _state = Configuration.removeSection(_options, sectionName, _state);
        }

        /// <summary>
        /// Reads a configuration file from a string.
        /// </summary>
        /// <param name="text">The text of a configuration file.</param>
        /// <param name="options">Optional. The options to use to read the configuration file.</param>
        /// <returns>A new ConfigurationWrapper of the file.</returns>
        public static ConfigurationWrapper FromText(string text, Options options = null)
        {
            options = options ?? Options.defaultOptions;
            return new ConfigurationWrapper(options, Configuration.fromText(options, text));
        }

        /// <summary>
        /// Reads a configuration file from a file.
        /// </summary>
        /// <param name="path">A path to configuration file.</param>
        /// <param name="options">Optional. The options to use to read the configuration file.</param>
        /// <returns>A new ConfigurationWrapper of the file.</returns>
        public static ConfigurationWrapper FromFile(string path, Options options = null)
        {
            options = options ?? Options.defaultOptions;
            return new ConfigurationWrapper(options, Configuration.fromFile(options, path));
        }

        /// <summary>
        /// Reads a configuration file from a stream.
        /// </summary>
        /// <param name="stream">A configuration file stream.</param>
        /// <param name="options">Optional. The options to use to read the configuration file.</param>
        /// <returns>A new ConfigurationWrapper of the file.</returns>
        public static ConfigurationWrapper FromStream(Stream stream, Options options = null)
        {
            options = options ?? Options.defaultOptions;
            return new ConfigurationWrapper(options, Configuration.fromStream(options, stream));
        }

        /// <summary>
        /// Reads a configuration file from a stream reader.
        /// </summary>
        /// <param name="textReader">A configuration file stream reader.</param>
        /// <param name="options">Optional. The options to use to read the configuration file.</param>
        /// <returns>A new ConfigurationWrapper of the file.</returns>
        public static ConfigurationWrapper FromStreamReader(StreamReader textReader, Options options = null)
        {
            options = options ?? Options.defaultOptions;
            return new ConfigurationWrapper(options, Configuration.fromStreamReader(options, textReader));
        }

        /// <summary>
        /// Reads a configuration file from a text reader.
        /// </summary>
        /// <param name="textReader">A configuration file text reader.</param>
        /// <param name="options">Optional. The options to use to read the configuration file.</param>
        /// <returns>A new ConfigurationWrapper of the file.</returns>
        public static ConfigurationWrapper FromTextReader(TextReader textReader, Options options = null)
        {
            options = options ?? Options.defaultOptions;
            return new ConfigurationWrapper(options, Configuration.fromTextReader(options, textReader));
        }

        /// <summary>
        /// Gets the section and its node in the syntax tree.
        /// </summary>
        /// <param name="sectionName">The name of the section to get.</param>
        /// <returns>A tuple of <see cref="IniLib.Wrapppers.KeyMapWrapper"/> and <see cref="IniLib.Node"/>.</returns>
        public Tuple<KeyMapWrapper, List<Node>> TryGetSectionNode (string sectionName)
        {
            var result = Configuration.tryGetSection(sectionName, _state);
            if (result == null)
            {
                return null;
            }
            else
            {
                var keyMap = new KeyMapWrapper(_options, _state, sectionName, result.Value.Item1, this.ReplaceState);
                return Tuple.Create(keyMap, result.Value.Item2.ToList());
            }
        }

        public ICollection<string> GetSections() => _state.Item2.Item.Keys;

        /// <summary>
        /// Converts the configuration back to the original format.
        /// </summary>
        /// <returns>Configuration file content in a string.</returns>
        public override string ToString()
        {
            return Configuration.toText(_options, _state);
        }

        /// <summary>
        /// Writes the configuration to a file.
        /// </summary>
        /// <param name="path">The file path to write to.</param>
        public void WriteToFile(string path, Options options = null)
        {
            Configuration.writeToFile(options ?? _options, path, _state);
        }

        /// <summary>
        /// Writes the configuration to a stream writer.
        /// </summary>
        /// <param name="streamWriter">The stream writer to write to.</param>
        public void WriteToStreamWriter(StreamWriter streamWriter, Options options = null)
        {
            Configuration.writeToStreamWriter(options ?? _options, streamWriter, _state);
        }

        /// <summary>
        /// Writes the configuration to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="encoding">The encoding to output. Defaults to <see cref="System.Text.Encoding.UTF8"/></param>
        public void WriteToStream(Stream stream, Encoding encoding, Options options = null)
        {
            Configuration.writeToStream(options ?? _options, encoding ?? Encoding.UTF8, stream, _state);
        }

        private void ReplaceState(Configuration.Configuration newState)
        {
            _state = newState;
        }
    }
}
