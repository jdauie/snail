using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Snail.Nodes;

namespace Snail
{
	public class FastParser : IParser
	{
		public DocumentNode Parse(string text)
		{
			var tags = ParseTags(text);
			var root = ParseWellFormedXml(text, tags);

			#region Test

			/* 
			 * [ low . . . . . . high ]
			 * [  24  ][  24  ][  16  ]
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

		public static DocumentNode ParseWellFormedXml(string text, List<long> tags)
		{
			var root = new DocumentNode();
			ElementNode current = root;

			foreach (var tag in tags)
			{
				int index = ((int)tag << 8) >> 8;
				int length = (int)((tag << 16) >> (24 + 16));
				ushort other = (ushort)(tag >> 64 - 16);

				if (other == 0)
				{
					var node = new TextNode(text.Substring(index, length));
					current.AppendChild(node);
				}
				else if (other == ushort.MaxValue)
				{
					var node = new CommentNode(text.Substring(index + 4, length - 7).Trim());
					current.AppendChild(node);
				}
				else
				{
					bool isClosingTag = text[index + 1] == '/';
					if (isClosingTag)
					{
						current = current.Parent;
					}
					else
					{
						bool isSelfClosingTag = text[index + length - 2] == '/';
						string tagName = text.Substring(index + 1, other);
						var node = new ElementNode(tagName, isSelfClosingTag);
						current.AppendChild(node);

						if (!isSelfClosingTag)
						{
							current = node;
						}
					}
				}
			}

			return root;
		}

		public static List<long> ParseTags(string text)
		{
			#region Markup Samples
			// <name attr="value" attr2=value attr3>
			// <name attr="value" />
			// </name>

			// <name attr=value">
			// <name attr="value attr2="value">
			// <name attr=value </name>
			// <name <name2 /></name attr=value>
			// <name>text/name>
			#endregion

			// pre-allocating is only a marginal improvement
			var tags = new List<long>();

			int i = 0;
			while (i < text.Length)
			{
				var textEndIndex = FindTextBlock(text, tags, i);

				i = textEndIndex;
				if (i == text.Length)
					break;

				// include search char
				var tagEndIndex = text.IndexOf('>', i);
				if (tagEndIndex == -1)
					tagEndIndex = text.Length - 1;
				if (tagEndIndex != i)
				{
					// find the element name?
					// check if it is a script/style/comment/processing instruction
					// (so the contents can be skipped)
					
					// skip whitespace? Not for now.
					int j = i + 1;
					char jc = text[j];

					if (jc == '!')
					{
						tagEndIndex = IdentifyTagStartingWithExclamation(text, tags, i, tagEndIndex);
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

						string tagName;
						if (tagNameLength == (tagName = "script").Length && String.Compare(text, i + 1, tagName, 0, tagNameLength) == 0)
						{
							tagEndIndex = FindEndOfSpecialBlock(text, tags, i, tagEndIndex, tagName);
						}
						else if (tagNameLength == (tagName = "style").Length && String.Compare(text, i + 1, tagName, 0, tagNameLength) == 0)
						{
							tagEndIndex = FindEndOfSpecialBlock(text, tags, i, tagEndIndex, tagName);
						}
						else
						{
							//long tagLengthIncludingBrackets = tagEndIndex - textStartIndex + 1;
							//tags.Add(textStartIndex | (((long)tagEndIndex - textStartIndex + 1) << 24) | (((long)tagNameLength) << 48));
							tags.Add(CreateTagIndex(i, tagEndIndex - i + 1, tagNameLength));
						}
					}
					i = tagEndIndex;
				}

				++i;
			}

			return tags;
		}

		private static int FindTextBlock(string text, List<long> tags, int textStartIndex)
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
				tags.Add(CreateTagIndex(textStartIndex, textEndIndex - textStartIndex));
			}

			return textEndIndex;
		}

		private static int FindEndOfSpecialBlock(string text, List<long> tags, int i, int tagEndIndex, string tagName)
		{
			// find end tag
			int searchPos = tagEndIndex + 1;
			while (searchPos != -1)
			{
				searchPos = text.IndexOf('<', searchPos);
				// is this condition correct?
				if (searchPos > text.Length - (tagName.Length + 3))
					searchPos = -1;

				if (searchPos != -1)
				{
					if (text[searchPos + 1] == '/' && text[searchPos + tagName.Length + 2] == '>' && String.Compare(text, searchPos + 2, tagName, 0, tagName.Length, true) == 0)
					{
						break;
					}
					++searchPos;
				}
			}

			if (searchPos != -1)
			{
				// [current (start) tag]
				// [special contents]
				// [closing tag]

				tags.Add(CreateTagIndex(i, tagEndIndex - i + 1, tagName.Length));
				tags.Add(CreateTagIndex(tagEndIndex + 1, searchPos - tagEndIndex - 1));
				tags.Add(CreateTagIndex(searchPos, tagName.Length + 3, tagName.Length + 1));

				tagEndIndex = searchPos + tagName.Length + 2;
			}


			//// find end tag
			//string t2 = "</" + tagName + ">";
			//var t2Length = t2.Length;
			//int endTagStartIndex = text.IndexOf(t2, tagEndIndex + 1, StringComparison.OrdinalIgnoreCase);
			//if (endTagStartIndex != -1)
			//{
			//    // [current (start) tag]
			//    // [special contents]
			//    // [closing tag]

			//    tags.Add(CreateTagIndex(textStartIndex, tagEndIndex - textStartIndex + 1, tagName.Length));
			//    tags.Add(CreateTagIndex(tagEndIndex + 1, endTagStartIndex - tagEndIndex - 1));
			//    tags.Add(CreateTagIndex(endTagStartIndex, t2Length, t2Length - 2));

			//    tagEndIndex = endTagStartIndex + t2Length - 1;
			//}

			return tagEndIndex;
		}

		private static int IdentifyTagStartingWithExclamation(string text, List<long> tags, int i, int tagEndIndex)
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
					tags.Add(CreateTagIndex(i, tagEndIndex - i + 1, int.MaxValue));
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

		private static long CreateTagIndex(int index, int length)
		{
			return (index | (((long)length) << 24));
		}

		private static long CreateTagIndex(int index, int length, int tagNameLength)
		{
			return (index | (((long)length) << 24) | (((long)tagNameLength) << 48));
		}
	}
}
