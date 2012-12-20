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

			string text = File.ReadAllText(testFile);

			GC.Collect();
			GC.Collect();

			var obj = ParseTime(text);
			var sw = (Stopwatch)obj[0];
			var tags = (TagList)obj[1];

			var status = string.Format("parse:\t{0} ms\n", sw.Elapsed.TotalMilliseconds);
			status += string.Format("tags:\t{0:#,#} ({1})\n", tags.Count, ((long)tags.Count * sizeof(long)));

			//var status = "";
			//status += string.Format("parse:\t{0} ms\n", sw.Elapsed.TotalMilliseconds);
			//status += string.Format("chars:\t{0:#,#}\n", text.Length);
			//status += string.Format("tags:\t{0:#,#} ({1:#,#,,.00} MB)\n", tags.Count, ((long)tags.Count * sizeof(long)));

			Console.WriteLine(status);
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
