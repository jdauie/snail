using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
{
	public class CommentNode : Node
	{
		public CommentNode(string value)
			: base(NodeType.COMMENT_NODE, "#comment", value)
		{
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			// todo add indent if necessary
			return string.Format("<!-- {0} -->", Value);
		}
	}
}
