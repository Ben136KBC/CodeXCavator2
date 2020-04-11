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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// Class for wrapping a readonly list of models such that by accesing the list view models get created for them.
    /// </summary>
    /// <typeparam name="MODEL_TYPE">Model type.</typeparam>
    /// <typeparam name="VIEW_MODEL_TYPE">View model type.</typeparam>
    public class ModelToViewModelMappingReadOnlyList<MODEL_TYPE, VIEW_MODEL_TYPE> : IReadOnlyList<VIEW_MODEL_TYPE>, IList 
        where MODEL_TYPE : class 
        where VIEW_MODEL_TYPE : class
    {
        private IReadOnlyList<MODEL_TYPE> mModels;
        private ConditionalWeakTable<MODEL_TYPE, VIEW_MODEL_TYPE> mViewModels;
        private Func<MODEL_TYPE, VIEW_MODEL_TYPE> mModelToViewModel;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="models">Readonly list of models.</param>
        /// <param name="modelToViewModel">Functor for creating a view model from a model.</param>
        public ModelToViewModelMappingReadOnlyList( IReadOnlyList<MODEL_TYPE> models, Func<MODEL_TYPE, VIEW_MODEL_TYPE> modelToViewModel )
        {
            mModels = models;
            mViewModels = new ConditionalWeakTable<MODEL_TYPE, VIEW_MODEL_TYPE>();
            mModelToViewModel = modelToViewModel;
        }

        public VIEW_MODEL_TYPE this[int index]
        {
            get
            {
                var model = mModels[index];
                if (model == null)
                    return null;

                return GetViewModel(model);
            }
        }

        /// <summary>
        /// Returns the corresponding view model for the given model.
        /// </summary>
        /// <param name="model">Model for which a view model should be retrieved.</param>
        /// <returns>View model for given model.</returns>
        private VIEW_MODEL_TYPE GetViewModel(MODEL_TYPE model)
        {
            VIEW_MODEL_TYPE viewModel;
            if (mViewModels.TryGetValue(model, out viewModel))
                return viewModel;

            viewModel = mModelToViewModel(model);
            mViewModels.Add(model, viewModel);
            return viewModel;
        }

        object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }

        public int Count { get { return mModels.Count; } }

        bool IList.IsReadOnly { get { return true; } }

        bool IList.IsFixedSize { get { return true; } }

        int ICollection.Count { get { return this.Count; } }

        object ICollection.SyncRoot { get { return false; } }

        bool ICollection.IsSynchronized { get { return false; } }

        public IEnumerator<VIEW_MODEL_TYPE> GetEnumerator()
        {
            var count = Count;
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
            var viewModel = value as VIEW_MODEL_TYPE;
            if( value == null )
                return -1;
            var count = Count;
            for( int i = 0 ; i < count ; ++i )
                if( this[i] == value )
                    return i;
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
