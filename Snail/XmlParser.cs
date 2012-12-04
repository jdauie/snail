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
		public DocumentNode ParseWellFormedXml(string text)
		{
			var root = ParseTags(text);

			#region Test

			/* 
			 * [ low . . . . . . high ]
			 * [  32  ][  16  ][  16  ]
			 *  index   count   other
			 */

			//var test = new List<string>();
			////var sb = new StringBuilder(text.Length);
			//foreach (var tag in tags)
			//{
			//    int index = ((int)tag << 8) >> 8;
			//    int length = (int)((tag << 16) >> (24 + 16));
			//    ushort other = (ushort)(tag >> 64 - 16);

			//    if (other == 0)
			//    {
			//        // text block
			//        test.Add(text.Substring(index, length));
			//    }
			//    else if (other == ushort.MaxValue)
			//    {
			//        // comment block
			//        //test.Add(text.Substring(index, length));
			//    }
			//    else
			//    {
			//        // tag name
			//        test.Add(string.Format("<{0}>", text.Substring(index + 1, other)));
			//        //test.Add(text.Substring(index, length));
			//    }

			//    //test.Add(text.Substring(index, length));
			//    //sb.Append(text.Substring(index, length));
			//}
			////var textRecreated = sb.ToString();
			//Console.WriteLine(test.Count);

			//var textRecreated = string.Join("", tags);
			//Console.WriteLine(textRecreated.Length);

			#endregion

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
				int indexOfEquals = text.IndexOf('=', index); // bound this (so I don't have the check it below)
				if (indexOfEquals > endIndex || indexOfEquals == -1)
					break;

				string name = text.Substring(index, indexOfEquals - index).Trim();
				index = indexOfEquals + 1;
				
				// get value between quotes
				int indexOfQuote1 = text.IndexOf('"', index);
				int indexOfQuote2 = text.IndexOf('"', indexOfQuote1 + 1);
				string value = text.Substring(indexOfQuote1 + 1, indexOfQuote2 - indexOfQuote1 - 1);
				index = indexOfQuote2 + 1;

				node.Attributes.Add(name, value);
			}
		}

		public static DocumentNode ParseTags(string text)
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
					}
					//else if (jc == '?')
					//{
					//    // this only matters if I start parsing server-side directives e.g.
					//    // <?php...?>
					//    // otherwise, it is just things like xml declarations which can be parsed the default way e.g.
					//    // <?xml version optional parts?>
					//}
					else
					{
						// accumulate until whitespace or end
						//int nameStartIndex = j;
						while (j < tagEndIndex)
						{
							if (Char.IsWhiteSpace(text[j]))
								break;
							++j;
						}

						int tagNameLength = j - i - 1;

						bool isClosingTag = text[i + 1] == '/';
						if (isClosingTag)
						{
							current = current.Parent;
						}
						else
						{
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
					var node = new CommentNode(text.Substring(i + 4, tagEndIndex - i - 6).Trim());
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
	}
}
