using System;
using System.Linq;
using System.Text;

namespace Snail
{
	public struct Token
	{
		private readonly string m_text;

		private readonly int m_index;
		private readonly int m_length;
		private readonly int m_depth;
		private readonly TokenType m_type;

		public int Index
		{
			get { return m_index; }
		}

		public Token(string text, int index, int length, int depth, TokenType type)
		{
			m_text = text;

			m_index  = index;
			m_length = length;
			m_depth  = depth;
			m_type   = type;
		}

		public override string ToString()
		{
			return m_text.SubstringTrim(m_index, m_length);
		}
	}
}
