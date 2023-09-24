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

        public static readonly RoutedUICommand Revert
            = new("Revert", "Revert", typeof(ColorBandSetCommands));

        public static readonly RoutedUICommand Insert = new RoutedUICommand(
            text: "Insert",
            name: "Insert",
            ownerType: typeof(ColorBandSetCommands),
            inputGestures: new InputGestureCollection() {
                new KeyGesture(Key.Insert)
            }
        );

        public static readonly RoutedUICommand Delete = new RoutedUICommand(
            text: "Delete",
            name: "Delete",
            ownerType: typeof(ColorBandSetCommands),
            inputGestures: new InputGestureCollection() {
                new KeyGesture(Key.Delete)
            }
        );

        public static readonly RoutedUICommand Apply = new(
            text: "Apply", 
            name: "Apply", 
            ownerType: typeof(ColorBandSetCommands)
       );

        public static readonly RoutedUICommand ShowDetails = new(
            text: "Show Details",
            name: "ShowDetails",
            ownerType: typeof(ColorBandSetCommands),
            inputGestures: new InputGestureCollection() {
                new KeyGesture(Key.F3)
            }
       );

        //public static readonly RoutedUICommand Settings
        //    = new("Settings", "Settings", typeof(ColorBandSetCommands));
    }
}
