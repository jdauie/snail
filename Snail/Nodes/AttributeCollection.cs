using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Nodes
{
	public class AttributeCollection : IEnumerable<NodeAttribute>, IComparable<AttributeCollection>
	{
		private readonly List<KeyValuePair<string, string>> m_attributes;
		//private readonly SortedDictionary<string, string> m_attributes;

		//public string this[string key]
		//{
		//    get { return m_attributes[key]; }
		//}

		public AttributeCollection()
		{
			//m_attributes = new SortedDictionary<string, string>();
			m_attributes = new List<KeyValuePair<string, string>>();
		}

		public void Add(string name, string value)
		{
			//m_attributes.Add(name, value);
			m_attributes.Add(new KeyValuePair<string, string>(name, value));
		}

		public IEnumerator<NodeAttribute> GetEnumerator()
		{
			return m_attributes.Select(kvp => new NodeAttribute(kvp)).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int CompareTo(AttributeCollection other)
		{
			int value = 0;

			//foreach (var kvp in m_attributes)
			//{
			//    if (other.m_attributes.ContainsKey(kvp.Key))
			//    {
			//        //
			//    }
			//    else
			//    {
			//        //
			//    }
			//}

			return value;
		}
	}
}
