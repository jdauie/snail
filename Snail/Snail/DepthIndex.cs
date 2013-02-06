using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Snail
{
	public class DepthIndex
	{
		#region Depth Index Format

		public const int TokenIndexBits = 30;
		public const int TokenFirstChildBits = 30;
		public const int TokenIsLastChildBits = 4;

		public const int TokenIndexMax = (1 << TokenIndexBits) - 1;
		public const int TokenFirstChildMax = (1 << TokenFirstChildBits) - 1;
		public const int TokenIsLastChildMax = (1 << TokenIsLastChildBits) - 1;

		public const int TokenIndexShift = 0;
		public const int TokenFirstChildShift = TokenIndexShift + TokenIndexBits;
		public const int TokenIsLastChildShift = TokenFirstChildShift + TokenFirstChildBits;

		#endregion

		private readonly List<long>[] m_depths;

		private int m_maxActivatedDepth;

		private long m_currentIndex;
		private long m_currentDepth;

		public DepthIndex(int maxDepth)
		{
			m_depths = new List<long>[maxDepth];
			m_maxActivatedDepth = -1;
			m_currentDepth = 0;
		}

		/// <summary>
		/// Adds an attribute.
		/// </summary>
		public void Add()
		{
			Add(m_currentDepth);
		}

		public void Add(long depth)
		{
			if (depth > m_maxActivatedDepth)
			{
				++m_maxActivatedDepth;
				m_depths[m_maxActivatedDepth] = new List<long>();
			}

			if (depth > m_currentDepth)
			{
				SetFirstChildForCurrent();
			}
			else if (depth < m_currentDepth)
			{
				SetCurrentAsLastChild(depth);
			}

			++m_currentIndex;
			m_depths[depth].Add(m_currentIndex);
		}

		private void SetCurrentAsLastChild(long depth)
		{
			// this also needs to be done in "finalization", to deal with the last nodes
			while (m_currentDepth != depth)
			{
				// mark current as last child
				var c = m_depths[m_currentDepth];
				c[c.Count - 1] |= (1 << TokenIsLastChildShift);
				--m_currentDepth;
			}
		}

		private void SetFirstChildForCurrent()
		{
			// save ref to first child (which will be added after this call)
			var current = m_depths[m_currentDepth];
			++m_currentDepth;
			var next = m_depths[m_currentDepth];
			current[current.Count - 1] |= (next.Count << TokenFirstChildShift);
		}
	}
}
