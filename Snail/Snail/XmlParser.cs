﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Snail.Nodes;

namespace Snail
{
	enum TagType : long
	{
		TAG_TYPE_TEXT        = 0,
		TAG_TYPE_OPENING     = 1,
		TAG_TYPE_CLOSING     = 2,
		TAG_TYPE_COMMENT     = 3,
		TAG_TYPE_DECLARATION = 4,
		TAG_TYPE_PROCESSING  = 5
	}

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
			//    int length = (int)((tag << 4) >> (32 + 4));
			//    int type = (int)(tag >> (32 + 28));

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

		private static long CreateTagIndex(long index, long length, TagType type)
		{
			// format : [  32  ][  28  ][  4  ]
			//           index   length  type
			// 
			// type   : 0 =      #text
			//        : 1 = '<'  #opening
			//        : 2 = '</' #closing
			//        : 3 = '<!' #comment
			//        : 4 = '<!' #declaration (CDATA, DOCTYPE, ENTITY, ELEMENT, ATTLIST)
			//        : 5 = '<?' #processing-instruction
			// 
			// Assume length will fit, rather than explicitly clipping it.
			// It will be garbage either way -- I would really have to throw.
			return (index | (length << 32) | ((long)type << (32 + 28)));
		}

		private static long CreateTagIndex(long index, long length)
		{
			return (index | (length << 32));
		}

		private static void ReadTagIndex(long tag, out int index, out int length, out TagType type)
		{
			index  = (int)tag;
			length = (int)((tag << 4) >> (32 + 4));
			type   = (TagType)(tag >> (32 + 28));
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
					// this removes whitespace, but at ~10% time penalty
					//while (p != pEnd && char.IsWhiteSpace(*p))
					//    ++p;
					// this doesn't slow things down, but doesn't cover unicode
					//while (p != pEnd && ((*p == ' ') || (*p >= '\x0009' && *p <= '\x000d') || *p == '\x00a0' || *p == '\x0085'))
					//    ++p;

					// identify text region (if there is one)
					if (p != pEnd && *p != '<')
					{
						while (p != pEnd && *p != '<')
							++p;

						tags.Add(CreateTagIndex(pStart - pText, p - pStart));
					}
					//// this remembers whitespace, but at ~10% time penalty
					//else if (p != pStart)
					//{
					//    // remember that this is whitespace, but no more details
					//    tags.Add(0L);
					//}

					// identify tag region
					if (p != pEnd)
					{
						TagType type = 0;

						pStart = p;
						++p;
						if (p[0] == '!' && p[1] == '-' && p[2] == '-')
						{
							// comment
							char* pEndComment = pEnd - 2;
							while (p != pEndComment && (p[0] != '-' || p[1] != '-' || p[2] != '>'))
								++p;
							p += 2;

							type = TagType.TAG_TYPE_COMMENT;
						}
						else
						{
							if (*p == '/')
								type = TagType.TAG_TYPE_CLOSING;
							else if (*p == '?')
								type = TagType.TAG_TYPE_PROCESSING;
							else if (*p == '!')
								type = TagType.TAG_TYPE_DECLARATION;
							else
								type = TagType.TAG_TYPE_OPENING;

							while (p != pEnd && *p != '>') ++p;
						}

						tags.Add(CreateTagIndex(pStart - pText, p - pStart + 1, type));
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

					// whitespace
					if (tag == 0)
						continue;

					int index;
					int length;
					TagType type;
					ReadTagIndex(tag, out index, out length, out type);

					if (type == TagType.TAG_TYPE_TEXT)
					{
						var node = new TextNode(text.Substring(index, length));
						current.AppendChild(node);
					}
					else if (type == TagType.TAG_TYPE_CLOSING)
					{
						current = current.Parent;
					}
					else if (type == TagType.TAG_TYPE_OPENING)
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
					else if (type == TagType.TAG_TYPE_COMMENT)
					{
						var node = new CommentNode(text.SubstringTrim(index + 4, length - 7));
						current.AppendChild(node);
					}
					else if (type == TagType.TAG_TYPE_DECLARATION)
					{
						//
					}
					else if (type == TagType.TAG_TYPE_PROCESSING)
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
