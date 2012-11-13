using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkupParser
{
	/// <summary>
	/// List allows too much, but whatever (for now).
	/// </summary>
	class TokenList
	{
		private readonly List<Token> m_list;
		private readonly StringBuilder m_current;

		public List<Token> Tokens
		{
			get { return m_list; }
		}

		public Token Current
		{
			get
			{
				if (m_list.Count == 0)
					return default(Token);
				return m_list[m_list.Count - 1];
			}
		}

		public Token Previous
		{
			get
			{
				if (m_list.Count < 2)
					return default(Token);
				return m_list[m_list.Count - 2];
			}
			set
			{
				if (m_list.Count < 2)
					throw new Exception("bad index");
				m_list[m_list.Count - 2] = value;
			}
		}

		public Token this[int index]
		{
			get { return m_list[index]; }
			set { m_list[index] = value; }
		}

		public TokenList()
		{
			m_list = new List<Token>();
			m_current = new StringBuilder();
		}

		public void Create(TokenType type)
		{
			FinalizeCurrent();
			m_list.Add(new Token(type));
		}

		public void Create(string value)
		{
			FinalizeCurrent();
			m_list.Add(new Token(value));
		}

		public void Create(char c)
		{
			FinalizeCurrent();
			m_current.Append(c);
		}

		public void AppendToCurrent(char c)
		{
			m_current.Append(c);
		}

		public void FinalizeCurrent()
		{
			if (m_current.Length > 0)
			{
				m_list.Add(new Token(m_current.ToString()));
				m_current.Clear();
			}
		}

		public override string ToString()
		{
			return string.Format("{0}", m_list.Count);
		}
	}
}
