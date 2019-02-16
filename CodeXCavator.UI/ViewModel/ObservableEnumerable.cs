// Copyright 2014 Christoph Brzozowski
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
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Collections;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// ObservableEnumerable class.
    /// 
    /// This class wraps an enumerable and implements the INotifyCollection changed interface.
    /// It allows to refresh all views bound to the enumerable.
    /// </summary>
    public class ObservableEnumerable : IEnumerable, INotifyCollectionChanged
    {
        /// <summary>
        /// Wrapped enumerable.
        /// </summary>
        protected IEnumerable mEnumerable;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="enumerable">Enumerable to be wrapped.</param>
        public ObservableEnumerable( IEnumerable enumerable = null )
        {
            mEnumerable = enumerable;
        }

        /// <summary>
        /// Wrapped enumerable. 
        /// </summary>
        public IEnumerable Enumerable
        {
            get { return mEnumerable; }
            set
            {
                mEnumerable = value;
                Update();
            }
        }
        
        /// <summary>
        /// Issues a NotifyCollectionChangedAction.Reset event, which causes all views to refresh.
        /// </summary>
        public void Update()
        {
            if( CollectionChanged != null )
                CollectionChanged( this, new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Reset ) );
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Enumerates the contents of the wrapped enumerable.
        /// </summary>
        /// <returns>Enumerator over the contents of the wrapped enumerable.</returns>
        public IEnumerator GetEnumerator()
        {
            if( mEnumerable == null )
                yield break;
            foreach( var element in mEnumerable )
                yield return element;
        }
    }
}
