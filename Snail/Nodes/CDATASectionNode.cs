using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkupParser.Nodes
{
	public class CDATASectionNode : Node
	{
		public CDATASectionNode(string value)
			: base(NodeType.CDATA_SECTION_NODE, "#cdata-section", value)
		{
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			// todo add indent if necessary
			return string.Format("<![CDATA[{0}]]>", Value);
		}
	}
}
