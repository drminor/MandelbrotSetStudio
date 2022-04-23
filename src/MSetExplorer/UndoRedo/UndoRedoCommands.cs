using System.Windows.Input;

namespace MSetExplorer
{
	public static class UndoRedoCommands
	{

		public static readonly RoutedUICommand Undo = new RoutedUICommand(
			text: "Undo",
			name: "Undo",
			ownerType: typeof(UndoRedoCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.B,
					ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand Redo  = new RoutedUICommand(
			text: "Redo",
			name: "Redo",
			ownerType: typeof(UndoRedoCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.F,
					ModifierKeys.Alt
				)
			}
		);

	}
}
