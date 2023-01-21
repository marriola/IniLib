using System;

namespace IniLib.Wrappers
{
    public class NodeWrapper
    {
        private Node _node;
        private Options _options;
        private Configuration.Configuration _state = Configuration.empty;
        private Action<Configuration.Configuration> _replaceState;

        /// <summary>
        /// Gets the wrapped node.
        /// </summary>
        public Node Node => _node;

        private NodeWrapper() { }

        internal NodeWrapper(
            Node node,
            Options options,
            Configuration.Configuration state,
            Action<Configuration.Configuration> replaceState)
        {
            _node = node;
            _options = options;
            _state = state;
            _replaceState = replaceState;
        }

        /// <summary>
        /// Removes the node from the configuration.
        /// </summary>
        public void Remove()
        {
            _state = Configuration.removeNode(_options, _node, _state);
            _replaceState(_state);
        }

        /// <summary>
        /// Adds a comment adjacent to the node.
        /// </summary>
        /// <param name="commentPosition">The position relative to the node to place the comment.</param>
        /// <param name="text">The comment text.</param>
        public void AddComment(CommentPosition commentPosition, string text)
        {
            _state = Configuration.addComment(commentPosition, _options, _node, text, _state);
            _replaceState(_state);
        }

        public override string ToString()
        {
            return Node.toText(_options, _node);
        }
    }
}
