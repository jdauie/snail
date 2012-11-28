using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail
{
	class BlockIdentifier
	{
		public readonly TokenType StartIdentifier;
		public readonly TokenType EndIdentifier;
		public readonly bool CheckStart;

		public BlockIdentifier(TokenType start, TokenType end, bool checkStart = true)
		{
			StartIdentifier = start;
			EndIdentifier = end;
			CheckStart = checkStart;
		}
	}

	class Lexer
	{
		/// <summary>
		/// Tokenizes the specified text. 
		/// Loses whitespace information within tags, but maintains it between tags.
		/// </summary>
		/// <param name="text">The text.</param>
		public static TokenList Tokenize(string text)
		{
			// state variables for the character cursor
			bool inTag    = false; // in a markup tag
			bool inStart  = false; // in a markup tag, with no entities consumed
			bool inEntity = false; // in a string entity
			bool inQuotes = false; // in a quoted string
			bool inEscape = false; // in an escaped tag

			var tokens = new TokenList();

			// constraint variables
			const string whitespace = "\n\r\t "; // whitespace elements
			const string quotes = "\"'"; // double/single-quotes

			// special tokens
			var delimiterTokens = new List<TokenType> {
				TokenType.EQUALS_SIGN,
				TokenType.FORWARD_SLASH,
				TokenType.EXCLAMATION_POINT,
				TokenType.QUESTION_MARK,
				TokenType.START_PAREN,
				TokenType.END_PAREN,
				TokenType.PERCENT,
				TokenType.POUND,
				TokenType.ASTERISK,
				TokenType.PIPE
			};

			//var delimiters = delimiterTokens.ToDictionary(token => Config.CharTokens[token]);

			var delimiters = new Dictionary<char, TokenType>(delimiterTokens.Count);
			foreach (var d in delimiterTokens)
				delimiters.Add(Config.CharTokens[d], d);
			
			// markup types that have to be handled specially because they contain 
			// character data that the lexer should identify as a single character string
			var escaped = new List<KeyValuePair<string, string>>
			{
			    new KeyValuePair<string, string>("script", "</script>"),
			    new KeyValuePair<string, string>("style", "</style>")
			};

			// other markup types whose content should also be identified 
			// as a single character string
			var single = new Dictionary<char, List<BlockIdentifier>> {
				{Config.CharTokens[TokenType.EXCLAMATION_POINT], new List<BlockIdentifier> {
					new BlockIdentifier(TokenType.START_COMMENT, TokenType.END_COMMENT), 
					new BlockIdentifier(TokenType.START_CDATA, TokenType.END_CDATA)}}, 
				{Config.CharTokens[TokenType.QUESTION_MARK], new List<BlockIdentifier> {
					new BlockIdentifier(TokenType.START_PROCESSING_INSTRUCTION, TokenType.END_PROCESSING_INSTRUCTION, false)}}
			};

			////var initConfig = Config.EmptyTags;
			//Console.Write("{0}", single.Count);
			//return null;

			// temporary variables
			var length = text.Length;
			var quote = '\0'; // active instance of a quote character from quotes
			var escape = -1; // index of current section in escaped
			
			for (int i = 0; i < length; i++)
			{
				var c = text[i];

				if (inTag)
				{
					if (inQuotes)
					{
						if (c == quote)
						{
							// end quote
							tokens.Create(TokenType.END_QUOTE);
							inQuotes = false;
							inEntity = false;
							// check for potential malformed quote
							if (text[i - 1] == '=')
							{
								// there is a high probability that a quote 
								// is missing before this point, so rollback
								// to the last space and end the quote
								var t = i;
								while (i >= 0 && whitespace.IndexOf(text[i]) < 0)
									--i;

								// Revert changes to valid markup
								// e.g. <a href="page?hash=EEASEFAE=">
								int newLength = tokens.Previous.Value.Length - (t - i);
								if (newLength > 0)
									tokens.Previous = new Token(tokens.Previous.Value.Substring(0, newLength));
								else // undo
									i = t;
							}
						}
						else if (inEntity)
						{
							// add to current token
							tokens.AppendToCurrent(c);
							// check for potential malformed quote
							if (c == '>')
							{
								// in this case, it is possible that there is a 
								// missing quote, but the probability is lower
								// than if the string starts with an END_TAG
								// todo implement rollback code
							}
						}
						else
						{
							// start quoted string
							tokens.Create(c);
							inEntity = true;
							// check for potential malformed quote
							if (c == '>')
							{
								// there is a high probability that a quote 
								// is missing earlier in the tag, so rollback
								// until a likely position is found
								// todo implement rollback code
							}
						}
					}
					else if (c == '>')
					{
						// tag end
						tokens.Create(TokenType.END_TAG);
						inTag = false;
						inEntity = false;
						// handle script block
						if (inEscape)
						{
							// find the ending tag out of order 
							// so that the contents can be skipped
							var pos = text.IndexOf(escaped[escape].Value, i + 1);
							if (pos == -1)
							{
								pos = length + 1;
							}
							tokens.Create(text.Substring(i + 1, pos - (i + 1)));
							i = pos - 1;
							inEscape = false;
							escape = -1;
						}
					}
					else if (delimiters.ContainsKey(c))
					{
						// store special character
						if (c != '?')
						{
							tokens.Create(delimiters[c]);
						}
						inEntity = false;
						// check for xml-style declaration
						if (inStart && single.ContainsKey(c))
						{
							foreach (var block in single[c])
							{
								var startIdentifier = Config.Tokens[block.StartIdentifier];
								var endIdentifier   = Config.Tokens[block.EndIdentifier];
								var startlen = (block.CheckStart ? startIdentifier.Length : 0);
								if (!block.CheckStart ||
									((i + startlen < length) &&
									  (String.Compare(text.Substring(i + 1, startlen), startIdentifier, true) == 0)))
								{
									// find the end of this declaration out of order 
									// so that the contents can be skipped
									var pos = text.IndexOf(endIdentifier + '>', i + startlen + 1);
									if (pos == -1)
									{
										pos = length + 1;
									}
									tokens.Create(block.StartIdentifier);
									tokens.Create(text.Substring(i + startlen + 1, pos - (i + startlen + 1)));
									tokens.Create(block.EndIdentifier);
									i = pos + endIdentifier.Length - 1;
									break;
								}
							}
						}
					}
					else if (quotes.IndexOf(c) != -1)
					{
						// start quote
						tokens.Create(TokenType.START_QUOTE);
						inQuotes = true;
						inEntity = false;
						quote = c;
					}
					else if (c == '<')
					{
						// handle invalid markup condition where a new tag starts 
						// before the previous tag ends
						tokens.Create(TokenType.END_TAG);
						inTag = false;
						inEntity = false;
						--i;
					}
					else if (inEntity)
					{
						// break on whitespace
						if (whitespace.IndexOf(c) != -1)
						{
							inEntity = false;
						}
						else
						{
							// add to current token
							tokens.AppendToCurrent(c);
						}
					}
					else
					{
						// ignore whitespace
						if (whitespace.IndexOf(c) == -1)
						{
							tokens.Create(c);
							inEntity = true;
							inStart = false;
						}
					}
				}
				else
				{
					if (c == '<')
					{
						// tag start
						tokens.Create(TokenType.START_TAG);
						inTag = true;
						inStart = true;
						inEntity = false;
						// check if this is the beginning of an escaped block
						// this condition will be handled at the end of the tag
						for (int j = 0; j < escaped.Count; j++)
						{
							var kvp = escaped[j];
							if (i + kvp.Key.Length < length &&
								String.Compare(text.Substring(i + 1, kvp.Key.Length), kvp.Key, true) == 0)
							{
								// begin tag delimited escape block
								inEscape = true;
								escape = j;
								break;
							}
						}
					}
					else if (inEntity)
					{
						// add to current token
						tokens.AppendToCurrent(c);
					}
					else
					{
						// start quoted string
						tokens.Create(c);
						inEntity = true;
					}
				}
			}

			tokens.FinalizeCurrent();
			
			return tokens;
		}
	}
}
