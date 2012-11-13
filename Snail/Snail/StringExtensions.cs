using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkupParser
{
	public static class StringExtensions
	{
		public static string Repeat(this string input, int count)
		{
			var builder = new StringBuilder((input == null ? 0 : input.Length) * count);

			for (int i = 0; i < count; i++)
				builder.Append(input);

			return builder.ToString();
		}
	}


}
