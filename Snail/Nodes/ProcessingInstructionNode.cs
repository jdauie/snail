using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkupParser.Nodes
{
	public class ProcessingInstructionNode : Node
	{
		/// <summary>
		/// todo use a good "#name" for this node
		/// </summary>
		/// <param name="value"></param>
		public ProcessingInstructionNode(string value)
			: base(NodeType.PROCESSING_INSTRUCTION_NODE, "#processing", value)
		{
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			var value = string.Format("<?{0}?>", Name);
			if(mode == WhitespaceMode.Insert) {
				value = string.Format("\n{0}{1}", indentation.Repeat(currentDepth - 1), value);
			}
			return value;
		}
	}
}
