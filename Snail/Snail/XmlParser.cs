using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Snail.Nodes;

namespace Snail
{
	//internal struct TagIndex
	//{
	//    public readonly int Index;
	//    public readonly int Length;

	//    public TagIndex(int index, int length)
	//    {
	//        Index = index;
	//        Length = length;
	//    }
	//}

	public class XmlParser : IParser
	{
		public DocumentNode Parse(string text)
		{
			//var root = ParseWellFormedXml(text);
			var tags = ParseWellFormedXml2(text);

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

			//return root;
			return null;
		}

		private static long CreateTagIndex(long index, long length)
		{
			return (index | (length << 32));
		}

		public static unsafe List<long> ParseWellFormedXml2(string text)
		{
			var tags = new List<long>();

			fixed (char* pText = text)
			{
				char* p = pText;
				char* pEnd = pText + text.Length;
				char* pStart = p;

				while (p < pEnd)
				{
					// identify text region (if there is one)
					if (*p != '<')
					{
						pStart = p;
						while (p != pEnd && *p != '<')
							++p;

						tags.Add(CreateTagIndex(pStart - pText, p - pStart));
					}

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
						else if (*p == '?' || *p == '/')
						{
							// processing instruction or closing tag
							while (p != pEnd && *p != '>') ++p;
						}
						else
						{
							// normal tag
							while (p != pEnd && *p != '>') ++p;
						}

						tags.Add(CreateTagIndex(pStart - pText, p - pStart + 1));
					}

					++p;
				}
			}

			return tags;
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

		public static DocumentNode ParseWellFormedXml(string text)
		{
			var root = new DocumentNode();
			ElementNode current = root;

			int i = 0;
			while (i < text.Length)
			{
				var textEndIndex = FindTextBlock(text, current, i);

				i = textEndIndex;
				if (i == text.Length)
					break;

				// include search char
				var tagEndIndex = text.IndexOf('>', i);
				if (tagEndIndex == -1)
					tagEndIndex = text.Length - 1;
				if (tagEndIndex != i)
				{
					int j = i + 1;
					char jc = text[j];

					if (jc == '!')
					{
						// comment handling
						tagEndIndex = IdentifyTagStartingWithExclamation(text, current, i, tagEndIndex);
						// skip document type declarations and markup declarations
					}
					else if (jc == '?')
					{
						// <?xml version="1.0" encoding="utf-8"?>
						IdentifyTagStartingWithQuestionMark(text, current, i, tagEndIndex);
					}
					else
					{
						bool isClosingTag = text[i + 1] == '/';
						if (isClosingTag)
						{
							current = current.Parent;
						}
						else
						{
							// accumulate until whitespace or end
							while (j < tagEndIndex)
							{
								if (Char.IsWhiteSpace(text[j]))
									break;
								++j;
							}

							int tagNameLength = j - i - 1;

							bool isSelfClosingTag = text[tagEndIndex - 1] == '/';
							string tagName = text.Substring(i + 1, tagNameLength);
							var node = new ElementNode(tagName, isSelfClosingTag);

							int attributeStart = j + 1;
							ParseAttributesFromWellFormedXml(node, text, attributeStart, tagEndIndex - attributeStart);

							current.AppendChild(node);
							if (!isSelfClosingTag)
							{
								current = node;
							}
						}
					}
					i = tagEndIndex;
				}

				++i;
			}

			return root;
		}

		private static int FindTextBlock(string text, ElementNode current, int textStartIndex)
		{
			int length = text.Length;
			int textEndIndex = textStartIndex;

			while (textEndIndex < length && char.IsWhiteSpace(text[textEndIndex]))
				++textEndIndex;

			// only add the block if it contains non-whitespace chars
			if (textEndIndex != length && text[textEndIndex] != '<')
			{
				while (textEndIndex < length && text[textEndIndex] != '<')
					++textEndIndex;
				
				// exclude search char
				// this substring is expensive!  Is it a cache problem?  Would it be faster to copy as I go?
				var node = new TextNode(text.Substring(textStartIndex, textEndIndex - textStartIndex));
				current.AppendChild(node);
			}

			return textEndIndex;
		}

		private static int IdentifyTagStartingWithExclamation(string text, ElementNode current, int i, int tagEndIndex)
		{
			int j = i + 1;

			// check if this is a comment block and find the ending "-->"
			if (String.Compare(text, j + 1, "--", 0, "--".Length) == 0)
			{
				// find -->

				int searchPos = tagEndIndex - 3;
				while (searchPos != -1)
				{
					searchPos = text.IndexOf('-', searchPos);
					// is this condition correct
					if (searchPos > text.Length - 3)
						searchPos = -1;

					if (searchPos != -1)
					{
						if (text[searchPos + 1] == '-' && text[searchPos + 2] == '>')
						{
							break;
						}
						++searchPos;
					}
				}

				if (searchPos != -1)
				{
					// add entire comment as one tag
					tagEndIndex = searchPos + "-->".Length - 1;
					var node = new CommentNode(text.SubstringTrim(i + 4, tagEndIndex - i - 6));
					current.AppendChild(node);
				}

				// equivalent, but slower
				//int closeCommentIndex = text.IndexOf("-->", j + 3);
				//if (closeCommentIndex != -1)
				//{
				//    tagEndIndex = closeCommentIndex + "-->".Length - 1;
				//    tags.Add(textStartIndex | (((long)tagEndIndex - textStartIndex + 1) << 24));
				//}
			}
			//else if (String.Compare(text, j + 1, "[CDATA[", 0, "[CDATA[".Length, true) == 0)
			//{

			//}
			//else if (String.Compare(text, j + 1, "DOCTYPE", 0, "DOCTYPE".Length, true) == 0)
			//{

			//}
			//else if (String.Compare(text, j + 1, "ENTITY", 0, "ENTITY".Length, true) == 0)
			//{

			//}
			//else if (String.Compare(text, j + 1, "ELEMENT", 0, "ELEMENT".Length, true) == 0)
			//{

			//}
			//else if (String.Compare(text, j + 1, "ATTLIST", 0, "ATTLIST".Length, true) == 0)
			//{

			//}

			return tagEndIndex;
		}

		private static void IdentifyTagStartingWithQuestionMark(string text, ElementNode current, int i, int tagEndIndex)
		{
			var node = new ProcessingInstructionNode(text.Substring(i + 2, tagEndIndex - i - 3));
			current.AppendChild(node);
		}
	}
}
