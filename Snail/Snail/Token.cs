using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail
{
	/// <summary>
	/// THESE ARE EXTREMELY EXPENSIVE
	/// Making it a struct helps for ot.xml
	/// </summary>
	struct Token
	{
		public readonly TokenType Type;
		public readonly string Value;

		public Token(TokenType type)
		{
			Type = type;
			Value = null;
		}

		public Token(string value)
		{
			Type = TokenType.CDATA;
			Value = value;
		}

		public override string ToString()
		{
			if (Type == TokenType.CDATA)
				return Value;
			
			return Config.Tokens[Type];
		}
	}
}
