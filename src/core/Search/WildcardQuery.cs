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

using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    /// <summary>Implements the wildcard search query. Supported wildcards are <c>*</c>, which
    /// matches any character sequence (including the empty one), and <c>?</c>,
    /// which matches any single character. Note this query can be slow, as it
    /// needs to iterate over many terms. In order to prevent extremely slow WildcardQueries,
    /// a Wildcard term should not start with one of the wildcards <c>*</c> or
    /// <c>?</c>.
    /// 
    /// <p/>This query uses the <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT" />
    ///
    /// rewrite method.
    /// 
    /// </summary>
    /// <seealso cref="WildcardTermEnum">
    /// </seealso>
    [Serializable]
    public class WildcardQuery : AutomatonQuery
    {
        public const char WILDCARD_STRING = '*';

        public const char WILDCARD_CHAR = '?';

        public const char WILDCARD_ESCAPE = '\\';

        public WildcardQuery(Term term)
            : base(term, ToAutomaton(term))
        {
        }

        public static Automaton ToAutomaton(Term wildcardquery)
        {
            var automata = new List<Automaton>();

            var wildcardText = wildcardquery.Text;

            for (var i = 0; i < wildcardText.Length; )
            {
                int c = wildcardText[i];
                var length = 1; // .NET Port: chars are always length 1 in .NET
                switch (c)
                {
                    case WILDCARD_STRING:
                        automata.Add(BasicAutomata.MakeAnyString());
                        break;
                    case WILDCARD_CHAR:
                        automata.Add(BasicAutomata.MakeAnyChar());
                        break;
                    case WILDCARD_ESCAPE:
                        // add the next codepoint instead, if it exists
                        if (i + length < wildcardText.Length)
                        {
                            int nextChar = wildcardText[i + length];
                            length += 1; // .NET port: chars are always length 1 in .NET
                            automata.Add(BasicAutomata.MakeChar(nextChar));
                            break;
                        } // else fallthru, lenient parsing with a trailing \
                        automata.Add(BasicAutomata.MakeChar(c));
                        break;
                    default:
                        automata.Add(BasicAutomata.MakeChar(c));
                        break;
                }
                i += length;
            }

            return BasicOperations.Concatenate(automata);
        }

        /// <summary> Returns the pattern term.</summary>
        public Term Term
        {
            get { return term; }
        }

        /// <summary>Prints a user-readable version of this query. </summary>
        public override String ToString(String field)
        {
            var buffer = new StringBuilder();
            if (!Field.Equals(field))
            {
                buffer.Append(Field);
                buffer.Append(":");
            }
            buffer.Append(term.Text);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }
    }
}