using System.Windows.Input;

namespace MSetExplorer
{
	public static class JobTreeCommands
    {

        public static readonly RoutedUICommand MoveTo = new RoutedUICommand(
            text: "Move To",
            name: "MoveTo",
            ownerType: typeof(JobTreeCommands),
            inputGestures: new InputGestureCollection() {
                new KeyGesture(Key.Enter)
            }
        );

        //public static readonly RoutedUICommand RestoreBranch = new RoutedUICommand(
        //    text: "Restore Branch",
        //    name: "RestoreBranch",
        //    ownerType: typeof(JobTreeCommands),
        //    inputGestures: new InputGestureCollection() {
        //        new KeyGesture(Key.Insert & Key.LeftShift | Key.Insert & Key.RightShift)
        //    }
        //);

        public static readonly RoutedUICommand RestoreBranch = new RoutedUICommand(
            text: "Restore Branch",
            name: "RestoreBranch",
            ownerType: typeof(JobTreeCommands),
            inputGestures: new InputGestureCollection() {
                new KeyGesture(Key.Insert)
            }
        );

        public static readonly RoutedUICommand Delete = new RoutedUICommand(
            text: "Delete",
            name: "Delete",
            ownerType: typeof(JobTreeCommands),
            inputGestures: new InputGestureCollection() {
                new KeyGesture(Key.Delete)
            }
        );

        public static readonly RoutedUICommand ShowDetails = new(
            text: "Show Details",
            name: "ShowDetails",
            ownerType: typeof(JobTreeCommands),
            inputGestures: new InputGestureCollection() {
                new KeyGesture(Key.F3)
            }
       );

    }
}
