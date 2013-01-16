using System;

namespace Snail
{
	public enum TokenBasicType : byte
	{
		Text      = 0,
		Tag       = 1,
		Special   = 2,
		Attribute = 3,
	}

	public enum TokenTextType : byte
	{
		WhiteSpace = 0,
		Normal     = 1,
	}

	public enum TokenSpecialType : byte
	{
		Comment     = 0,
		CDATA       = 1,
		Declaration = 2,
		Processing  = 3,
	}

	public enum TokenType : byte
	{
		TextWhiteSpace = TokenBasicType.Text | (TokenTextType.WhiteSpace << 2),
		TextNormal     = TokenBasicType.Text | (TokenTextType.Normal << 2),
		OpeningTag     = TokenBasicType.Tag,
		Comment        = TokenBasicType.Special | (TokenSpecialType.Comment << 2),
		CDATA          = TokenBasicType.Special | (TokenSpecialType.CDATA << 2),
		Declaration    = TokenBasicType.Special | (TokenSpecialType.Declaration << 2),
		Processing     = TokenBasicType.Special | (TokenSpecialType.Processing << 2),
		Attribute      = TokenBasicType.Attribute,
	}
}
