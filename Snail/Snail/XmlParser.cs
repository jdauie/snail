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
			var tags = ParseTokens(text);

			return tags;
		}

		private static void ReadTagIndex(long tag, out int index, out int length, out TokenType type)
		{
			index = (int)tag;
			length = (int)((tag << 4) >> (32 + 4));
			type = (TokenType)(tag >> (32 + 28));
		}

		public static unsafe TokenList ParseTokens(string text)
		{
			var tokens = new TokenList(text);

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

						tokens.AddRegion(pStart - pText, TokenType.TextNormal, p - pStart, depth);
					}
					//else if (p != pStart)
					//{
					//    // remember that this is whitespace, but no more details
					//    tokens.AddWhitespace();
					//}

					// identify tag region
					if (p != pEnd)
					{
						pStart = p;
						++p;

						if (*p == '!')
						{
							p = HandleExclamationPoint(pText, pStart, pEnd, depth, tokens);
						}
						else if (*p == '?')
						{
							p = HandleQuestionMark(pText, pStart, pEnd, depth, tokens);
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
								//tags.Add(pStart - pText, length, depth, TokenType.OpeningTag);


								// QName format
								// [prefix:]local

								char* pFirstSymbol = pStart + 1;
								char* pTmp = pFirstSymbol;
								while (pTmp != p && (*pTmp != ' ' && *pTmp != '\t' && *pTmp != '\r' && *pTmp != '\n'))
									++pTmp;
								char* pNameEnd = pTmp;

								long length = pTmp - pFirstSymbol;
								//long namePrefixLength = 0;

								//pTmp = pStart + 1;
								//while (pTmp != pNameEnd && *pTmp != ':')
								//    ++pTmp;
								//if (pTmp != pNameEnd)
								//{
								//    // prefix:qname
								//    namePrefixLength = pTmp - (pStart + 1);
								//}

								tokens.AddTag(pFirstSymbol - pText, length, 0, depth);

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

			return tokens;
		}

		private static unsafe char* HandleExclamationPoint(char* pText, char* pStart, char* pEnd, long depth, TokenList tokens)
		{
			char* p = pStart + 2;

			if (p[0] == '-' && p[1] == '-')
			{
				p = FindEndComment(p + 2, pEnd);
				tokens.AddRegion(pStart - pText, TokenType.Comment, p - pStart + 1, depth);
			}
			else if (p[0] == '[' && p[1] == 'C' && p[2] == 'D' && p[3] == 'A' && p[4] == 'T' && p[5] == 'A' && p[6] == '[')
			{
				p = FindEndCDATA(p + 7, pEnd);
				tokens.AddRegion(pStart - pText, TokenType.CDATA, p - pStart + 1, depth);
			}
			else
			{
				while (p != pEnd && *p != '>') ++p;
				tokens.AddDecl(pStart - pText, p - pStart + 1, depth);
			}

			return p;
		}

		private static unsafe char* HandleQuestionMark(char* pText, char* pStart, char* pEnd, long depth, TokenList tokens)
		{
			char* p = pStart + 2;

			p = FindEndProcessing(p, pEnd);

			tokens.AddProc(pStart - pText, 0, p - pStart + 1, depth);

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
	}
}
