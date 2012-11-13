using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
{
	public class DocumentTypeNode : Node
	{
		private readonly string m_publicIdentifier;
		private readonly string m_systemIdentifier;

		public DocumentTypeNode(string name, string publicIdentifier, string systemIdentifier)
			: base(NodeType.DOCUMENT_TYPE_NODE, name, null)
		{
			m_publicIdentifier = publicIdentifier;
			m_systemIdentifier = systemIdentifier;
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			var value = string.Format("<!DOCTYPE {0} PUBLIC \"{1}\" \"{2}\" >", Name, m_publicIdentifier, m_systemIdentifier);
			if (mode == WhitespaceMode.Insert)
			{
				value = string.Format("\n{0}{1}", indentation.Repeat(currentDepth - 1), value);
			}
			return value;
		}
	}
}
