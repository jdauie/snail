using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkupParser
{
	/// <summary>
	/// IDL values from http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113/idl-definitions.html
	/// </summary>
	public enum NodeType : byte
	{
		UNKNOWN = 0,
		ELEMENT_NODE,
		ATTRIBUTE_NODE,
		TEXT_NODE,
		CDATA_SECTION_NODE,
		ENTITY_REFERENCE_NODE,
		ENTITY_NODE,
		PROCESSING_INSTRUCTION_NODE,
		COMMENT_NODE,
		DOCUMENT_NODE,
		DOCUMENT_TYPE_NODE,
		DOCUMENT_FRAGMENT_NODE,
		NOTATION_NODE,

		DTD_ENTITY_NODE,
		DTD_ELEMENT_NODE,
		DTD_ATTLIST_NODE
	}

	/// <summary>
	/// Lexer token type
	/// </summary>
	public enum TokenType : byte
	{
		UNKNOWN = 0,
		CDATA,
		START_TAG,
		END_TAG,
		START_QUOTE,
		END_QUOTE,
		EQUALS_SIGN,
		EXCLAMATION_POINT,
		QUESTION_MARK,
		FORWARD_SLASH,
		START_CDATA,
		END_CDATA,
		START_COMMENT,
		END_COMMENT,
		START_PROCESSING_INSTRUCTION,
		END_PROCESSING_INSTRUCTION,
		START_PAREN,
		END_PAREN,
		PERCENT,
		POUND,
		ASTERISK,
		COMMA,
		PIPE
	}

	public static class Config
	{
		public static IDictionary<TokenType, string> Tokens;
		public static IDictionary<TokenType, char> CharTokens;

		/// <summary>
		/// @todo create a new class to contain definition data like this array?
		/// EMPTY tags for XHTML 1.0 (from xhtml1-strict.dtd)
		/// </summary>
		public static ICollection<string> EmptyTags;

		static Config()
		{
			Tokens = new Dictionary<TokenType, string> {
				//{TokenType.CDATA,                        ""},
				{TokenType.START_CDATA,                  "[CDATA["},
				{TokenType.END_CDATA,                    "]]"},
				{TokenType.START_COMMENT,                "--"},
				{TokenType.END_COMMENT,                  "--"},
				{TokenType.START_PROCESSING_INSTRUCTION, "?"},
				{TokenType.END_PROCESSING_INSTRUCTION,   "?"}
			};

			CharTokens = new Dictionary<TokenType, char> {
				{TokenType.START_TAG,                    '<'},
				{TokenType.END_TAG,                      '>'},
				{TokenType.START_QUOTE,                  '"'},
				{TokenType.END_QUOTE,                    '"'},
				{TokenType.EQUALS_SIGN,                  '='},
				{TokenType.EXCLAMATION_POINT,            '!'},
				{TokenType.QUESTION_MARK,                '?'},
				{TokenType.START_PAREN,                  '('},
				{TokenType.END_PAREN,                    ')'},
				{TokenType.PERCENT,                      '%'},
				{TokenType.POUND,                        '#'},
				{TokenType.ASTERISK,                     '*'},
				{TokenType.COMMA,                        ','},
				{TokenType.PIPE,                         '|'},
				{TokenType.FORWARD_SLASH,                '/'}
			};

			EmptyTags = new SortedSet<string> {
				"base",
				"meta",
				"link",
				"hr",
				"br",
				"param",
				"img",
				"area",
				"input",
				"col"
			};
		}
	}
}
