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

		Attr        = 7,
		AttrNS      = 8,

		Reserved09 = 9,
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

		private readonly string m_text;
		private readonly List<long[]> m_chunks;
		private long[] m_current;
		private int m_index;

		//private readonly List<List<int>> m_depthIndex;

		public TokenList(string text)
		{
			m_text = text;
			m_chunks = new List<long[]>();
			m_current = new long[CHUNK_SIZE];
			m_index = 0;

			// should this be lazy?
			//m_depthIndex = new List<List<int>>(MAX_DEPTH);
			//for (int i = 0; i < MAX_DEPTH; i++)
			//    m_depthIndex.Add(new List<int>());
		}

		public string Text
		{
			get { return m_text; }
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

		public Token CreateToken(long token)
		{
			//long index  = (token & MAX_INDEX);
			//long length = ((token >> (BITS_INDEX)) & MAX_LENGTH);
			//long depth  = ((token >> (BITS_INDEX + BITS_LENGTH)) & MAX_DEPTH);
			//long type   = ((token >> (BITS_INDEX + BITS_LENGTH + BITS_DEPTH)) & MAX_TYPE);

			//return new Token(this, (int)index, (int)length, (int)depth, (TokenType)type);
			return new Token();
		}

		#region Token Bits

		/// <summary>
		/// token    = [         64        ]
		///          = [  30  ][  4  ][ 30 ]
		///             index   type   @1
		/// </summary>
		public const int TokenIndexBits           = 30;
		public const int TokenTypeBits            = 4;
		public const int TokenDataBits            = 30;

		/// <summary>
		/// @1(attr) = [  11  ][   9   ][  10  ]
		///             qname   prefix   value
		/// </summary>
		public const int TokenDataAttrQNameBits   = 11;
		public const int TokenDataAttrPrefixBits  = 9;
		public const int TokenDataAttrValueBits   = 10;

		/// <summary>
		/// @1(node) = [ 22 ][  8  ]
		///             @2   depth
		/// </summary>
		public const int TokenDataNodeBits        = 22;
		public const int TokenDataNodeDepthBits   = 8;

		/// <summary>
		/// @2(decl),
		/// @2(tag)  = [  11  ][   9   ][  2  ]
		///             qname   prefix    ?
		/// </summary>
		public const int TokenDataNodeQNameBits   = 11;
		public const int TokenDataNodePrefixBits  = 9;
		public const int TokenDataNodeOtherBits   = 2;

		/// <summary>
		/// @2(proc) = [  9  ][   13   ]
		///             target  content
		/// </summary>
		public const int TokenDataNodeTargetBits  = 9;
		public const int TokenDataNodeContentBits = 13;

		/// <summary>
		/// @2(comment),
		/// @2(cdata),
		/// @2(text) = [  22  ]
		///             length
		/// </summary>
		public const int TokenDataNodeLengthBits  = 22;

		#endregion

		#region Token Max Values (post-shift masks)

		public const int TokenIndexMax           = (1 << TokenIndexBits) - 1;
		public const int TokenTypeMax            = (1 << TokenTypeBits) - 1;
		public const int TokenDataMax            = (1 << TokenDataBits) - 1;

		public const int TokenDataAttrQNameMax   = (1 << TokenDataAttrQNameBits) - 1;
		public const int TokenDataAttrPrefixMax  = (1 << TokenDataAttrPrefixBits) - 1;
		public const int TokenDataAttrValueMax   = (1 << TokenDataAttrValueBits) - 1;

		public const int TokenDataNodeMax        = (1 << TokenDataNodeBits) - 1;
		public const int TokenDataNodeDepthMax   = (1 << TokenDataNodeDepthBits) - 1;

		public const int TokenDataNodeQNameMax   = (1 << TokenDataNodeQNameBits) - 1;
		public const int TokenDataNodePrefixMax  = (1 << TokenDataNodePrefixBits) - 1;
		public const int TokenDataNodeOtherMax   = (1 << TokenDataNodeOtherBits) - 1;

		public const int TokenDataNodeTargetMax  = (1 << TokenDataNodeTargetBits) - 1;
		public const int TokenDataNodeContentMax = (1 << TokenDataNodeContentBits) - 1;

		public const int TokenDataNodeLengthMax  = (1 << TokenDataNodeLengthBits) - 1;

		#endregion

		#region Token Shifts

		public const int TokenIndexShift           = 0; // NOT NECESSARY
		public const int TokenTypeShift            = TokenIndexShift + TokenIndexBits;
		public const int TokenDataShift            = TokenTypeShift + TokenTypeBits;

		public const int TokenDataAttrQNameShift   = TokenDataShift;
		public const int TokenDataAttrPrefixShift  = TokenDataAttrQNameShift + TokenDataAttrQNameBits;
		public const int TokenDataAttrValueShift   = TokenDataAttrPrefixShift + TokenDataAttrPrefixBits;

		public const int TokenDataNodeShift        = TokenDataShift;
		public const int TokenDataNodeDepthShift   = TokenDataNodeShift + TokenDataNodeBits;

		public const int TokenDataNodeQNameShift   = TokenDataNodeShift;
		public const int TokenDataNodePrefixShift  = TokenDataNodeQNameShift + TokenDataNodeQNameBits;
		public const int TokenDataNodeOtherShift   = TokenDataNodePrefixShift + TokenDataNodePrefixBits;

		public const int TokenDataNodeTargetShift  = TokenDataNodeShift;
		public const int TokenDataNodeContentShift = TokenDataNodeTargetShift + TokenDataNodeTargetBits;

		public const int TokenDataNodeLengthShift  = TokenDataNodeShift;

		#endregion

		#region Token Creators

		private static long CreateTagToken(long index, long qName, long prefix, long depth)
		{
			return (index) | ((long)TokenType.OpeningTag << TokenTypeShift) | (qName << TokenDataNodeQNameShift) | (prefix << TokenDataNodePrefixShift) | (depth << TokenDataNodeDepthShift);
		}

		private static long CreateDeclToken(long index, long qName, long depth)
		{
			return (index) | ((long)TokenType.Declaration << TokenTypeShift) | (qName << TokenDataNodeQNameShift) | (depth << TokenDataNodeDepthShift);
		}

		private static long CreateRegionToken(long index, TokenType type, long length, long depth)
		{
			return (index) | ((long)type << TokenTypeShift) | (length << TokenDataNodeLengthShift) | (depth << TokenDataNodeDepthShift);
		}

		private static long CreateProcToken(long index, long target, long content, long depth)
		{
			return (index) | ((long)TokenType.Processing << TokenTypeShift) | (target << TokenDataNodeTargetShift) | (content | TokenDataNodeContentShift) | (depth << TokenDataNodeDepthShift);
		}

		private static long CreateAttrToken(long index, long qname, long prefix, long value)
		{
			return (index) | ((long)TokenType.Attr << TokenTypeShift) | (qname << TokenDataNodeQNameShift) | (prefix << TokenDataNodePrefixShift) | (value << TokenDataAttrValueShift);
		}

		#endregion

		#region Add Token Methods

		private void Add(long token)
		{
			if (m_index == CHUNK_SIZE)
				CreateChunk();

			m_current[m_index] = token;
			++m_index;
		}

		public void AddTag(long index, long qName, long prefix, long depth)
		{
			Add(CreateTagToken(index, qName, prefix, depth));
		}

		public void AddDecl(long index, long qName, long depth)
		{
			Add(CreateDeclToken(index, qName, depth));
		}

		public void AddRegion(long index, TokenType type, long length, long depth)
		{
			//// Precondition: INT_MIN <= (x - y) <= INT_MAX
			//// min(length, TokenDataNodeLengthMax)
			//// CHAR_BIT => 8
			//long clippedLength = TokenDataNodeLengthMax + ((length - TokenDataNodeLengthMax) & ((length - TokenDataNodeLengthMax) >> (sizeof(int) * 8 - 1)));
			//Add(CreateRegionToken(index, type, clippedLength, depth));
			//if (length >= TokenDataNodeLengthMax)
			//{
			//    // add another tokens
			//    Add(length);
			//}

			if (length >= TokenDataNodeLengthMax)
			{
				// add two tokens
				Add(CreateRegionToken(index, type, TokenDataNodeLengthMax, depth));
				Add(length);
			}
			else
			{
				Add(CreateRegionToken(index, type, length, depth));
			}
		}

		public void AddProc(long index, long target, long content, long depth)
		{
			Add(CreateProcToken(index, target, content, depth));
		}

		public void AddAttr(long index, long qName, long prefix, long value)
		{
			Add(CreateAttrToken(index, qName, prefix, value));
		}

		#endregion

		public List<int> Analyze()
		{
			var chunks = new List<long[]>(m_chunks);
			if (m_index != 0)
			{
				var currentSlice = new long[m_index];
				Array.Copy(m_current, currentSlice, m_index);
				chunks.Add(currentSlice);
			}

			var chunkRanges = new List<int>();

			foreach (var chunk in chunks)
			{
				var firstToken = CreateToken(chunk[0]);
				var lastToken = CreateToken(chunk[chunk.Length - 1]);

				chunkRanges.Add(lastToken.Index - firstToken.Index);
			}

			return chunkRanges;
		}

		private void CreateChunk()
		{
			m_chunks.Add(m_current);
			m_current = new long[m_index];
			m_index = 0;
		}

		#region IEnumerable Members

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

		#endregion
	}
}
