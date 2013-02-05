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
			//testFile = @"..\Snail.Test\afe\Postcompaction_Lrn.shp.afe";

			string text = File.ReadAllText(testFile);

			var status = string.Format("\nfile\t: {0}\n", testFile);
			status += string.Format("size\t: {0:f} MB\n", (double)(new FileInfo(testFile).Length) / (1 << 20));

			GC.Collect();
			GC.Collect();

			var obj = ParseTime(text);
			var sw = obj[0] as Stopwatch;
			var tokens = obj[1] as TokenList;

			//var tokenList = tokens.ToList();
			//var result = tokens.Analyze();
			//result.Add(0);

			status += string.Format("parse\t: {0:.00} ms\n", sw.Elapsed.TotalMilliseconds);
			if (tokens != null)
			{
				status += string.Format("tokens\t: {0:#,#} ({1:f} MB)\n", tokens.Count, (tokens.Count*((double)sizeof (long)/(1 << 20))));
			}

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
