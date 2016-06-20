using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Precog.Commands
{
    public class SortCommands
    {
        private static RoutedUICommand _sortListView;

        static SortCommands()
        {
            var inputs = new InputGestureCollection();
            _sortListView = new RoutedUICommand("SortListView", "SortListView", typeof(SortCommands),inputs);
        }

        public static RoutedCommand SortListView
        {
            get { return _sortListView; }
        }
    }
}
