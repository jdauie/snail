using System;
using System.Collections.Generic;
using System.Linq;

using Snail.Legacy;
using Snail.Nodes;

namespace Snail
{
	class LegacyParser : IParser
	{
		public DocumentNode Parse(string text)
		{
			var tokens = Lexer.Tokenize(text);
			var tags = Parser.ParseTokens(tokens);
			Parser.CalculateTagDepths(tokens, tags);
			var tree = Parser.ParseTagsToTree(tokens, tags);

			//string text1 = tree.ToFormattedString();
			//string text2 = tree.ToFormattedString(WhitespaceMode.Strip);
			//string text3 = tree.ToFormattedString(WhitespaceMode.Insert, "\t");

			//string testFile = @"\projects\Snail\test1.htm";
			//File.WriteAllText(testFile, text2);

			return tree;
		}
	}
}
