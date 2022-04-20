using System.Windows.Input;

namespace MSetExplorer
{
	public static class ColorBandSetCommands
    {
        //public static readonly RoutedUICommand Edit
        //    = new("Edit", "Edit", typeof(ColorBandSetCommands));

        //public static readonly RoutedUICommand Cancel
        //    = new("Cancel", "Cancel", typeof(ColorBandSetCommands));

        //public static readonly RoutedUICommand Commit
        //    = new("Commit", "Commit", typeof(ColorBandSetCommands));

        public static readonly RoutedUICommand Delete
            = new("Delete", "Delete", typeof(ColorBandSetCommands));

        public static readonly RoutedUICommand Insert
            = new("Insert", "Insert", typeof(ColorBandSetCommands));

        public static readonly RoutedUICommand Apply
            = new("Apply", "Apply", typeof(ColorBandSetCommands));

        public static readonly RoutedUICommand ShowDetails
            = new("ShowDetails", "ShowDetails", typeof(ColorBandSetCommands));

        //public static readonly RoutedUICommand Settings
        //    = new("Settings", "Settings", typeof(ColorBandSetCommands));
    }
}
