using System.Windows.Input;

namespace MSetExplorer
{
	public static class CustomCommands
	{

		public static readonly RoutedUICommand Exit = new RoutedUICommand(
			text: "Exit",
			name: "Exit",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.F4, 
					ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand JobGoBack = new RoutedUICommand(
			text: "Go Back",
			name: "JobGoBack",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.B,
					ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand JobGoForward = new RoutedUICommand(
			text: "Go Forward",
			name: "JobGoForward",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.F,
					ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand ProjectSave = new RoutedUICommand(
			text: "Save",
			name: "ProjectSave",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.S,
					ModifierKeys.Control
				)
			}
		);

	}
}
