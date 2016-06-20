using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Precog.Commands
{
    public class MergeCommands
    {
        private static RoutedUICommand _merge;

        static MergeCommands()
        {
            var inputs = new InputGestureCollection();
            _merge = new RoutedUICommand("Merge", "Merge", typeof(MergeCommands), inputs);
        }

        public static RoutedCommand Merge
        {
            get { return _merge; }
        }
    }
}
