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
	enum TagType : long
	{
		TAG_TYPE_TEXT        = 0,
		TAG_TYPE_OPENING     = 1,
		TAG_TYPE_CLOSING     = 2,
		TAG_TYPE_COMMENT     = 3,
		TAG_TYPE_CDATA       = 4,
		TAG_TYPE_DECLARATION = 5,
		TAG_TYPE_PROCESSING  = 6
	}

	/// <summary>
	/// This can be better for massive files.
	/// </summary>
	public class TagList : IEnumerable<long>
	{
		private const int CHUNK_SIZE = 1000;

		private readonly List<long[]> m_chunks;
		private long[] m_current;
		private int m_index;

		public TagList()
		{
			m_chunks = new List<long[]>();
			m_current = new long[CHUNK_SIZE];
			m_index = 0;
		}

		public int Count
		{
			get { return (m_chunks.Count * CHUNK_SIZE) + m_index; }
		}

		public void Add(long item)
		{
			if (m_index == CHUNK_SIZE)
			{
				m_chunks.Add(m_current);
				m_current = new long[CHUNK_SIZE];
				m_index = 0;
			}

			m_current[m_index] = item;
			++m_index;
		}

		public IEnumerator<long> GetEnumerator()
		{
			foreach (var chunk in m_chunks)
				foreach (var tag in chunk)
					yield return tag;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
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
			//        : 4 = '<!' #CDATA
			//        : 5 = '<!' #declaration (DOCTYPE, ENTITY, ELEMENT, ATTLIST)
			//        : 6 = '<?' #processing-instruction
			// 
			// Assume length will fit, rather than explicitly clipping it.
			// It will be garbage either way -- I would really have to throw.
			// 
			// I could limit the bits for [length] even more, and use either zero or all ones to indicate that it doesn't fit.
			// This would require reading forward again or backing up from the next tag (when I want to read the data).
			// Also, I don't necessarily care about length...sometimes tag name/prefix length is all I want.  
			// Should they share the bits (depending on the type of tag and how expensive it is to re-read)?
			// 
			// I could shrink the [index] bits by grouping tags using a common offset.
			// 
			// I want to start storing tag name/namespace length again.
			// Also start storing depth of nesting (then I don't need to store end tags).
			// 
			// Add start-tag index (to navigate like a tree).
			// 
			// I don't know yet if I want attributes to be included as tags (which obviously would become tokens at that point).

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

		public static unsafe IEnumerable<long> ParseTags(string text)
		{
			var tags = new List<long>();
			//var tags = new TagList();

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

						tags.Add(CreateTagIndex(pStart - pText, p - pStart));
					}
					//else if (p != pStart)
					//{
					//    // remember that this is whitespace, but no more details
					//    tags.Add(0L);
					//}

					// identify tag region
					if (p != pEnd)
					{
						TagType type = TagType.TAG_TYPE_OPENING;

						pStart = p;
						++p;
						if (*p == '!' && p[1] == '-' && p[2] == '-')
						{
							type = TagType.TAG_TYPE_COMMENT;
							p = FindEndComment(p, pEnd);
						}
						else if (*p == '!' && p[1] == '[' && p[2] == 'C' && p[3] == 'D' && p[4] == 'A' && p[5] == 'T' && p[6] == 'A' && p[7] == '[')
						{
							type = TagType.TAG_TYPE_CDATA;
							p = FindEndCDATA(p, pEnd);
						}
						else if (*p == '?')
						{
							type = TagType.TAG_TYPE_PROCESSING;
							p = FindEndProcessing(p, pEnd);
						}
						else
						{
							if (*p == '/')
								type = TagType.TAG_TYPE_CLOSING;
							else if (*p == '!')
								type = TagType.TAG_TYPE_DECLARATION;

							while (p != pEnd && *p != '>') ++p;
						}

						tags.Add(CreateTagIndex(pStart - pText, p - pStart + 1, type));
					}

					++p;
				}
			}

			return tags;
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
					else if (type == TagType.TAG_TYPE_CDATA)
					{
						var node = new CDATASectionNode(text.SubstringTrim(index + 9, length - 12));
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
