using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
{
	public class ElementNode : Node
	{
		private readonly LinkedList<Node> m_childNodes;
		private readonly AttributeCollection m_attributes;

		public IEnumerable<Node> Children
		{
			get { return m_childNodes; }
		}

		public AttributeCollection Attributes
		{
			get { return m_attributes; }
		}

		public ElementNode(string name, bool empty)
			: base(NodeType.ELEMENT_NODE, name, null)
		{
			m_childNodes = empty ? null : new LinkedList<Node>();
			m_attributes = new AttributeCollection();
		}

		protected ElementNode(NodeType type, string name, string value)
			: base(type, name, value)
		{
			m_childNodes = new LinkedList<Node>();
			m_attributes = new AttributeCollection();
		}

		public void InsertBefore(Node node, Node newNode)
		{
			var listNode = m_childNodes.Find(node);
			if (listNode == null)
				throw new ArgumentException("node");

			m_childNodes.AddBefore(listNode, newNode);
			node.Parent = null;
			newNode.Parent = this;
		}

		public void AppendChild(Node node)
		{
			m_childNodes.AddLast(node);
			node.Parent = this;
		}

		public void RemoveChild(Node node)
		{
			m_childNodes.Remove(node);
			node.Parent = null;
		}

		public void ReplaceChild(Node node, Node newNode)
		{
			var listNode = m_childNodes.Find(node);
			if (listNode == null)
				throw new ArgumentException("node");

			m_childNodes.AddBefore(listNode, newNode);
			m_childNodes.Remove(listNode);
			node.Parent = null;
			newNode.Parent = this;
		}

		public bool HasChildNodes()
		{
			return (m_childNodes.Count > 0);
		}

		protected override string ToString(WhitespaceMode mode, string indentation, int currentDepth)
		{
			var indent = "";
			if(mode == WhitespaceMode.Insert) {
			    indent = string.Format("\n{0}", indentation.Repeat(currentDepth - 1));
			}

			var attr = new StringBuilder();
			foreach (var attribute in Attributes)
				attr.AppendFormat(" {0}", attribute);

			var value = new StringBuilder();
			if (m_childNodes == null)
			{
				value.AppendFormat("{0}<{1}{2} />", indent, Name, attr);
			}
			else
			{
				value.AppendFormat("{0}<{1}{2}>", indent, Name, attr);
				foreach (var node in m_childNodes)
					value.Append(node.ToFormattedString(mode, indentation, currentDepth + 1));
				value.AppendFormat("{0}</{1}>", indent, Name);
			}
			return value.ToString();
		}
	}
}
