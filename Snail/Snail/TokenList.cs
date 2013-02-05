﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail
{
	/// <summary>
	/// This can be better for massive files.
	/// </summary>
	public class TokenList : IEnumerable<TokenBase> // : IEnumerable<long>
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
			//m_depthIndex = new List<List<int>>(TokenDataNodeDepthMax);
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

		#region Token Bits

		/// <summary>Type is common to all token formats, including attributes.</summary>
		public const int TokenTypeBits            = 4;
		public const int TokenIndexBits           = 30;
		public const int TokenDataBits            = 30;

		public const int TokenAttrOffsetBits      = 14;
		public const int TokenAttrQNameBits       = 11;
		public const int TokenAttrPrefixBits      = 9;
		public const int TokenAttrValueOffsetBits = 10;
		public const int TokenAttrValueLengthBits = 16;

		public const int TokenDataNodeBits        = 22;
		public const int TokenDataNodeDepthBits   = 8;

		public const int TokenDataNodeQNameBits   = 11;
		public const int TokenDataNodePrefixBits  = 9;
		public const int TokenDataNodeOtherBits   = 2;

		public const int TokenDataNodeTargetBits  = 9;
		public const int TokenDataNodeContentBits = 13;

		public const int TokenDataNodeLengthBits  = 22;

		#endregion

		#region Token Max Values (post-shift masks)
		
		public const int TokenTypeMax            = (1 << TokenTypeBits) - 1;
		public const int TokenIndexMax           = (1 << TokenIndexBits) - 1;
		public const int TokenDataMax            = (1 << TokenDataBits) - 1;

		public const int TokenAttrOffsetMax      = (1 << TokenAttrOffsetBits) - 1;
		public const int TokenAttrQNameMax       = (1 << TokenAttrQNameBits) - 1;
		public const int TokenAttrPrefixMax      = (1 << TokenAttrPrefixBits) - 1;
		public const int TokenAttrValueOffsetMax = (1 << TokenAttrValueOffsetBits) - 1;
		public const int TokenAttrValueLengthMax = (1 << TokenAttrValueLengthBits) - 1;

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
		
		public const int TokenTypeShift            = 0; // NOT NECESSARY
		public const int TokenIndexShift           = TokenTypeShift + TokenTypeBits;
		public const int TokenDataShift            = TokenIndexShift + TokenIndexBits;

		public const int TokenAttrOffsetShift      = TokenTypeShift;
		public const int TokenAttrQNameShift       = TokenAttrOffsetShift + TokenAttrOffsetBits;
		public const int TokenAttrPrefixShift      = TokenAttrQNameShift + TokenAttrQNameBits;
		public const int TokenAttrValueOffsetShift = TokenAttrPrefixShift + TokenAttrPrefixBits;
		public const int TokenAttrValueLengthShift = TokenAttrValueOffsetShift + TokenAttrValueOffsetBits;

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

		public TokenBase ConvertToken(long token)
		{
			var type = ((token >> TokenTypeShift) & TokenTypeMax);
			var typeBasic = (TokenBasicType)(type & 3);
			
			if (typeBasic == TokenBasicType.Text)
			{
				var index  = ((token >> TokenIndexShift) & TokenIndexMax);
				var length = ((token >> TokenDataNodeLengthShift) & TokenDataNodeLengthMax);
				var depth  = ((token >> TokenDataNodeDepthShift) & TokenDataNodeDepthMax);
				return new TokenRegion(this, (int)index, (TokenType)type, (int)length, (byte)depth);
			}
			
			if (typeBasic == TokenBasicType.Tag)
			{
				var index  = ((token >> TokenIndexShift) & TokenIndexMax);
				var qName  = ((token >> TokenDataNodeQNameShift) & TokenDataNodeQNameMax);
				var prefix = ((token >> TokenDataNodePrefixShift) & TokenDataNodePrefixMax);
				var depth  = ((token >> TokenDataNodeDepthShift) & TokenDataNodeDepthMax);
				return new TokenTag(this, (int)index, (int)qName, (int)prefix, (byte)depth);
			}
			
			if (typeBasic == TokenBasicType.Special)
			{
				// decl:   long index, long qName, long depth

				// region: long index, TokenType type, long length, long depth
				//         (comment, cdata)

				// proc:   long index, long target, long content, long depth
				return null;
			}
			
			if (typeBasic == TokenBasicType.Attribute)
			{
				return null;
#warning offset is from index of last tag
				var offset = ((token >> TokenAttrOffsetShift) & TokenAttrOffsetMax);
				var qName  = ((token >> TokenAttrQNameShift) & TokenAttrQNameMax);
				var prefix = ((token >> TokenAttrPrefixShift) & TokenAttrPrefixMax);
				var valJmp = ((token >> TokenAttrValueOffsetShift) & TokenAttrValueOffsetMax);
				var valLen = ((token >> TokenAttrValueLengthShift) & TokenAttrValueLengthMax);
				return new TokenAttr(this, (int)offset, (int)qName, (int)prefix, (int)valJmp, (int)valLen);
			}

			return null;
		}

		private static long CreateTagToken(long index, long qName, long prefix, long depth)
		{
			return ((long)TokenType.OpeningTag) |
				(index   << TokenIndexShift) |
				(qName   << TokenDataNodeQNameShift) |
				(prefix  << TokenDataNodePrefixShift) |
				(depth   << TokenDataNodeDepthShift);
		}

		private static long CreateDeclToken(long index, long qName, long depth)
		{
			return ((long)TokenType.Declaration) |
				(index   << TokenIndexShift) |
				(qName   << TokenDataNodeQNameShift) |
				(depth   << TokenDataNodeDepthShift);
		}

		private static long CreateRegionToken(long index, TokenType type, long length, long depth)
		{
			return ((long)type << TokenTypeShift) |
				(index   << TokenIndexShift) |
				(length  << TokenDataNodeLengthShift) |
				(depth   << TokenDataNodeDepthShift);
		}

		private static long CreateProcToken(long index, long target, long content, long depth)
		{
			return ((long)TokenType.Processing) |
				(index   << TokenIndexShift) |
				(target  << TokenDataNodeTargetShift) |
				(content << TokenDataNodeContentShift) |
				(depth   << TokenDataNodeDepthShift);
		}

		private static long CreateAttrToken(long offset, long qname, long prefix, long valJmp, long valLen)
		{
			return ((long)TokenType.Attribute) |
				(offset  << TokenAttrOffsetShift) |
				(qname   << TokenAttrQNameShift) |
				(prefix  << TokenAttrPrefixShift) |
				(valJmp  << TokenAttrValueOffsetShift) |
				(valLen  << TokenAttrValueLengthShift);
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
			//// Precondition: MIN <= (x - y) <= MAX
			//// min(length, TokenDataNodeLengthMax)
			//long clippedLength = TokenDataNodeLengthMax + ((length - TokenDataNodeLengthMax) & ((length - TokenDataNodeLengthMax) >> 63));
			//Add(CreateRegionToken(index, type, clippedLength, depth));
			//if (clippedLength == TokenDataNodeLengthMax)
			//    Add(length);

			if (length >= TokenDataNodeLengthMax)
			{
				// add two tokens
				Add(CreateRegionToken(index, type, TokenDataNodeLengthMax, depth));
				// how common will this be?
				// would it be better to use a seperate lookup table for the few massive regions?
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

		public void AddAttr(long index, long qName, long prefix, long valueOffset, long valueLength)
		{
			Add(CreateAttrToken(index, qName, prefix, valueOffset, valueLength));
		}

		#endregion

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

		private void CreateChunk()
		{
			m_chunks.Add(m_current);
			m_current = new long[m_index];
			m_index = 0;
		}

		private IEnumerable<long> GetEnumeratorInternal()
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

		#region IEnumerable Members

		public IEnumerator<TokenBase> GetEnumerator()
		{
			foreach (var token in GetEnumeratorInternal())
			{
				var t = ConvertToken(token);
				if (t != null)
					yield return ConvertToken(token);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
