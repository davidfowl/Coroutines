using System;
using System.Diagnostics;
using System.Text;

namespace Coroutines
{
    public struct MemoryPoolIterator
    {
        private IMemoryPoolBlock _block;
        private int _index;

        public MemoryPoolIterator(IMemoryPoolBlock block)
        {
            _block = block;
            _index = _block?.Start ?? 0;
        }
        public MemoryPoolIterator(IMemoryPoolBlock block, int index)
        {
            _block = block;
            _index = index;
        }

        public bool IsDefault => _block == null;

        public bool IsEnd
        {
            get
            {
                if (_block == null)
                {
                    return true;
                }
                else if (_index < _block.End)
                {
                    return false;
                }
                else
                {
                    var block = _block.Next;
                    while (block != null)
                    {
                        if (block.Start < block.End)
                        {
                            return false; // subsequent block has data - IsEnd is false
                        }
                        block = block.Next;
                    }
                    return true;
                }
            }
        }

        public IMemoryPoolBlock Block => _block;

        public int Index => _index;

        public int Take()
        {
            var block = _block;
            if (block == null)
            {
                return -1;
            }

            var index = _index;

            if (index < block.End)
            {
                _index = index + 1;
                return block.Data.Array[index];
            }

            do
            {
                if (block.Next == null)
                {
                    return -1;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }

                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    return block.Data.Array[index];
                }
            } while (true);
        }

        public void Skip(int bytesToSkip)
        {
            if (_block == null)
            {
                return;
            }
            var following = _block.End - _index;
            if (following >= bytesToSkip)
            {
                _index += bytesToSkip;
                return;
            }

            var block = _block;
            var index = _index;
            while (true)
            {
                if (block.Next == null)
                {
                    return;
                }
                else
                {
                    bytesToSkip -= following;
                    block = block.Next;
                    index = block.Start;
                }
                following = block.End - index;
                if (following >= bytesToSkip)
                {
                    _block = block;
                    _index = index + bytesToSkip;
                    return;
                }
            }
        }

        public int Peek()
        {
            var block = _block;
            if (block == null)
            {
                return -1;
            }

            var index = _index;

            if (index < block.End)
            {
                return block.Data.Array[index];
            }

            do
            {
                if (block.Next == null)
                {
                    return -1;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }

                if (index < block.End)
                {
                    return block.Data.Array[index];
                }
            } while (true);
        }

        /// <summary>
        /// Save the data at the current location then move to the next available space.
        /// </summary>
        /// <param name="data">The byte to be saved.</param>
        /// <returns>true if the operation successes. false if can't find available space.</returns>
        public bool Put(byte data)
        {
            if (_block == null)
            {
                return false;
            }
            else if (_index < _block.End)
            {
                _block.Data.Array[_index++] = data;
                return true;
            }

            var block = _block;
            var index = _index;
            while (true)
            {
                if (index < block.End)
                {
                    _block = block;
                    _index = index + 1;
                    block.Data.Array[index] = data;
                    return true;
                }
                else if (block.Next == null)
                {
                    return false;
                }
                else
                {
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public int GetLength(MemoryPoolIterator end)
        {
            if (IsDefault || end.IsDefault)
            {
                return -1;
            }

            var block = _block;
            var index = _index;
            var length = 0;
            checked
            {
                while (true)
                {
                    if (block == end._block)
                    {
                        return length + end._index - index;
                    }
                    else if (block.Next == null)
                    {
                        throw new InvalidOperationException("end did not follow iterator");
                    }
                    else
                    {
                        length += block.End - index;
                        block = block.Next;
                        index = block.Start;
                    }
                }
            }
        }

        public MemoryPoolIterator CopyTo(Encoding encoding, char[] array, int offset, int count, out int actual)
        {
            if (IsDefault)
            {
                actual = 0;
                return this;
            }

            var block = _block;
            var index = _index;
            var remaining = count;
            while (true)
            {
                var following = block.End - index;
                if (remaining <= following)
                {
                    actual = count;
                    if (array != null)
                    {
                        encoding.GetChars(block.Data.Array, index, remaining, array, offset);
                        // Buffer.BlockCopy(block.Data.Array, index, array, offset, remaining);
                    }
                    return new MemoryPoolIterator(block, index + remaining);
                }
                else if (block.Next == null)
                {
                    actual = count - remaining + following;
                    if (array != null)
                    {
                        encoding.GetChars(block.Data.Array, index, following, array, offset);
                        // Buffer.BlockCopy(block.Data.Array, index, array, offset, following);
                    }
                    return new MemoryPoolIterator(block, index + following);
                }
                else
                {
                    if (array != null)
                    {
                        encoding.GetChars(block.Data.Array, index, following, array, offset);
                        // Buffer.BlockCopy(block.Data.Array, index, array, offset, following);
                    }
                    offset += following;
                    remaining -= following;
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public MemoryPoolIterator CopyTo(byte[] array, int offset, int count, out int actual)
        {
            if (IsDefault)
            {
                actual = 0;
                return this;
            }

            var block = _block;
            var index = _index;
            var remaining = count;
            while (true)
            {
                var following = block.End - index;
                if (remaining <= following)
                {
                    actual = count;
                    if (array != null)
                    {
                        Buffer.BlockCopy(block.Data.Array, index, array, offset, remaining);
                    }
                    return new MemoryPoolIterator(block, index + remaining);
                }
                else if (block.Next == null)
                {
                    actual = count - remaining + following;
                    if (array != null)
                    {
                        Buffer.BlockCopy(block.Data.Array, index, array, offset, following);
                    }
                    return new MemoryPoolIterator(block, index + following);
                }
                else
                {
                    if (array != null)
                    {
                        Buffer.BlockCopy(block.Data.Array, index, array, offset, following);
                    }
                    offset += following;
                    remaining -= following;
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public void CopyFrom(byte[] data)
        {
            CopyFrom(data, 0, data.Length);
        }

        public void CopyFrom(ArraySegment<byte> buffer)
        {
            CopyFrom(buffer.Array, buffer.Offset, buffer.Count);
        }

        public void CopyFrom(byte[] data, int offset, int count)
        {
            if (IsDefault)
            {
                return;
            }

            Debug.Assert(_block != null, "Block == null!");
            Debug.Assert(_block.Next == null, "Block.Next != null!");
            Debug.Assert(_block.End == _index, "At the end");

            var pool = _block.Pool;
            var block = _block;
            var blockIndex = _index;

            var bufferIndex = offset;
            var remaining = count;
            var bytesLeftInBlock = block.Data.Offset + block.Data.Count - blockIndex;

            while (remaining > 0)
            {
                if (bytesLeftInBlock == 0)
                {
                    var nextBlock = pool.Lease();
                    block.End = blockIndex;
                    block.Next = nextBlock;
                    block = nextBlock;

                    blockIndex = block.Data.Offset;
                    bytesLeftInBlock = block.Data.Count;
                }

                var bytesToCopy = remaining < bytesLeftInBlock ? remaining : bytesLeftInBlock;

                Buffer.BlockCopy(data, bufferIndex, block.Data.Array, blockIndex, bytesToCopy);

                blockIndex += bytesToCopy;
                bufferIndex += bytesToCopy;
                remaining -= bytesToCopy;
                bytesLeftInBlock -= bytesToCopy;
            }

            block.End = blockIndex;
            _block = block;
            _index = blockIndex;
        }
    }
}