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
using System.Windows.Input;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// Generic command class.
    /// </summary>
    public class Command : ICommand
    {
        private Func<object, bool> mCanExecute;
        private Action<object> mExecute;

        /// <summary>
        /// Command constructor.
        /// </summary>
        /// <param name="canExecute">Delegate for checking, whether the command can be executed.</param>
        /// <param name="execute">Delegate for executing the command.</param>
        public Command( Func<object, bool> canExecute, Action<object> execute )
        {
            mCanExecute = canExecute;
            mExecute = execute;
        }

        /// <summary>
        /// Checks, whether the command can be executed.
        /// </summary>
        /// <param name="parameter">Command parameter.</param>
        /// <returns>True, if command can be executed, false otherwise.</returns>
        public bool CanExecute( object parameter )
        {
            if( mCanExecute != null )
                return mCanExecute( parameter );
            return false;
        }

        /// <summary>
        /// CanExecute change event.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">Command parameter.</param>
        public void Execute( object parameter )
        {
            if( mExecute != null )
                mExecute( parameter );
        }
    }

}
