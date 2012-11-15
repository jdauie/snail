using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Snail.Nodes;

namespace Snail
{
	public class Document
	{
		public static void Parse(string text)
		{
			var tokens = Lexer.Tokenize(text);
			//var tags = Parser.ParseTokens(tokens);
			//Parser.CalculateTagDepths(tokens, tags);
			//var tree = Parser.ParseTagsToTree(tokens, tags);

			//string text1 = tree.ToFormattedString();
			//string text2 = tree.ToFormattedString(WhitespaceMode.Strip);
			//string text3 = tree.ToFormattedString(WhitespaceMode.Insert, "\t");

			//string testFile = @"\projects\Snail\test1.htm";
			//File.WriteAllText(testFile, text2);
		}

		public static int Parse2(string text)
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
				// exclude search char
				var textEndIndex = text.IndexOf('<', i);
				if (textEndIndex != i)
				{
					if (textEndIndex == -1)
						textEndIndex = text.Length;

					tags.Add(i | (((long)textEndIndex - i) << 24));

					i = textEndIndex;
				}

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
						// check if this is a comment block and find the ending "-->"
						if (String.Compare(text, j + 1, "--", 0, "--".Length) == 0)
						{
							// find -->
							// In the future, check if the tagEndIndex is correct already.  This would save some time searching the same region again.

							// add entire comment as one tag

							// why is this so expensive?
							// should I just keep getting the next '>' char and checking the 2 chars in front of it?

							int closeCommentIndex = text.IndexOf("-->", j + 3);
							if (closeCommentIndex != -1)
							{
								tagEndIndex = closeCommentIndex + "-->".Length - 1;
								tags.Add(i | (((long)tagEndIndex - i + 1) << 24));
							}
						}
						// else
						// doctype, etc.
					}
					else if (jc == '?')
					{
						// something
					}
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

						//if (tagNameLength == "script".Length && String.Compare(text, j, "script", 0, "script".Length, true) == 0)
						//{
						//    // find </script>
						//    //int endTagStartIndex = text.IndexOf("</script>", StringComparison.OrdinalIgnoreCase);
						//    //if (endTagStartIndex != -1)
						//    //{
						//    //    // current (start) tag
						//    //    tags.Add(i | (((long)tagEndIndex - i + 1) << 32));
						//    //    // special contents
						//    //    tags.Add(i | (((long)tagEndIndex - i + 1) << 32));
						//    //    // closing tag
						//    //    tags.Add(i | (((long)tagEndIndex - i + 1) << 32));
						//    //}
						//}
						//else if (tagNameLength == "style".Length && String.Compare(text, j, "style", 0, "style".Length, true) == 0)
						//{
						//    // find </style>
						//}

						//long tagLengthIncludingBrackets = tagEndIndex - i + 1;
						tags.Add(i | (((long)tagEndIndex - i + 1) << 24) | (((long)tagNameLength) << 48));
					}
					i = tagEndIndex;
				}

				++i;
			}

			// low...high
			// [  24  ][  24  ][  16  ]
			//  index   count   other

			//var test = new List<string>();
			//var sb = new StringBuilder(text.Length);
			//foreach (var tag in tags)
			//{
			//    int index = ((int)tag << 8) >> 8;
			//    int length = (int)((tag << 16) >> (24 + 16));
			//    ushort other = (ushort)(tag >> 64 - 16);

			//    //sb.Append(text.Substring(index, length));

			//    string s = null;
			//    if (other != 0)
			//    {
			//        test.Add(string.Format("<{0}>", text.Substring(index + 1, other)));
			//    }

			//    sb.Append(text.Substring(index, length));
			//}
			//var textRecreated = sb.ToString();
			//Console.WriteLine(textRecreated.Length);

			//var textRecreated = string.Join("", tags);
			//Console.WriteLine(textRecreated.Length);

			return tags.Count;
		}
	}
}
