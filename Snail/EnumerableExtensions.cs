using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkupParser
{
	/// <summary>
	/// http://gregbeech.com/blog/from-projections-to-comparers
	/// </summary>
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Distinct<T, TKey>(
			this IEnumerable<T> source,
			Func<T, TKey> keySelector)
		{
			var comparer = new KeyEqualityComparer<T, TKey>(keySelector);
			return source.Distinct(comparer);
		}

		public static IEnumerable<T> Distinct<T, TKey>(
			this IEnumerable<T> source,
			Func<T, TKey> keySelector,
			IEqualityComparer<TKey> keyEqualityComparer)
		{
			var comparer = new KeyEqualityComparer<T, TKey>(keySelector, keyEqualityComparer);
			return source.Distinct(comparer);
		}
	}

	public sealed class KeyEqualityComparer<T, TKey> : IEqualityComparer<T>
	{
		private readonly IEqualityComparer<TKey> m_equalityComparer;
		private readonly Func<T, TKey> m_keySelector;

		public KeyEqualityComparer(Func<T, TKey> keySelector)
			: this(keySelector, EqualityComparer<TKey>.Default)
		{
		}

		public KeyEqualityComparer(Func<T, TKey> keySelector, IEqualityComparer<TKey> equalityComparer)
		{
			m_keySelector = keySelector;
			m_equalityComparer = equalityComparer;
		}

		public bool Equals(T x, T y)
		{
			return m_equalityComparer.Equals(m_keySelector(x), m_keySelector(y));
		}

		public int GetHashCode(T obj)
		{
			return m_equalityComparer.GetHashCode(m_keySelector(obj));
		}
	}
}
