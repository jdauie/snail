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

		private const int BITS_INDEX = 30;
		private const int BITS_LENGTH = 20;
		private const int BITS_DEPTH = 8;
		private const int BITS_TYPE = 4;

		private const int MAX_INDEX = (1 << 30) - 1;
		private const int MAX_LENGTH = (1 << 20) - 1;
		private const int MAX_DEPTH = (1 << 8) - 1;

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

		public void AddMarkup(long index, long length, long depth, TokenType type)
		{
			
		}

		public void AddAttribute(long index, long length, long depth)
		{
			
		}

		public void AddText(long index, long length, long depth)
		{
			
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
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
