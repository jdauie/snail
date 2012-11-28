using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Snail.Nodes;

namespace Snail
{
	interface IParser
	{
		DocumentNode Parse(string text);
	}
}
