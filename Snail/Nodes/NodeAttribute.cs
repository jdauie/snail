using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
{
	public class NodeAttribute
	{
		private readonly string m_name;
		private string m_value;

		public string Name
		{
			get { return m_name; }
		}

		public string Value
		{
			get { return m_value; }
			set { m_value = value; }
		}

		public NodeAttribute(KeyValuePair<string, string> attribute)
		{
			m_name = attribute.Key;
			m_value = attribute.Value;
		}

		public NodeAttribute(string name, string value)
		{
			m_name = name;
			m_value = value;
		}

		public override string ToString()
		{
			return string.Format("{0}=\"{1}\"", m_name, m_value);
		}
	}
}
