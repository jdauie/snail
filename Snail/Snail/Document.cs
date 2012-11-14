using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Snail.Nodes;

namespace Snail
{
	// Why is this so slow?
	//internal struct Substring
	//{
	//    public readonly int Index;
	//    public readonly int Count;

	//    public Substring(int index, int count)
	//    {
	//        Index = index;
	//        Count = count;
	//    }
	//}

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
			//var tags = new List<string>(); // THIS IS FOR DEBUGGING ONLY
			var tags = new List<long>(); // THIS IS THE BEST SO FAR, PACKING INDEX AND COUNT

			var nameBuffer = new char[7];

			//var specialBlocks = new string[] { "script", "style" };
			
			int i = 0;
			while (i < text.Length)
			{
				// exclude search char
				var textEndIndex = text.IndexOf('<', i);
				if (textEndIndex != i)
				{
					if (textEndIndex == -1)
						textEndIndex = text.Length;

					//tags.Add(text.Substring(i, textEndIndex - i));
					tags.Add(i | (((long)textEndIndex - i) << 32));

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
					
					// 1) skip any whitespace? Probably not.
					int j = i + 1;
					char jc = text[j];
					if (jc < '[')
						jc += (char)('a' - 'A');

					//if (jc == '!')
					//{
					//    // check if this is a comment block and find the ending "--"
					//}
					//if (jc == '?')
					//{
					//    // processing instruction
					//}
					//else if (jc == 's' || jc == 'S')
					if (jc == 's')
					{
						nameBuffer[0] = 's';
						

						jc = text[j + 1];

						// accumulate until whitespace or end
						int nameStartIndex = j;
						while (j < tagEndIndex)
						{
							if (Char.IsWhiteSpace(text[j]))
								break;
							++j;
						}

						var tagNameLength = j - i - 1;

						if (tagNameLength == 6 && String.Compare(text, i + 1, "script", 0, 6, true) == 0)
						{
							// find </script>
						}
						else if (tagNameLength == 5 && String.Compare(text, i + 1, "style", 0, 5, true) == 0)
						{
							// find </style>
						}
					}

					//while (j < tagEndIndex)
					//{
					//    if (!Char.IsWhiteSpace(text[j]))
					//        break;
					//    ++j;
					//}

					// 2) accumulate until whitespace or end
					//int nameStartIndex = j;
					//while (j < tagEndIndex)
					//{
					//    if (Char.IsWhiteSpace(text[j]))
					//        break;
					//    ++j;
					//}

					//var tagNameLength = j - i - 1;

					// debug
					//if (jc == 's' || jc == 'S')
					//{
					//    var tagName = text.Substring(i + 1, j - i - 1);

					//    if (tagName == "script")
					//    {
					//        ++i;
					//    }
					//}

					//if (tagNameLength == 6 && String.Compare(text, i + 1, "script", 0, 6, true) == 0)
					//{
					//    // find </script>
					//}
					//else if (tagNameLength == 5 && String.Compare(text, i + 1, "style", 0, 5, true) == 0)
					//{
					//    // find </style>
					//}



					//tags.Add(text.Substring(i, tagEndIndex - i + 1));
					tags.Add(i | (((long)tagEndIndex - i + 1) << 32));

					i = tagEndIndex;
				}

				++i;
			}

			//var sb = new StringBuilder(text.Length);
			//foreach (var tag in tags)
			//{
			//    sb.Append(text.Substring((int)tag, (int)(tag >> 32)));
			//}
			//var textRecreated = sb.ToString();
			//Console.WriteLine(textRecreated.Length);

			//var textRecreated = string.Join("", tags);
			//Console.WriteLine(textRecreated.Length);

			return tags.Count;
		}

		private static bool CompareStrings(string s1, string s2)
		{
			return false;
		}

		private static void IsStyleBlock(string text, int j)
		{
			char jc;
			jc = text[j + 2];
			if (jc == 'y' || jc == 'Y')
			{
				jc = text[j + 3];
				if (jc == 'l' || jc == 'L')
				{
					jc = text[j + 4];
					if (jc == 'e' || jc == 'E')
					{
						//yup
					}
				}
			}
		}
	}
}
