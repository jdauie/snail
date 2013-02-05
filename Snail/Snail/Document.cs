using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
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
			//parser = new XmlParser();
			return parser.Parse(text);
		}

		public static TokenList ParseXml(string text)
		{
			//var doc = new XmlDocument();
			//doc.LoadXml(text);

			//int count = 0;
			//using (var sr = new StringReader(text))
			//using (var reader = new XmlTextReader(sr))
			//{
			//    while (reader.Read())
			//    {
			//        if (reader.NodeType == XmlNodeType.Element)
			//        {
			//            while (reader.MoveToNextAttribute())
			//            {
			//                ++count;
			//            }
			//        }

			//        ++count;
			//    }
			//}

			//return null;

			var parser = new XmlParser();
			return parser.Parse(text);
		}
	}
}
