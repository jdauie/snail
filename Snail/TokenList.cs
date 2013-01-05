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

	public class TokenGroup
	{
		//public readonly int TokenOffset;
		public readonly int CharOffset;
		public readonly long[] Data;

		public TokenGroup(int charOffset, long[] source, int count)
		{
			//TokenOffset = tokenOffset;
			CharOffset = charOffset;
			Data = new long[count];
			Array.Copy(source, Data, count);
		}

		public override string ToString()
		{
			return string.Format("Offset: {0} ({1} tokens)", CharOffset, Data.Length);
		}
	}

	/// <summary>
	/// This can be better for massive files.
	/// </summary>
	public class TokenList : IEnumerable<long>
	{
		private const int CHUNK_SIZE = (1 << 20) / sizeof(long);
		private const int INDEX_RANGE = (1 << 18);

		public const int BITS_INDEX  = 30;
		public const int BITS_LENGTH = 20;
		public const int BITS_DEPTH  = 8;
		public const int BITS_TYPE   = 4;

		public const int MAX_INDEX   = (1 << BITS_INDEX) - 1;
		public const int MAX_LENGTH  = (1 << BITS_LENGTH) - 1;
		public const int MAX_DEPTH   = (1 << BITS_DEPTH) - 1;
		public const int MAX_TYPE    = (1 << BITS_TYPE) - 1;

		private readonly string m_text;
		private readonly List<TokenGroup> m_chunks;
		private readonly long[] m_current;
		private int m_index;

		//private int m_currentTokenOffset;
		private int m_currentCharOffset;

		public TokenList(string text)
		{
			m_text = text;
			m_chunks = new List<TokenGroup>();
			m_current = new long[CHUNK_SIZE];
			m_index = 0;

			//m_currentTokenOffset = 0;
			m_currentCharOffset = 0;
		}

		public string Text
		{
			get { return m_text; }
		}

		public int Count
		{
			get
			{
				int count = m_index;
				foreach (var chunk in m_chunks)
					count += chunk.Data.Length;
				return count;
			}
		}

		//public int Capacity
		//{
		//    get { return ((m_chunks.Count + 1) * CHUNK_SIZE); }
		//}

		public long this[int index]
		{
			get
			{
				int chunkIndex = index / CHUNK_SIZE;
				int localIndex = index % CHUNK_SIZE;

				if (chunkIndex < m_chunks.Count)
				{
					return m_chunks[chunkIndex].Data[localIndex];
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

		public Token CreateToken(long token)
		{
			long index  = (token & MAX_INDEX);
			long length = ((token >> (BITS_INDEX)) & MAX_LENGTH);
			long depth  = ((token >> (BITS_INDEX + BITS_LENGTH)) & MAX_DEPTH);
			long type   = ((token >> (BITS_INDEX + BITS_LENGTH + BITS_DEPTH)) & MAX_TYPE);

			return new Token(this, (int)index, (int)length, (int)depth, (TokenType)type);
		}

		private static long CreateToken(long index, long length, long depth, TokenType type)
		{
			return (index) | (length << BITS_INDEX) | (depth << (BITS_INDEX + BITS_LENGTH)) | ((long)type << (BITS_INDEX + BITS_LENGTH + BITS_DEPTH));
		}

		//public List<int> Analyze()
		//{
		//    var chunks = new List<long[]>(m_chunks);
		//    if (m_index != 0)
		//    {
		//        var currentSlice = new long[m_index];
		//        Array.Copy(m_current, currentSlice, m_index);
		//        chunks.Add(currentSlice);
		//    }

		//    var chunkRanges = new List<int>();

		//    foreach (var chunk in chunks)
		//    {
		//        var firstToken = CreateToken(chunk[0]);
		//        var lastToken = CreateToken(chunk[chunk.Length - 1]);

		//        chunkRanges.Add(lastToken.Index - firstToken.Index);
		//    }

		//    return chunkRanges;
		//}

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
			if (m_index == CHUNK_SIZE || (index - m_currentCharOffset > INDEX_RANGE))
				CreateChunk(index);

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
			// INVALID
			throw new Exception();

			if (m_index == CHUNK_SIZE)
				CreateChunk(0);

			m_current[m_index] = length;
			++m_index;
		}

		public void AddWhitespace()
		{
			// INVALID
			throw new Exception();

			if (m_index == CHUNK_SIZE)
				CreateChunk(0);

			m_current[m_index] = 0;
			++m_index;
		}

		private void CreateChunk(long index)
		{
			m_chunks.Add(new TokenGroup(m_currentCharOffset, m_current, m_index));

			//m_currentTokenOffset += m_index;
			m_currentCharOffset = (int)index;

			m_index = 0;
		}

		public IEnumerator<long> GetEnumerator()
		{
			foreach (var chunk in m_chunks)
				foreach (var tag in chunk.Data)
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
