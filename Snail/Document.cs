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
			//IParser parser = new LegacyParser();
			IParser parser = new FastParser();
			return parser.Parse(text);
		}
	}
}
