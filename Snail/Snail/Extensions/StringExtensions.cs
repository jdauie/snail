using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail
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

		public static string SubstringTrim(this string input, int startIndex, int length)
		{
			int endIndex = startIndex + length - 1;

			for (; startIndex <= endIndex; startIndex++)
			{
				if (!Char.IsWhiteSpace(input[startIndex])) break;
			}

			for (; endIndex >= startIndex; endIndex--)
			{
				if (!Char.IsWhiteSpace(input[endIndex])) break;
			}

			return input.Substring(startIndex, endIndex - startIndex + 1);
		}
	}


}
