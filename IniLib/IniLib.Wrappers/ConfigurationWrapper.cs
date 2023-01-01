using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IniLib.Wrappers
{
    public class ConfigurationWrapper
    {
        private Options _options;
        private Configuration.Configuration _state;

        /// <summary>
        /// Instantiates a new configuration.
        /// </summary>
        /// <param name="options">The configuration options.</param>
        public ConfigurationWrapper(Options options)
        {
            _options = options;
            _state = Configuration.empty;
        }

        /// <summary>
        /// Instantiates a new configuration with the default options.
        /// </summary>
        public ConfigurationWrapper()
            : this(Options.defaultOptions)
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
        /// Gets the section and its node in the syntax tree.
        /// </summary>
        /// <param name="sectionName">The name of the section to get.</param>
        /// <returns>A tuple of <see cref="IniLib.Wrapppers.KeyMapWrapper"/> and <see cref="IniLib.Node"/>.</returns>
        public Tuple<KeyMapWrapper, List<Node>> TryGetSectionNode (string sectionName)
        {
            if (Configuration.tryGetSection(sectionName, _state) is var result)
            {
                var keyMap = new KeyMapWrapper(_options, _state, sectionName, result.Value.Item1, this.ReplaceState);
                return Tuple.Create(keyMap, result.Value.Item2.ToList());
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Converts the configuration back to the original format.
        /// </summary>
        /// <returns>Configuration file content in a string.</returns>
        public override string ToString()
        {
            return Configuration.toText(_options, _state);
        }

        private void ReplaceState(Configuration.Configuration newState)
        {
            _state = newState;
        }
    }
}
