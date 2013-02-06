using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Snail
{
	public class ChunkList<T> : IEnumerable<T> where T : struct
	{
		private const int CHUNK_SIZE_BYTES = (1 << 20);

		private readonly int m_chunkLength;
		private readonly List<T[]> m_chunks;
		private T[] m_current;
		private int m_index;

		public ChunkList()
		{
			m_chunkLength = CHUNK_SIZE_BYTES / Marshal.SizeOf(default(T));
			m_chunks = new List<T[]>();
			m_current = new T[m_chunkLength];
			m_index = 0;
		}

		public int Count
		{
			get { return (m_chunks.Count * m_chunkLength) + m_index; }
		}

		public int Capacity
		{
			get { return ((m_chunks.Count + 1) * m_chunkLength); }
		}

		public T this[int index]
		{
			get
			{
				int chunkIndex = index / m_chunkLength;
				int localIndex = index % m_chunkLength;

				if (chunkIndex < m_chunks.Count)
				{
					return m_chunks[chunkIndex][localIndex];
				}

				if (chunkIndex == m_chunks.Count)
				{
					if (localIndex > m_index)
						throw new ArgumentException("index out of range");

					return m_current[localIndex];
				}

				throw new ArgumentException("chunk out of range");
			}
		}

		public void Add(T token)
		{
			if (m_index == m_chunkLength)
				CreateChunk();

			m_current[m_index] = token;
			++m_index;
		}

		private void CreateChunk()
		{
			m_chunks.Add(m_current);
			m_current = new T[m_index];
			m_index = 0;
		}

		#region IEnumerable Members

		public IEnumerator<T> GetEnumerator()
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
