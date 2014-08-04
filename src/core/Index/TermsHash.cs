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

using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using ArrayUtil = Lucene.Net.Util.ArrayUtil;

namespace Lucene.Net.Index
{

    /// <summary>This class implements <see cref="InvertedDocConsumer" />, which
    /// is passed each token produced by the analyzer on each
    /// field.  It stores these tokens in a hash table, and
    /// allocates separate byte streams per token.  Consumers of
    /// this class, eg <see cref="FreqProxTermsWriter" /> and <see cref="TermVectorsTermsWriter" />
    ///, write their own byte streams
    /// under each term.
    /// </summary>
    internal sealed class TermsHash : InvertedDocConsumer
    {
        internal readonly TermsHashConsumer consumer;
        internal readonly TermsHash nextTermsHash;

        internal readonly IntBlockPool intPool;
        internal readonly ByteBlockPool bytePool;
        internal ByteBlockPool termBytePool;
        internal readonly Counter bytesUsed;

        internal readonly bool primary;
        internal readonly DocumentsWriterPerThread.DocState docState;

        // Used when comparing postings via termRefComp, in TermsHashPerField
        internal readonly BytesRef tr1 = new BytesRef();
        internal readonly BytesRef tr2 = new BytesRef();

        // Used by perField to obtain terms from the analysis chain
        internal readonly BytesRef termBytesRef = new BytesRef(10);

        internal readonly bool trackAllocations;

        public TermsHash(DocumentsWriterPerThread docWriter, TermsHashConsumer consumer, bool trackAllocations, TermsHash nextTermsHash)
        {
            this.docState = docWriter.docState;
            this.consumer = consumer;
            this.trackAllocations = trackAllocations;
            this.nextTermsHash = nextTermsHash;
            this.bytesUsed = trackAllocations ? docWriter.bytesUsed : Counter.NewCounter();
            intPool = new IntBlockPool(docWriter.intBlockAllocator);
            bytePool = new ByteBlockPool(docWriter.byteBlockAllocator);

            if (nextTermsHash != null)
            {
                // We are primary
                primary = true;
                termBytePool = bytePool;
                nextTermsHash.termBytePool = bytePool;
            }
            else
            {
                primary = false;
            }
        }

        public override void Abort()
        {
            Reset();
            try
            {
                consumer.Abort();
            }
            finally
            {
                if (nextTermsHash != null)
                {
                    nextTermsHash.Abort();
                }
            }
        }

        // Clear all state
        internal void Reset()
        {
            // we don't reuse so we drop everything and don't fill with 0
            intPool.Reset(false, false);
            bytePool.Reset(false, false);
        }

        public override void Flush(IDictionary<string, InvertedDocConsumerPerField> fieldsToFlush, SegmentWriteState state)
        {
            IDictionary<String, TermsHashConsumerPerField> childFields = new HashMap<String, TermsHashConsumerPerField>();
            IDictionary<String, InvertedDocConsumerPerField> nextChildFields;

            if (nextTermsHash != null)
            {
                nextChildFields = new HashMap<String, InvertedDocConsumerPerField>();
            }
            else
            {
                nextChildFields = null;
            }

            foreach (KeyValuePair<String, InvertedDocConsumerPerField> entry in fieldsToFlush)
            {
                TermsHashPerField perField = (TermsHashPerField)entry.Value;
                childFields[entry.Key] = perField.consumer;
                if (nextTermsHash != null)
                {
                    nextChildFields[entry.Key] = perField.nextPerField;
                }
            }

            consumer.Flush(childFields, state);

            if (nextTermsHash != null)
            {
                nextTermsHash.Flush(nextChildFields, state);
            }
        }

        public override InvertedDocConsumerPerField AddField(DocInverterPerField docInverterPerField, FieldInfo fieldInfo)
        {
            return new TermsHashPerField(docInverterPerField, this, nextTermsHash, fieldInfo);
        }

        public override void FinishDocument()
        {
            consumer.FinishDocument(this);
            if (nextTermsHash != null)
            {
                nextTermsHash.consumer.FinishDocument(nextTermsHash);
            }
        }

        public override void StartDocument()
        {
            consumer.StartDocument();
            if (nextTermsHash != null)
            {
                nextTermsHash.consumer.StartDocument();
            }
        }
    }
}