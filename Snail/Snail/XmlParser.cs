using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Snail.Nodes;

namespace Snail
{
	public class XmlParser
	{
		public TokenList Parse(string text)
		{
			var tags = ParseTags(text);

			return tags;
		}

		private static void ReadTagIndex(long tag, out int index, out int length, out TokenType type)
		{
			index = (int)tag;
			length = (int)((tag << 4) >> (32 + 4));
			type = (TokenType)(tag >> (32 + 28));
		}

		public static unsafe TokenList ParseTags(string text)
		{
			var tags = new TokenList();

			long depth = 0;

			fixed (char* pText = text)
			{
				char* p = pText;
				char* pEnd = pText + text.Length;

				while (p < pEnd)
				{
					// skip past whitespace between tags
					char* pStart = p;
					while (p != pEnd && (*p == ' ' || *p == '\t' || *p == '\r' || *p == '\n'))
						++p;

					// identify text region (if there is one)
					if (p != pEnd && *p != '<')
					{
						while (p != pEnd && *p != '<')
							++p;

						tags.Add(pStart - pText, p - pStart, depth, TokenType.Text);
					}
					//else if (p != pStart)
					//{
					//    // remember that this is whitespace, but no more details
					//    tags.AddWhitespace();
					//}

					// identify tag region
					if (p != pEnd)
					{
						pStart = p;
						++p;

						if (*p == '!')
						{
							p = HandleExclamationPoint(pText, pStart, pEnd, depth, tags);
						}
						else if (*p == '?')
						{
							p = HandleQuestionMark(pText, pStart, pEnd, depth, tags);
						}
						else
						{
							// normal tags (closing, opening, self-closing)

							bool isClosing = (*p == '/');

							while (p != pEnd && *p != '>') ++p;

							if (isClosing)
							{
								--depth;
							}
							else
							{
								//long length = (p - pStart + 1);


								char* pTmp = pStart + 1;
								while (pTmp != p && (*pTmp != ' ' && *pTmp != '\t' && *pTmp != '\r' && *pTmp != '\n'))
									++pTmp;
								char* pNameEnd = pTmp;

								long length = pTmp - (pStart + 1);
								long namePrefixLength = 0;

								pTmp = pStart + 1;
								while (pTmp != pNameEnd && *pTmp != ':')
									++pTmp;
								if (pTmp != pNameEnd)
								{
									// prefix:qname
									namePrefixLength = pTmp - (pStart + 1);
								}


								tags.Add(pStart - pText, length, depth, TokenType.OpeningTag);

								// check for self-closing
								if ((*(p - 1) != '/'))
									++depth;
							}
						}
					}

					++p;
				}
			}

			if (depth != 0)
				throw new Exception("bad depth");

			return tags;
		}

		private static unsafe char* HandleExclamationPoint(char* pText, char* pStart, char* pEnd, long depth, TokenList tokens)
		{
			char* p = pStart + 2;

			TokenType type;

			if (p[0] == '-' && p[1] == '-')
			{
				type = TokenType.Comment;
				p = FindEndComment(p + 2, pEnd);
			}
			else if (p[0] == '[' && p[1] == 'C' && p[2] == 'D' && p[3] == 'A' && p[4] == 'T' && p[5] == 'A' && p[6] == '[')
			{
				type = TokenType.CDATA;
				p = FindEndCDATA(p + 7, pEnd);
			}
			else
			{
				type = TokenType.Declaration;
				while (p != pEnd && *p != '>') ++p;
			}

			tokens.Add(pStart - pText, p - pStart, depth, type);

			return p;
		}

		private static unsafe char* HandleQuestionMark(char* pText, char* pStart, char* pEnd, long depth, TokenList tokens)
		{
			char* p = pStart + 2;

			p = FindEndProcessing(p, pEnd);

			tokens.Add(pStart - pText, p - pStart, depth, TokenType.Processing);

			return p;
		}

		private static unsafe char* FindEndCDATA(char* p, char* pEnd)
		{
			char* pEndCDATA = pEnd - 2;
			while (p != pEndCDATA && (p[0] != ']' || p[1] != ']' || p[2] != '>'))
				++p;
			p += 2;
			return p;
		}

		private static unsafe char* FindEndComment(char* p, char* pEnd)
		{
			char* pEndComment = pEnd - 2;
			while (p != pEndComment && (p[0] != '-' || p[1] != '-' || p[2] != '>'))
				++p;
			p += 2;
			return p;
		}

		private static unsafe char* FindEndProcessing(char* p, char* pEnd)
		{
			char* pEndComment = pEnd - 1;
			while (p != pEndComment && (p[0] != '?' || p[1] != '>'))
				++p;
			p += 1;
			return p;
		}

		public static unsafe DocumentNode BuildTree(string text, IEnumerable<long> tags)
		{
			var root = new DocumentNode();
			ElementNode current = root;
			fixed (char* pText = text)
			{
				foreach (var tag in tags)
				{
					// whitespace
					if (tag == 0)
						continue;

					int index;
					int length;
					TokenType type;
					ReadTagIndex(tag, out index, out length, out type);

					if (type == TokenType.Text)
					{
						var node = new TextNode(text.Substring(index, length));
						current.AppendChild(node);
					}
					else if (type == TokenType.ClosingTag)
					{
						current = current.Parent;
					}
					else if (type == TokenType.OpeningTag)
					{
						bool isSelfClosingTag = (text[index + length - 2] == '/');

						char* p = pText + index + 1;
						char* pEnd = p + length - 2;
						while (p != pEnd && !char.IsWhiteSpace(*p))
							++p;

						int tagNameLength = (int)(p - (pText + index + 1));
						string tagName = text.Substring(index + 1, tagNameLength);
						var node = new ElementNode(tagName, isSelfClosingTag);

						int attributeStart = index + tagNameLength + 1;
						ParseAttributesFromWellFormedXml(node, text, attributeStart, length - (attributeStart - index) - 1);

						current.AppendChild(node);
						if (!isSelfClosingTag)
						{
							current = node;
						}
					}
					else if (type == TokenType.Comment)
					{
						var node = new CommentNode(text.SubstringTrim(index + 4, length - 7));
						current.AppendChild(node);
					}
					else if (type == TokenType.CDATA)
					{
						var node = new CDATASectionNode(text.SubstringTrim(index + 9, length - 12));
						current.AppendChild(node);
					}
					else if (type == TokenType.Declaration)
					{
						//
					}
					else if (type == TokenType.Processing)
					{
						var node = new ProcessingInstructionNode(text.Substring(index, length));
						current.AppendChild(node);
					}
				}
			}

			return root;
		}

		private static void ParseAttributesFromWellFormedXml(ElementNode node, string text, int index, int length)
		{
			if (length <= 0)
				return;

			// THIS IS NOT FAST...IT'S JUST TO GET SOMETHING WORKING
			int endIndex = index + length;
			while (index < endIndex)
			{
				// get name before '='
				int indexOfEquals = text.IndexOf('=', index, endIndex - index);
				if (indexOfEquals == -1)
					break;

				string name = text.SubstringTrim(index, indexOfEquals - index);
				index = indexOfEquals + 1;

				// get value between quotes
				int indexOfQuote1 = text.IndexOf('"', index);
				int indexOfQuote2 = text.IndexOf('"', indexOfQuote1 + 1);
				string value = text.Substring(indexOfQuote1 + 1, indexOfQuote2 - indexOfQuote1 - 1);
				index = indexOfQuote2 + 1;

				node.Attributes.Add(name, value);
			}
		}
	}
}
