using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Precog.Commands
{
    public class CopyCommands
    {
        private static RoutedUICommand _copyGraph;

        static CopyCommands()
        {
            var inputs = new InputGestureCollection();
            _copyGraph = new RoutedUICommand("CopyGraph", "CopyGraph", typeof(CopyCommands), inputs);
            _copyData = new RoutedUICommand("CopyData", "CopyData", typeof(CopyCommands), inputs);
        }

        public static RoutedCommand CopyGraph
        {
            get { return _copyGraph; }
        }

        private static RoutedUICommand _copyData;

       
        public static RoutedCommand CopyData
        {
            get { return _copyData; }
        }
    }
}
