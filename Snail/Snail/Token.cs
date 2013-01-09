using System;
using System.Linq;
using System.Text;

namespace Snail
{
	public interface IToken
	{
		int Index { get; }
	}

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
	public struct Token2 : IToken
	{
		private readonly TokenList m_list;
		private readonly int m_index;
		private readonly TokenType m_type;

		public Token2(TokenList list, int index, TokenType type)
		{
			m_list = list;
			m_index = index;
			m_type = type;
		}

		public int Index
		{
			get { return m_index; }
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
