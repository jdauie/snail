using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Snail.Nodes;

namespace Snail
{
	public class XmlParser : IParser
	{
		public DocumentNode Parse(string text)
		{
			var tags = ParseTags(text);

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

			DocumentNode root = null;
			//root = BuildTree(text, tags);

			return root;
		}

		private static long CreateTagIndex(long index, long length, long type)
		{
			// format : [  32  ][  28  ][  4  ]
			//         index   length  type
			// 
			// type   : [ 1 ][  2  ][ 1 ]
			//          tag  start  end
			// 
			// tag    : 0 = #text
			//          1 = tag
			// 
			// start  : 0 = "<"
			//        : 1 = "<!"
			//        : 2 = "<?"
			//        : 3 = "</"
			// 
			// end    : 0 = normal
			//          1 = self-closing
			// 
			// Assume length will fit, rather than explicitly clipping it.
			return (index | (length << 28) | (type << (32 + 28)));
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
						else// if (*p == '?' || *p == '/')
						{
							// processing instruction, closing tag, or normal tag
							while (p != pEnd && *p != '>') ++p;
						}
						// I don't need to do this separately until I do something different with it.
						//else
						//{
						//    // normal tag
						//    while (p != pEnd && *p != '>') ++p;
						//}

						tags.Add(CreateTagIndex(pStart - pText, p - pStart + 1));
					}

					++p;
				}
			}

			return tags;
		}

		public static unsafe DocumentNode BuildTree(string text, List<long> tags)
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
