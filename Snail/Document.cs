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
		public static DocumentNode Parse(string text)
		{
			IParser parser = null;
			//parser = new LegacyParser();
			//parser = new FastParser();
			//parser = new HtmlParser();
			parser = new XmlParser();
			return parser.Parse(text);
		}

		public static DocumentNode ParseXml(string text)
		{
			var parser = new XmlParser();
			return parser.Parse(text);
		}
	}
}
