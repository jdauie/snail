using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
{
	public class TextNode : Node
	{
		public TextNode(string value)
			: base(NodeType.TEXT_NODE, "#text", value)
		{
		}

		public override string StringRepresentation
		{
			get { return Value; }
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			var value = Value;
			if(mode == WhitespaceMode.Strip) {
				value = value.Trim();
			} else if(mode == WhitespaceMode.Insert) {
				value = value.Trim();
				if(value.Length > 0) {
					value = string.Format("\n{0}{1}", indentation.Repeat(currentDepth - 1), value.Trim());
				}
			}
			return value;
		}
	}
}
