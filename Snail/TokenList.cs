using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail
{
	public enum TokenType : byte
	{
		Text        = 0,
		OpeningTag  = 1,
		ClosingTag  = 2,

		// special blocks
		Comment     = 3,
		CDATA       = 4,
		Declaration = 5,
		Processing  = 6, // currently, this includes "<?xml ...>"

		AttrName    = 7,
		AttrNS      = 8,
		AttrValue   = 9,

		Reserved10 = 10,
		Reserved11 = 11,
		Reserved12 = 12,
		Reserved13 = 13,
		Reserved14 = 14,
		Reserved15 = 15
	}

	//public const int TOKEN_PI_NAME = 7;
	//public const int TOKEN_PI_VAL = 8;

	//public const int TOKEN_DEC_ATTR_NAME = 9;
	//public const int TOKEN_DEC_ATTR_VAL = 10;

	//public const int TOKEN_DTD_VAL = 12;
	//public const int TOKEN_DOCUMENT = 13;

	/// <summary>
	/// This can be better for massive files.
	/// </summary>
	public class TokenList : IEnumerable<long>
	{
		private const int CHUNK_SIZE = (1 << 20) / sizeof(long);

		public const int BITS_INDEX  = 30;
		public const int BITS_LENGTH = 20;
		public const int BITS_DEPTH  = 8;
		public const int BITS_TYPE   = 4;

		public const int MAX_INDEX   = (1 << BITS_INDEX) - 1;
		public const int MAX_LENGTH  = (1 << BITS_LENGTH) - 1;
		public const int MAX_DEPTH   = (1 << BITS_DEPTH) - 1;
		public const int MAX_TYPE    = (1 << BITS_TYPE) - 1;

		private readonly List<long[]> m_chunks;
		private long[] m_current;
		private int m_index;

		public TokenList()
		{
			m_chunks = new List<long[]>();
			m_current = new long[CHUNK_SIZE];
			m_index = 0;
		}

		public int Count
		{
			get { return (m_chunks.Count * CHUNK_SIZE) + m_index; }
		}

		public int Capacity
		{
			get { return ((m_chunks.Count + 1) * CHUNK_SIZE); }
		}

		public long this[int index]
		{
			get
			{
				int chunkIndex = index / CHUNK_SIZE;
				int localIndex = index % CHUNK_SIZE;

				if (chunkIndex < m_chunks.Count)
				{
					return m_chunks[chunkIndex][localIndex];
				}

				if (chunkIndex == m_chunks.Count)
				{
					if(localIndex > m_index)
						throw new ArgumentException("index out of range");

					return m_current[localIndex];
				}

				throw new ArgumentException("chunk out of range");
			}
		}

		public static Token CreateToken(string text, long token)
		{
			long index  = (token & MAX_INDEX);
			long length = ((token >> (BITS_INDEX)) & MAX_LENGTH);
			long depth  = ((token >> (BITS_INDEX + BITS_LENGTH)) & MAX_DEPTH);
			long type   = ((token >> (BITS_INDEX + BITS_LENGTH + BITS_DEPTH)) & MAX_TYPE);

			return new Token(text, (int)index, (int)length, (int)depth, (TokenType)type);
		}

		private static long CreateToken(long index, long length, long depth, TokenType type)
		{
			return (index) | (length << BITS_INDEX) | (depth << (BITS_INDEX + BITS_LENGTH)) | ((long)type << (BITS_INDEX + BITS_LENGTH + BITS_DEPTH));
		}

		/// <summary>
		/// format : [  30  ][  20  ][  8  ][  4  ][  2  ]
		///           index   length  depth  type   ?
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="length">The length.</param>
		/// <param name="depth">The depth.</param>
		/// <param name="type">The type.</param>
		public void Add(long index, long length, long depth, TokenType type)
		{
			if (m_index == CHUNK_SIZE)
				CreateChunk();

			if (length > MAX_LENGTH)
			{
				m_current[m_index] = CreateToken(index, MAX_LENGTH, depth, type);
				++m_index;
				AddLength(length);
			}
			else
			{
				m_current[m_index] = CreateToken(index, length, depth, type);
				++m_index;
			}
		}

		public void AddTag(long index, long length, long depth)
		{
			Add(index, length, depth, TokenType.OpeningTag);
		}

		public void AddAttribute(long index, long length, long depth)
		{
			
		}

		public void AddText(long index, long length, long depth)
		{
			Add(index, length, depth, TokenType.Text);
		}

		private void AddLength(long length)
		{
			if (m_index == CHUNK_SIZE)
				CreateChunk();

			m_current[m_index] = length;
			++m_index;
		}

		public void AddWhitespace()
		{
			if (m_index == CHUNK_SIZE)
				CreateChunk();

			m_current[m_index] = 0;
			++m_index;
		}

		private void CreateChunk()
		{
			m_chunks.Add(m_current);
			m_current = new long[m_index];
			m_index = 0;
		}

		public IEnumerator<long> GetEnumerator()
		{
			foreach (var chunk in m_chunks)
				foreach (var tag in chunk)
					yield return tag;

			if (m_index > 0)
			{
				for (int i = 0; i < m_index; i++)
					yield return m_current[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
