using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkupParser.Nodes
{
	public class CommentNode : Node
	{
		public CommentNode(string value)
			: base(NodeType.COMMENT_NODE, "#comment", value)
		{
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			return string.Format("<!-- {0} -->", Value);
		}
	}
}
