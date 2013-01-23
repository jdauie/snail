using System;
using System.Linq;
using System.Text;

namespace Snail
{
	/// <summary>
	/// Tag (opening tag only)
	/// *2 bits reserved for possible namespace flag
	/// 
	///             [ 30 ][ 4 ][ 11 ][ 9 ][ 2 ][ 8 ]
	/// { index   } __/    /     /    /    /    /
	/// { type    } ______/     /    /    /    /
	/// { qname   } ___________/    /    /    /
	/// { prefix  } _______________/    /    /
	/// { ?       } ___________________/    /
	/// { depth   } _______________________/
	///
	/// Declaration (DOCTYPE, ELEMENT, ENTITY, ATTLIST)
	/// *Inline (nested) DTD parsing not yet supported.
	/// 
	///             [ 30 ][ 4 ][ 9 ][ 13 ][ 8 ]
	/// { index   } __/    /    /     /    /
	/// { type    } ______/    /     /    /
	/// { name    } __________/     /    /
	/// { length  } _______________/    /
	/// { depth   } ___________________/
	///
	/// Processing Instruction
	/// 
	///             [ 30 ][ 4 ][ 9 ][ 13 ][ 8 ]
	/// { index   } __/    /    /     /    /
	/// { type    } ______/    /     /    /
	/// { target  } __________/     /    /
	/// { content } _______________/    /
	/// { depth   } ___________________/
	///
	/// Comment, CDATA, Text
	/// 
	///             [ 30 ][ 4 ][ 22 ][ 8 ]
	/// { index   } __/    /     /    /
	/// { type    } ______/     /    /
	/// { length  } ___________/    /
	/// { depth   } _______________/
	/// </summary>

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
			return TokenList.Text.SubstringTrim(Index, m_qName);
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

	public class TokenAttr : TokenBase
	{
		private readonly int m_qName;
		private readonly int m_prefix;
		private readonly int m_value;

		public TokenAttr(TokenList list, int index, int qName, int prefix, int value)
			: base(list, index, TokenType.Attribute)
		{
			m_qName = qName;
			m_prefix = prefix;
			m_value = value;
		}

		public override string ToString()
		{
			// currently, does not include value index
			return TokenList.Text.SubstringTrim(Index, m_qName);
		}
	}
}
