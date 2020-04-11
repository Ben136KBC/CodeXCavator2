// Copyright 2020 Christoph Brzozowski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using System.Collections;
using Lucene.Net.Search;

namespace CodeXCavator.Engine
{
    internal partial class LuceneIndex
    {
        /// <summary>
        /// FileList class, which encapuslates access to the documents and their paths in the index.
        /// </summary>
        private class FileList : IReadOnlyList<string>, IList
        {
            
            private LuceneIndex mIndex;
            private int mCount;
            private Lucene.Net.Documents.MapFieldSelector mPathSelector;

            private List<Tuple<int,int>> mIndexToDocIdMapping;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="index">Parent Lucene index.</param>
            internal FileList( LuceneIndex index )
            {

                mIndex = index;
                
                // This gets a little complicated, as the tag documents are also part of the index an can be inbetween the source code documents and
                // therfore they must be skipped...
                // So we build up an index-to-docid mapping first and need to subtract the number of tag documents from the total number of documents
                // to get the actual number of files.
                int numTagDocs = BuildIndexToDocIdMap(mIndex.mIndexReader, out mIndexToDocIdMapping);
                mCount = mIndex.mIndexReader.NumDocs() - numTagDocs;

                mPathSelector = new Lucene.Net.Documents.MapFieldSelector(FIELD_PATH);
            }

            private int BuildIndexToDocIdMap(IndexReader indexReader, out List<Tuple<int,int>> indexToDocIdMapping)
            {
                // The problem is, we want to access the list of files given a consecutive file index number, but there are gaps in the doc-ids caused by tag documents
                //
                // DocId  0 Tag
                // DocId  1 File => Index 0
                // DocId  2 Tag 
                // DocId  3 Tag
                // DocId  4 File => Index 1
                // DocId  5 File => Index 2
                // DocId  6 File => Index 3
                // DocId  7 Tag
                // DocId  8 Tag
                // DocId  9 Tag
                // DocId 10 File => Index 4
                // DocId 11 Tag
                // DocId 12 Tag
                // ............  => Index 5
                // Therfore we construct a list of index to docId tuples, which can be searched using binary search in order to map the index to the corresponding doc id.

                indexToDocIdMapping = new List<Tuple<int,int>>();
                using( var indexSearcher = new IndexSearcher(indexReader) )
                {
                    var tagQuery = new WildcardQuery(new Term(FIELD_TAG, "*"));
                    var tagDocs = indexSearcher.Search(tagQuery, mIndex.mIndexReader.NumDocs()).ScoreDocs;
                    if( tagDocs.Length > 0 )
                    {
                        // Ensure docids are sorted in ascending order, such that they are consecutive too
                        Array.Sort(tagDocs, (x,y) => x.Doc - y.Doc);
                    
                        var docId = tagDocs[0].Doc;
                        int maxGapId = -1;
                        int baseIndex = 0;

                        // Sweep and detect gaps...
                        for( int i = 0 ; i < tagDocs.Length ; ++i )
                        {
                            docId = tagDocs[i].Doc;
                            if( docId - maxGapId > 1 )
                            {
                                // Gap => emit mapping
                                indexToDocIdMapping.Add( new Tuple<int,int>(baseIndex, maxGapId + 1 ) );
                                baseIndex += docId - (maxGapId + 1); 
                                maxGapId = docId;
                            }
                            else
                            {
                                maxGapId = docId;
                            }
                        }

                        // Emit last mapping
                        indexToDocIdMapping.Add( new Tuple<int,int>(baseIndex, maxGapId + 1 ) );
                    }
                    return tagDocs.Length;
                }
            }

            public string this[int index]
            {
                get
                {
                    if (index < 0 || index >= mCount)
                        return null;

                    var docId = MapIndexToDocId( mIndexToDocIdMapping, index );
                    if( docId >= 0 )
                        return GetFilePath(mIndex.mIndexReader, docId);
                    return null;
                }
            }

            private class IndexToDocIdComparer : IComparer<Tuple<int, int>>
            {
                public int Compare(Tuple<int, int> x, Tuple<int, int> y) { return x.Item1 - y.Item1; }
                public static readonly IComparer<Tuple<int,int>> Default = new IndexToDocIdComparer();
            }

            private static int MapIndexToDocId(List<Tuple<int, int>> indexToDocIdMapping, int index)
            {
                if( indexToDocIdMapping == null || indexToDocIdMapping.Count == 0 )
                    return index;
                
                var mapEntryIndex = indexToDocIdMapping.BinarySearch(new Tuple<int,int>(index, 0), IndexToDocIdComparer.Default );
                if( mapEntryIndex >= 0 )
                {
                    // Direct match => can take doc id directly
                    return indexToDocIdMapping[mapEntryIndex].Item2;
                }
                else
                {
                    // No direct match => need to compute doc id 
                    mapEntryIndex = ~mapEntryIndex - 1;
                    if( mapEntryIndex >= 0 )
                    {
                        var indexWithDocId = indexToDocIdMapping[mapEntryIndex];
                        return index - indexWithDocId.Item1 + indexWithDocId.Item2;
                    }
                }
                return -1;
            }

            private string GetFilePath(IndexReader indexReader, int docId)
            {
                if (!indexReader.IsDeleted(docId))
                {
                    var document = indexReader.Document(docId, mPathSelector);
                    var pathField = document.GetField(FIELD_PATH);
                    if( pathField != null )
                    {
                        var file = pathField.StringValue;
                        if( file != null )
                            return string.Intern(file);
                    }
                    return null;
                }
                else
                {
                    return "<deleted>";
                }
            }

            object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }

            public int Count { get { return mCount; } }

            bool IList.IsReadOnly { get { return true; } }

            bool IList.IsFixedSize { get { return true; } }

            int ICollection.Count { get { return this.Count; } }

            object ICollection.SyncRoot { get { return false; } }

            bool ICollection.IsSynchronized { get { return false; } }

            public IEnumerator<string> GetEnumerator()
            {
                IndexReader indexReader = mIndex.mIndexReader;
                int count = Count;
                for( int i = 0 ; i < count ; ++i )
                    yield return this[i];
            }

            int IList.Add(object value)
            {
                throw new NotSupportedException();
            }

            void IList.Clear()
            {
                throw new NotSupportedException();
            }

            bool IList.Contains(object value)
            {
                return ((IList) this).IndexOf(value) >= 0;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if( array == null )
                    throw new ArgumentNullException();
                if( index < 0 || index >= array.Length )
                    throw new ArgumentOutOfRangeException(nameof(index));
                if( array.Length - index < Count )
                    throw new ArgumentException(string.Empty, nameof(index));
                
                for( int i = 0 ; i < Count ; ++i )
                    array.SetValue(this[index], i);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            int IList.IndexOf(object value)
            {
                var stringValue = value as string;
                if( stringValue != null )
                {
                    for( int i = 0 ; i < Count ; ++i )
                        if( this[i] == stringValue )
                            return i;
                }
                return -1;
            }

            void IList.Insert(int index, object value)
            {
                throw new NotSupportedException();
            }

            void IList.Remove(object value)
            {
                throw new NotSupportedException();
            }

            void IList.RemoveAt(int index)
            {
                throw new NotSupportedException();
            }
        }
    }


}
