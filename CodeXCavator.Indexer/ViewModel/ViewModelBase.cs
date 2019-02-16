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
using System.ComponentModel;

namespace CodeXCavator.Indexer.ViewModel
{
    /// <summary>
    /// ViewModelBase class.
    /// 
    /// This class is the base class for all view models and provides
    /// a basic implementation of INotifyPropertyChanged.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Notifies of a property change.
        /// </summary>
        /// <param name="properties"></param>
        protected virtual void OnPropertyChanged( params string[] properties )
        {
            if( PropertyChanged != null )
            {
                foreach( var property in properties )
                    PropertyChanged( this, new PropertyChangedEventArgs( property ) );
            }
        }

        /// <summary>
        /// PropertyChanged event.
        /// 
        /// This is raised every time the value of a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
