using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Snail
{
	public class DepthIndex
	{
		private readonly List<long>[] m_depths;

		private int m_maxActivatedDepth;
 
		public DepthIndex(int maxDepth)
		{
			m_depths = new List<long>[maxDepth];
			m_depths[0] = new List<long>();
			m_maxActivatedDepth = 0;
		}

		public void Add(long index, long depth)
		{

		}
	}

	/// <summary>
	/// This can be better for massive files.
	/// </summary>
	public class TokenList : IEnumerable<TokenBase>
	{
		private readonly string m_text;
		private readonly ChunkList<long> m_list;

		//private readonly List<List<int>> m_depthIndex;

		public TokenList(string text)
		{
			m_text = text;
			m_list = new ChunkList<long>();

			// should this be lazy?
			//m_depthIndex = new List<List<int>>(TokenDataNodeDepthMax);
			//for (int i = 0; i < TokenDataNodeDepthMax; i++)
			//    m_depthIndex.Add(new List<int>());
		}

		public string Text
		{
			get { return m_text; }
		}

		public int Count
		{
			get { return m_list.Count; }
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

		public const int TokenAttrOffsetShift      = TokenTypeShift + TokenTypeBits;
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

		private TokenBase ConvertToken(long token, long indexOffset = 0)
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
				var offset = ((token >> TokenAttrOffsetShift) & TokenAttrOffsetMax);
				var qName  = ((token >> TokenAttrQNameShift) & TokenAttrQNameMax);
				var prefix = ((token >> TokenAttrPrefixShift) & TokenAttrPrefixMax);
				var valJmp = ((token >> TokenAttrValueOffsetShift) & TokenAttrValueOffsetMax);
				var valLen = ((token >> TokenAttrValueLengthShift) & TokenAttrValueLengthMax);
				return new TokenAttr(this, (int)(indexOffset + offset), (int)qName, (int)prefix, (int)valJmp, (int)valLen);
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

		public void AddTag(long index, long qName, long prefix, long depth)
		{
			m_list.Add(CreateTagToken(index, qName, prefix, depth));
		}

		public void AddDecl(long index, long qName, long depth)
		{
			m_list.Add(CreateDeclToken(index, qName, depth));
		}

		public void AddRegion(long index, TokenType type, long length, long depth)
		{
			//if (length >= TokenDataNodeLengthMax)
			//{
			//    // add two tokens
			//    m_list.Add(CreateRegionToken(index, type, TokenDataNodeLengthMax, depth));
			//    // how common will this be?
			//    // would it be better to use a seperate lookup table for the few massive regions?
			//    m_list.Add(length);
			//}
			//else
			//{
			//    m_list.Add(CreateRegionToken(index, type, length, depth));
			//}

			m_list.Add(CreateRegionToken(index, type, length, depth));
		}

		public void AddProc(long index, long target, long content, long depth)
		{
			m_list.Add(CreateProcToken(index, target, content, depth));
		}

		public void AddAttr(long index, long qName, long prefix, long valueOffset, long valueLength)
		{
			m_list.Add(CreateAttrToken(index, qName, prefix, valueOffset, valueLength));
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator<TokenBase> GetEnumerator()
		{
			long lastTagIndex = 0;
			foreach (var token in m_list)
			{
				var t = ConvertToken(token);
				if (t != null)
				{
					var attr = t as TokenAttr;
					if (attr != null)
					{
						t = ConvertToken(token, lastTagIndex);
					}
					else if (t is TokenTag)
					{
						lastTagIndex = t.Index;
					}
					yield return t;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
