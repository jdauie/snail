using System;
using System.Linq;
using System.Text;

namespace Snail
{
	/// <summary>
	/// token    = [         64        ]
	///          = [  30  ][  4  ][ 30 ]
	///             index   type   @1
	/// 
	/// @1(attr) = [  11  ][   9   ][  10  ]
	///             qname   prefix   value
	/// 
	/// @1(node) = [ 22 ][  8  ]
	///             @2   depth
	/// 
	/// @2(decl),
	/// @2(tag)  = [  11  ][   9   ][  2  ]
	///             qname   prefix    ?
	/// 
	/// @2(proc) = [  9  ][   13   ]
	///             target  content
	/// 
	/// @2(comment),
	/// @2(cdata),
	/// @2(text) = [  22  ]
	///             length
	/// </summary>
	public interface IToken
	{
		int Index { get; }
	}

	public abstract class TokenBase : IToken
	{
		private readonly TokenList m_list;
		private readonly int m_index;
		private readonly TokenType m_type;

		protected TokenBase(TokenList list, int index, TokenType type)
		{
			m_list = list;
			m_index = index;
			m_type = type;
		}

		protected TokenList TokenList
		{
			get { return m_list; }
		}

		public int Index
		{
			get { return m_index; }
		}

		public abstract override string ToString();
	}

	public class TokenTag : TokenBase
	{
		private readonly int m_qName;
		private readonly int m_prefix;
		private readonly byte m_depth;

		public TokenTag(TokenList list, int index, int qName, int prefix, byte depth)
			: base(list, index, TokenType.OpeningTag)
		{
			m_qName = qName;
			m_prefix = prefix;
			m_depth = depth;
		}

		public override string ToString()
		{
			return TokenList.Text.SubstringTrim(Index + 1, m_qName);
		}
	}

	public class TokenRegion : TokenBase
	{
		private readonly int m_length;
		private readonly byte m_depth;

		public TokenRegion(TokenList list, int index, TokenType type, int length, byte depth)
			: base(list, index, type)
		{
			m_length = length;
			m_depth = depth;
		}

		public override string ToString()
		{
			return TokenList.Text.SubstringTrim(Index, m_length);
		}
	}


	public struct Token
	{
		private readonly TokenList m_list;

		private readonly int m_index;
		private readonly int m_length;
		private readonly int m_depth;
		private readonly TokenType m_type;

		public int Index
		{
			get { return m_index; }
		}

		public Token(TokenList list, int index, int length, int depth, TokenType type)
		{
			m_list = list;

			m_index = index;
			m_length = length;
			m_depth = depth;
			m_type = type;
		}

		public override string ToString()
		{
			return m_list.Text.SubstringTrim(m_index, m_length);
		}
	}
}
