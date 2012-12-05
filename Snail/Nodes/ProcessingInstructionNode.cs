using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
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

		public override string StringRepresentation
		{
			get { return Value; }
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			var value = string.Format("<?{0}?>", Value);
			if(mode == WhitespaceMode.Insert) {
				value = string.Format("\n{0}{1}", indentation.Repeat(currentDepth - 1), value);
			}
			return value;
		}
	}
}
