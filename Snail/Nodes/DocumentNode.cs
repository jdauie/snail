using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
{
	public class DocumentNode : ElementNode
	{
		public DocumentNode()
			: base(NodeType.DOCUMENT_NODE, "#document", null)
		{
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			var value = new StringBuilder();
			foreach (var node in Children)
				value.Append(node.ToFormattedString(mode, indentation, currentDepth + 1));

			return value.ToString();
		}
	}
}
