using System.Windows.Input;

namespace MSetExplorer
{
	public static class CustomCommands
	{
		#region Map

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

		public static readonly RoutedUICommand ZoomOut12 = new RoutedUICommand(
			text: "Zoom Out 12%",
			name: "ZoomOut12",
			ownerType: typeof(CustomCommands)
		);

		public static readonly RoutedUICommand ZoomOut25 = new RoutedUICommand(
			text: "Zoom Out 25%",
			name: "ZoomOut24",
			ownerType: typeof(CustomCommands)
		);

		public static readonly RoutedUICommand ZoomOut50 = new RoutedUICommand(
			text: "Zoom Out 50%",
			name: "ZoomOut50",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.Z,
					ModifierKeys.Alt, "Alt-Z"
				)
			}
		);

		public static readonly RoutedUICommand ZoomOut100 = new RoutedUICommand(
			text: "Zoom Out 100%",
			name: "ZoomOut100",
			ownerType: typeof(CustomCommands)
		);

		public static readonly RoutedUICommand ZoomOutCustom = new RoutedUICommand(
			text: "ZoomOut Custom",
			name: "ZoomOutCustom",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.Z,
					ModifierKeys.Control | ModifierKeys.Alt, "Ctrl-Alt-Z"
				)
			}
		);

		public static readonly RoutedUICommand EditCoords = new RoutedUICommand(
			text: "EditCoords",
			name: "EditCoords",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.F4
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

		#endregion

		#region Poster

		public static readonly RoutedUICommand PosterSave = new RoutedUICommand(
			text: "Save",
			name: "PosterSave",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.S,
					ModifierKeys.Control
				)
			}
		);

		public static readonly RoutedUICommand PosterSaveAs = new RoutedUICommand(
			text: "SaveAs",
			name: "PosterSaveAs",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.A,
					ModifierKeys.Control
				)
			}
		);

		public static readonly RoutedUICommand CreateImage = new RoutedUICommand(
			text: "Create Image",
			name: "CreateImage",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.P,
					ModifierKeys.Control
				)
			}
		);

		public static readonly RoutedUICommand PosterEditSize = new RoutedUICommand(
			text: "Edit Size",
			name: "PosterEditSize",
			ownerType: typeof(CustomCommands)
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

		public static readonly RoutedUICommand ImportColors = new RoutedUICommand(
			text: "Import",
			name: "ImportColors",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.I,
					ModifierKeys.Control | ModifierKeys.Alt
				)
			}
		);

		public static readonly RoutedUICommand ExportColors = new RoutedUICommand(
			text: "Export",
			name: "ExportColors",
			ownerType: typeof(CustomCommands),
			inputGestures: new InputGestureCollection() {
				new KeyGesture(
					Key.E,
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
