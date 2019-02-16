using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace CodeXCavator.UI.ViewModel
{
    public static class FindCommands
    {
        public static readonly RoutedUICommand FindInNewTab = new RoutedUICommand( "Find in new tab", "FindInNewTab", typeof( FindCommands ) );
        public static readonly RoutedUICommand FindInFile = new RoutedUICommand( "Find in file", "FindInFile", typeof( FindCommands ) );

    }
}
