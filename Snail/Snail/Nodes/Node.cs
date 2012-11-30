using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
{
	public enum WhitespaceMode
	{
		Leave,
		Strip,
		Insert
	}

	/// <summary>
	/// IDL values from http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/idl-definitions.html
	/// </summary>
	public enum NodeType : byte
	{
		UNKNOWN = 0,
		ELEMENT_NODE,
		ATTRIBUTE_NODE,
		TEXT_NODE,
		CDATA_SECTION_NODE,
		ENTITY_REFERENCE_NODE,
		ENTITY_NODE,
		PROCESSING_INSTRUCTION_NODE,
		COMMENT_NODE,
		DOCUMENT_NODE,
		DOCUMENT_TYPE_NODE,
		DOCUMENT_FRAGMENT_NODE,
		NOTATION_NODE,

		DTD_ENTITY_NODE,
		DTD_ELEMENT_NODE,
		DTD_ATTLIST_NODE
	}

	public abstract class Node : IComparable<Node>
	{
		private readonly NodeType m_type;
		private readonly string m_name;
		private readonly string m_value;
		private ElementNode m_parent;

		public NodeType Type
		{
			get { return m_type; }
		}

		public string Name
		{
			get { return m_name; }
		}

		public string Value
		{
			get { return m_value; }
		}

		public ElementNode Parent
		{
			get { return m_parent; }
			set { m_parent = value; }
		}

		protected Node(NodeType type, string name, string value)
		{
			m_type = type;
			m_name = name;
			m_value = value;

			m_parent = null;
		}

		public virtual int CompareTo(Node other)
		{
			int value = m_type.CompareTo(other.m_type);
			if (value == 0)
			{
				if (m_value != null)
				{
					value = m_value.CompareTo(other.m_value);
				}
				else
				{
					value = m_name.CompareTo(other.m_name);
				}
			}

			return value;
		}

		protected abstract string ToString(WhitespaceMode mode, string indentation, int currentDepth);

		public string ToFormattedString(WhitespaceMode mode = WhitespaceMode.Leave, string indentation = null, int currentDepth = 0)
		{
			return ToString(mode, indentation, currentDepth);
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
