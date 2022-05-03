using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for SingleCoordEditorControl.xaml
	/// </summary>
	public partial class SingleCoordEditorControl : UserControl
	{

		#region Constructor

		public SingleCoordEditorControl()
		{
			Loaded += SingleCoordEditorControl_Loaded;
			InitializeComponent();
		}

		private void SingleCoordEditorControl_Loaded(object sender, RoutedEventArgs e)
		{
		}

		#endregion

		#region Event Handlers

		#endregion

		#region Dependency Properties

		//public static readonly DependencyProperty StringCoordinateProperty = DependencyProperty.Register(
		//	"StringCoordinate",
		//	typeof(string),
		//	typeof(SingleCoordEditorControl),
		//	new FrameworkPropertyMetadata()
		//	{
		//		PropertyChangedCallback = OnStringCoordinateChanged,
		//		BindsTwoWayByDefault = true,
		//		DefaultValue = null
		//	});

		//public string StringCoordinate
		//{
		//	get => (string)GetValue(StringCoordinateProperty);
		//	set => SetValue(StringCoordinateProperty, value);
		//}

		//private static void OnStringCoordinateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		//{
		//	var oldValue = (string)e.OldValue;
		//	var newValue = (string)e.NewValue;

		//	if (oldValue != newValue)
		//	{
		//		if (d is SingleCoordEditorControl scec)
		//		{
		//		}
		//	}
		//}

		//public static readonly DependencyProperty RatValueProperty = DependencyProperty.Register(
		//	"RatValue",
		//	typeof(RValue),
		//	typeof(SingleCoordEditorControl),
		//	new FrameworkPropertyMetadata()
		//	{
		//		PropertyChangedCallback = OnRatValueChanged,
		//		BindsTwoWayByDefault = true,
		//		DefaultValue = RValue.Zero
		//	});

		//public RValue RatValue
		//{
		//	get => (RValue)GetValue(RatValueProperty);
		//	set => SetValue(RatValueProperty, value);
		//}

		//private static void OnRatValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		//{
		//	var oldValue = (RValue)e.OldValue;
		//	var newValue = (RValue)e.NewValue;

		//	if (oldValue != newValue)
		//	{
		//		if (d is SingleCoordEditorControl scec)
		//		{
		//		}
		//	}
		//}

		#endregion

		#region Private Methods

		#endregion

	}
}
