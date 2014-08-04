/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Lucene.Net.Store
{
    /// <summary>Base implementation class for buffered <see cref="IndexOutput" />. </summary>
    public abstract class BufferedIndexOutput : IndexOutput
    {
        public const int DEFAULT_BUFFER_SIZE = 16384;

        private readonly int bufferSize;
        private readonly byte[] buffer;
        private long bufferStart = 0; // position in file of buffer
        private int bufferPosition = 0; // position in buffer

        private bool isDisposed;

        public BufferedIndexOutput()
            : this(DEFAULT_BUFFER_SIZE)
        {
        }

        public BufferedIndexOutput(int bufferSize)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentException("bufferSize must be greater than 0 (got " + bufferSize + ")");
            }
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];
        }

        /// <summary>Writes a single byte.</summary>
        /// <seealso cref="IndexInput.ReadByte()">
        /// </seealso>
        public override void WriteByte(byte b)
        {
            if (bufferPosition >= bufferSize)
                Flush();
            buffer[bufferPosition++] = b;
        }

        /// <summary>Writes an array of bytes.</summary>
        /// <param name="b">the bytes to write
        /// </param>
        /// <param name="length">the number of bytes to write
        /// </param>
        /// <seealso cref="IndexInput.ReadBytes(byte[],int,int)">
        /// </seealso>
        public override void WriteBytes(byte[] b, int offset, int length)
        {
            int bytesLeft = bufferSize - bufferPosition;
            // is there enough space in the buffer?
            if (bytesLeft >= length)
            {
                // we add the data to the end of the buffer
                Buffer.BlockCopy(b, offset, buffer, bufferPosition, length);
                bufferPosition += length;
                // if the buffer is full, flush it
                if (bufferSize - bufferPosition == 0)
                    Flush();
            }
            else
            {
                // is data larger then buffer?
                if (length > bufferSize)
                {
                    // we flush the buffer
                    if (bufferPosition > 0)
                        Flush();
                    // and write data at once
                    FlushBuffer(b, offset, length);
                    bufferStart += length;
                }
                else
                {
                    // we fill/flush the buffer (until the input is written)
                    int pos = 0; // position in the input data
                    int pieceLength;
                    while (pos < length)
                    {
                        pieceLength = (length - pos < bytesLeft) ? length - pos : bytesLeft;
                        Buffer.BlockCopy(b, pos + offset, buffer, bufferPosition, pieceLength);
                        pos += pieceLength;
                        bufferPosition += pieceLength;
                        // if the buffer is full, flush it
                        bytesLeft = bufferSize - bufferPosition;
                        if (bytesLeft == 0)
                        {
                            Flush();
                            bytesLeft = bufferSize;
                        }
                    }
                }
            }
        }

        /// <summary>Forces any buffered output to be written. </summary>
        public override void Flush()
        {
            FlushBuffer(buffer, bufferPosition);
            bufferStart += bufferPosition;
            bufferPosition = 0;
        }

        /// <summary>Expert: implements buffer write.  Writes bytes at the current position in
        /// the output.
        /// </summary>
        /// <param name="b">the bytes to write
        /// </param>
        /// <param name="len">the number of bytes to write
        /// </param>
        private void FlushBuffer(byte[] b, int len)
        {
            FlushBuffer(b, 0, len);
        }

        /// <summary>Expert: implements buffer write.  Writes bytes at the current position in
        /// the output.
        /// </summary>
        /// <param name="b">the bytes to write
        /// </param>
        /// <param name="offset">the offset in the byte array
        /// </param>
        /// <param name="len">the number of bytes to write
        /// </param>
        public abstract void FlushBuffer(byte[] b, int offset, int len);

        /// <summary>Closes this stream to further operations. </summary>
        protected override void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                Flush();
            }

            isDisposed = true;
        }

        /// <summary>Returns the current position in this file, where the next write will
        /// occur.
        /// </summary>
        /// <seealso cref="Seek(long)">
        /// </seealso>
        public override long FilePointer
        {
            get { return bufferStart + bufferPosition; }
        }

        /// <summary>Sets current position in this file, where the next write will occur.</summary>
        /// <seealso cref="FilePointer">
        /// </seealso>
        public override void Seek(long pos)
        {
            Flush();
            bufferStart = pos;
        }

        /// <summary>The number of bytes in the file. </summary>
        public abstract override long Length { get; }

        public int BufferSize
        {
            get { return bufferSize; }
        }
    }
}