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


        public static readonly RoutedUICommand Revert = new(
            text: "Revert", 
            name: "Revert",
            ownerType: typeof(ColorBandSetCommands)
        );

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



    /*
        Supported Operations / Supported States.

        What is selected        Reorder Move    Delete  Insert

        1. Single Band          Y       Y       Y       Y          
        2. Range of Bands       Y       Y       Y       N
        3. Set of Bands         N       Y       Y       N

        4. Single Color         Y       N       Y       Y
        5. Range of Colors      Y       N       Y       N
        6. Set of Colors        N       N       Y       N

        7. Single Offset        N       Y       Y       Y
        8. Range of Offsets     N       Y       Y       N
        9. Set of Offsets       N       Y       Y       N

        10. A set of Colors     N       Y       Y       N 
            + a set of Offsets
            (disjoint to each
            other)
              
        NOTES:
            Reordering a Band or Range of Bands is the same as reordering a Color or range of Colors -- simply ignore the selected Offsets
            Moving an offset can be thought of as changing the width of the corresponding band and changing the starting position of the following band.
            The Delete operation simply deletes all selected items.

            The Insert operation, inserts a new Color and Offset, just a new Color or just a new Offset depending on what is selected.
            The Insert operation, updates the collection of selected items to only include the Color and Offset for the current position.



            



            
        NOTE: A range or set of Bands is the same as a range or set of Colors and a range or set of Offset where for each color included the corresponding Offset is also included.

    */

}
