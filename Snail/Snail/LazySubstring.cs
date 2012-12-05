using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail
{
	class LazySubstring
	{
		private string m_actual;

		private readonly string m_underlying;
		private readonly int m_startIndex;
		private readonly int m_length;

		public string Value
		{
			get
			{
				if (m_actual == null)
					m_actual = m_underlying.Substring(m_startIndex, m_length);

				return m_actual;
			}
		}

		public LazySubstring(string s, int startIndex, int length)
		{
			m_underlying = s;
			m_startIndex = startIndex;
			m_length = length;
			m_actual = null;
		}

		public override string ToString()
		{
			return Value;
		}
	}
}
