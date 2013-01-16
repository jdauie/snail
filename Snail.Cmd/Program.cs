using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Snail.Cmd
{
	class Program
	{
		static void Main(string[] args)
		{
			string testFile;

			testFile = @"..\Snail.Test\kjv-osis\kjv.osis.xml";
			//testFile = @"..\Snail.Test\xmark\standard.xml";
			//testFile = @"..\Snail.Test\pugixml-benchmark-data\house.dae";
			//testFile = @"..\Snail.Test\pugixml-benchmark-data\terrover.xml";
			//testFile = @"..\Snail.Test\afe\Precompaction_Lrn.shp.afe";

			string text = File.ReadAllText(testFile);

			var status = string.Format("\nfile\t: {0}\n", testFile);
			status += string.Format("size\t: {0:f} MB\n", (double)(new FileInfo(testFile).Length) / (1 << 20));

			GC.Collect();
			GC.Collect();

			var obj = ParseTime(text);
			var sw = (Stopwatch)obj[0];
			var tokens = (TokenList)obj[1];

			//var tokenList = new List<TokenBase>();
			//foreach (var token in tokens)
			//{
			//    tokenList.Add(tokens.ConvertToken(token));
			//}
			//var result = tokens.Analyze();
			//result.Add(0);

			status += string.Format("parse\t: {0:.00} ms\n", sw.Elapsed.TotalMilliseconds);
			status += string.Format("tokens\t: {0:#,#} ({1:f} MB)\n", tokens.Count, (tokens.Count * ((double)sizeof(long) / (1 << 20))));

			//var status = "";
			//status += string.Format("parse:\t{0} ms\n", sw.Elapsed.TotalMilliseconds);
			//status += string.Format("chars:\t{0:#,#}\n", text.Length);
			//status += string.Format("tags:\t{0:#,#} ({1:#,#,,.00} MB)\n", tags.Count, ((long)tags.Count * sizeof(long)));

			Console.Write(status);
		}

		private static object[] ParseTime(string text)
		{
			var sw = Stopwatch.StartNew();

			var tags = Document.ParseXml(text);

			sw.Stop();
			return new object[] { sw, tags };
		}
	}
}
