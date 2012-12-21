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
	public enum TokenType : byte
	{
		Text = 0,
		OpeningTag = 1,
		ClosingTag = 2,

		// special blocks
		Comment = 3,
		CDATA = 4,
		Declaration = 5,
		Processing = 6, // currently, this includes "<?xml ...>"

		AttrName = 7,
		AttrNS = 8,
		AttrValue = 9,

		Reserved10 = 10,
		Reserved11 = 11,
		Reserved12 = 12,
		Reserved13 = 13,
		Reserved14 = 14,
		Reserved15 = 15
	}

	//public const int TOKEN_PI_NAME = 7;
	//public const int TOKEN_PI_VAL = 8;

	//public const int TOKEN_DEC_ATTR_NAME = 9;
	//public const int TOKEN_DEC_ATTR_VAL = 10;

	//public const int TOKEN_DTD_VAL = 12;
	//public const int TOKEN_DOCUMENT = 13;

	/// <summary>
	/// This can be better for massive files.
	/// </summary>
	public class TagList : IEnumerable<long>
	{
		private const int CHUNK_SIZE = (1 << 20) / sizeof(long);

		private const int BITS_INDEX = 30;
		private const int BITS_LENGTH = 20;
		private const int BITS_DEPTH = 8;
		private const int BITS_TYPE = 4;

		private const int MAX_INDEX = (1 << 30) - 1;
		private const int MAX_LENGTH = (1 << 20) - 1;
		private const int MAX_DEPTH = (1 << 8) - 1;

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

		public int Capacity
		{
			get { return ((m_chunks.Count + 1) * CHUNK_SIZE); }
		}

		/// <summary>
		/// format : [  30  ][  20  ][  8  ][  4  ][  2  ]
		///           index   length  depth  type   ?
		/// 
		/// Can I pack this in 32 bits (by grouping "index" regions and shortening "length")?  Would it be more efficient?
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="length">The length.</param>
		/// <param name="depth">The depth.</param>
		/// <param name="type">The type.</param>
		public void Add(long index, long length, long depth, TokenType type)
		{
			if (m_index == CHUNK_SIZE)
				CreateChunk();

			if (length > MAX_LENGTH)
			{
				m_current[m_index] = (index) | (MAX_LENGTH << BITS_INDEX) | (depth << (BITS_INDEX + BITS_LENGTH)) | ((long)type << (BITS_INDEX + BITS_LENGTH + BITS_DEPTH));
				++m_index;
				AddLength(length);
			}
			else
			{
				m_current[m_index] = (index) | (length << BITS_INDEX) | (depth << (BITS_INDEX + BITS_LENGTH)) | ((long)type << (BITS_INDEX + BITS_LENGTH + BITS_DEPTH));
				++m_index;
			}
		}

		private void AddLength(long length)
		{
			if (m_index == CHUNK_SIZE)
				CreateChunk();

			m_current[m_index] = length;
			++m_index;
		}

		public void AddWhitespace()
		{
			if (m_index == CHUNK_SIZE)
				CreateChunk();

			m_current[m_index] = 0;
			++m_index;
		}

		private void CreateChunk()
		{
			m_chunks.Add(m_current);
			m_current = new long[m_index];
			m_index = 0;
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

	public class XmlParser// : IParser
	{
		public TagList Parse(string text)
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

			//DocumentNode root = null;
			//root = BuildTree(text, tags);

			return tags;
		}

		private static long CreateTagIndex(long index, long length, TokenType type)
		{
			// format : [  32  ][  28  ][  4  ]
			//           index   length  type
			// 
			// type   : 0 =      #text
			//        : 1 = '<'  #opening
			//        : 2 = '</' #closing
			//        : 3 = '<!' #comment
			//        : 4 = '<!' #CDATA
			//        : 5 = '<!' #declaration (DOCTYPE, ENTITY, ELEMENT, ATTLIST, NOTATION)
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
			// NEW IDEA:  I can get the benefits of using fewer bits for the length *and* not storing end tags if I add a second
			// token after a token that overflows the length.  That would be wasteful if all the lengths are large, but if that is
			// the case, then there will not be many tags, making it acceptable.
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

		private static void ReadTagIndex(long tag, out int index, out int length, out TokenType type)
		{
			index = (int)tag;
			length = (int)((tag << 4) >> (32 + 4));
			type = (TokenType)(tag >> (32 + 28));
		}

		public static unsafe TagList ParseTags(string text)
		{
			//var tags = new List<long>();
			var tags = new TagList();

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

						//tags.Add(CreateTagIndex(pStart - pText, p - pStart));
						tags.Add(pStart - pText, p - pStart, depth, TokenType.Text);
					}
					//else if (p != pStart)
					//{
					//    // remember that this is whitespace, but no more details
					//    //tags.Add(0L);
					//    tags.AddWhitespace();
					//}

					// identify tag region
					if (p != pEnd)
					{
						TokenType type = TokenType.OpeningTag;

						pStart = p;
						++p;
						if (*p == '!' && p[1] == '-' && p[2] == '-')
						{
							type = TokenType.Comment;
							p = FindEndComment(p, pEnd);
							tags.Add(pStart - pText, p - pStart + 1, depth, type);
						}
						else if (*p == '!' && p[1] == '[' && p[2] == 'C' && p[3] == 'D' && p[4] == 'A' && p[5] == 'T' && p[6] == 'A' && p[7] == '[')
						{
							type = TokenType.CDATA;
							p = FindEndCDATA(p, pEnd);
						}
						else if (*p == '?')
						{
							type = TokenType.Processing;
							p = FindEndProcessing(p, pEnd);
						}
						else
						{
							if (*p == '/')
								type = TokenType.ClosingTag;
							else if (*p == '!')
								type = TokenType.Declaration;

							while (p != pEnd && *p != '>') ++p;

							if (type == TokenType.OpeningTag)
							{
								// self-closing
								if (*(p - 1) != '/')
									++depth;
							}
						}

						if (type != TokenType.ClosingTag)
						{
							long length = 0;
							if (type == TokenType.OpeningTag)
							{
								char* pTmp = pStart + 1;
								while (pTmp != p && (*pTmp == ' ' || *pTmp == '\t' || *pTmp == '\r' || *pTmp == '\n'))
									++pTmp;
								char* pNameEnd = pTmp;

								length = pTmp - (pStart + 1);
								long namePrefixLength = 0;

								pTmp = pStart + 1;
								while (pTmp != pNameEnd && *pTmp != ':')
									++pTmp;
								if (pTmp != pNameEnd)
								{
									// prefix:qname
									namePrefixLength = pTmp - (pStart + 1);
								}

								// attributes
								//if (pTmp != p)
								//{

								//}
							}
							else
							{
								length = (p - pStart + 1);
							}

							//tags.Add(CreateTagIndex(pStart - pText, p - pStart + 1, type));
							tags.Add(pStart - pText, length, depth, type);
						}
						else
						{
							--depth;
						}
					}

					++p;
				}
			}

			if (depth != 0)
				throw new Exception("bad depth");

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
