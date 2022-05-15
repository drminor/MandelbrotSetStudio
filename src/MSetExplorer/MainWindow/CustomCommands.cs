using System.Windows.Input;

namespace MSetExplorer
{
	public static class CustomCommands
	{
		#region Job

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

		public static readonly RoutedUICommand PanLeft = new RoutedUICommand(
			text: "Pan Left",
			name: "PanLeft",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.L,
					ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand PanUp = new RoutedUICommand(
			text: "Pan Up",
			name: "PanUp",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.U,
					ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand PanRight = new RoutedUICommand(
			text: "Pan Right",
			name: "PanRight",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.R,
					ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand PanDown = new RoutedUICommand(
			text: "Pan Down",
			name: "PanDown",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.D,
					ModifierKeys.Alt
				)
			}
		);

		#endregion

		#region Project

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

		public static readonly RoutedUICommand ProjectSaveAs = new RoutedUICommand(
			text: "SaveAs",
			name: "ProjectSaveAs",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.A,
					ModifierKeys.Control
				)
			}
		);

		public static readonly RoutedUICommand ProjectEditCoords = new RoutedUICommand(
			text: "EditCoords",
			name: "ProjectEditCoords",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.F4
				)
			}
		);

		#endregion

		#region Colors

		public static readonly RoutedUICommand ColorsOpen = new RoutedUICommand(
			text: "Open",
			name: "ColorsOpen",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.O,
					ModifierKeys.Control | ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand ColorsSave = new RoutedUICommand(
			text: "Save",
			name: "ColorsSave",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.S,
					ModifierKeys.Control | ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand ColorsSaveAs = new RoutedUICommand(
			text: "SaveAs",
			name: "ColorsSaveAs",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.A,
					ModifierKeys.Control | ModifierKeys.Alt
				)
			}
		);

		#endregion

		#region Application

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

		public static readonly RoutedUICommand ToggleJobTree = new RoutedUICommand(
			text: "ToggleJobTree",
			name: "ToggleJobTree",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.F5
				)
			}
		);

		#endregion
	}
}
