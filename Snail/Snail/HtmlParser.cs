using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Snail.Nodes;

namespace Snail
{
	public class HtmlParser : IParser
	{
		public DocumentNode Parse(string text)
		{
			//var root = ParseWellFormedXml(text);
			var tags = ParseTags(text);

			DocumentNode root = null;
			//root = ParseTreeFromWellFormedXml(text, tags);

			#region Test

			//var tagStrings = new List<string>();
			//foreach (var tag in tags)
			//{
			//    int index = (int)tag;
			//    int length = (int)(tag >> 32);

			//    tagStrings.Add(text.Substring(index, length));
			//}
			//Console.WriteLine(tagStrings.Count);

			//var textRecreated = string.Join("", tagStrings);
			//Console.WriteLine(textRecreated.Equals(text));

			#endregion

			return root;
		}

		private static long CreateTagIndex(long index, long length)
		{
			return (index | (length << 32));
		}

		public static unsafe List<long> ParseTags(string text)
		{
			var tags = new List<long>();

			fixed (char* pText = text)
			{
				char* p = pText;
				char* pEnd = pText + text.Length;
				char* pStart;

				while (p < pEnd)
				{
					// skip past whitespace between tags
					// this is okay for the AFE SOAP format, but not for stuff like html
					pStart = p;
					while (p != pEnd && char.IsWhiteSpace(*p))
						++p;

					// identify text region (if there is one)
					if (p != pEnd && *p != '<')
					{
						while (p != pEnd && *p != '<')
							++p;

						tags.Add(CreateTagIndex(pStart - pText, p - pStart));
					}
					//else if(p != pTagStart)
					//{
					//    // remember that this is whitespace, but no more details
					//    tags.Add(0L);
					//}

					// identify tag region
					if (p != pEnd)
					{
						char* pActual = null;
						pStart = p;
						++p;
						if (p[0] == '!' && p[1] == '-' && p[2] == '-')
						{
							// comment
							char* pEndComment = pEnd - 2;
							while (p != pEndComment && p[0] != '-' && p[1] != '-' && p[2] != '>')
								++p;
							p += 2;
						}
						else if (*p == '?' || *p == '/')
						{
							// processing instruction, closing tag
							while (p != pEnd && *p != '>') ++p;
						}
						else
						{
							// normal tag
							while (p != pEnd && *p != '>') ++p;

							pActual = CheckForScriptOrStyleBlock(tags, pText, pStart, p, pEnd);
						}

						if (pActual == null || pActual == p)
							tags.Add(CreateTagIndex(pStart - pText, p - pStart + 1));
						else
							p = pActual;
					}

					++p;
				}
			}

			return tags;
		}

		public static unsafe DocumentNode ParseTreeFromWellFormedXml(string text, List<long> tags)
		{
			var root = new DocumentNode();
			ElementNode current = root;
			fixed (char* pText = text)
			{
				for (int i = 0; i < tags.Count; i++)
				{
					long tag = tags[i];

					if (tag == 0)
						continue;

					int index = (int)tag;
					int length = (int)(tag >> 32);

					if (text[index] != '<')
					{
						var node = new TextNode(text.Substring(index, length));
						current.AppendChild(node);
					}
					//else if (other == ushort.MaxValue)
					//{
					//    var node = new CommentNode(text.SubstringTrim(index + 4, length - 7));
					//    current.AppendChild(node);
					//}
					else
					{
						bool isClosingTag = text[index + 1] == '/';
						if (isClosingTag)
						{
							current = current.Parent;
						}
						else
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
					}
				}
			}

			return root;
		}

		private static unsafe char* CheckForScriptOrStyleBlock(List<long> tags, char* pText, char* pTagStart, char* pTagEnd, char* pEnd)
		{
			long tagLengthMinusOne = pTagEnd - pTagStart;
			if (tagLengthMinusOne > 6 && (pTagStart[1] == 's' || pTagStart[1] == 'S') && (pTagStart[2] == 'c' || pTagStart[2] == 'C') && (pTagStart[3] == 'r' || pTagStart[3] == 'R') && (pTagStart[4] == 'i' || pTagStart[4] == 'I') && (pTagStart[5] == 'p' || pTagStart[5] == 'P') && (pTagStart[6] == 't' || pTagStart[6] == 'T'))
			{
				// script?
				if (pTagStart[7] == '>' || char.IsWhiteSpace(pTagStart[7]))
				{
					char* p = pTagEnd + 1;
					char* pEndMinusEndTag = pEnd - 9;
					while (p != pEndMinusEndTag && (p[0] != '<' || p[1] != '/' || p[8] != '>' || (p[2] != 's' && p[2] != 'S') || (p[3] != 'c' && p[3] != 'C') || (p[4] != 'r' && p[4] != 'R') || (p[5] != 'i' && p[5] != 'I') || (p[6] != 'p' && p[6] != 'P') || (p[7] != 't' && p[7] != 'T')))
						++p;

					// add start tag
					tags.Add(CreateTagIndex(pTagStart - pText, pTagEnd - pTagStart + 1));

					// add contents as text block
					tags.Add(CreateTagIndex((pTagEnd + 1) - pText, p - pTagEnd - 1));

					// add end tag
					tags.Add(CreateTagIndex(p - pText, 9));

					return p + (9 - 1);
				}
			}
			else if (tagLengthMinusOne > 5 && (pTagStart[1] == 's' || pTagStart[1] == 'S') && (pTagStart[2] == 't' || pTagStart[2] == 'T') && (pTagStart[3] == 'y' || pTagStart[3] == 'Y') && (pTagStart[4] == 'l' || pTagStart[4] == 'L') && (pTagStart[5] == 'e' || pTagStart[5] == 'E'))
			{
				// style?
				if (pTagStart[7] == '>' || char.IsWhiteSpace(pTagStart[6]))
				{
					// do the same as with style
				}
			}

			//char* pName = pTagStart + 1;
			//while (pName != p && !char.IsWhiteSpace(*pName))
			//    ++pName;

			//int tagNameLength = (int)(pName - (pTagStart + 1));
			//string tagName = text.Substring((int)(pTagStart - pText) + 1, tagNameLength);

			return pTagEnd;
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
