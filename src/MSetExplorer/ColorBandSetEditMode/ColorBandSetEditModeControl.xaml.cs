using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandSetEditModeControl.xaml
	/// </summary>
	public partial class ColorBandSetEditModeControl : UserControl
	{
		#region Constructor

		public ColorBandSetEditModeControl()
		{
			ColorBandSetEditMode = ColorBandSetEditMode.Offsets;

			Loaded += ColorBandSetEditModeControl_Loaded;
			InitializeComponent();
		}

		private void ColorBandSetEditModeControl_Loaded(object sender, RoutedEventArgs e)
		{
			rdoBtnOffset.IsChecked = true;
		}

		#endregion

		#region Event Handlers

		private void rdoBtnOffset_Checked(object sender, RoutedEventArgs e)
		{
			ColorBandSetEditMode = ColorBandSetEditMode.Offsets;
		}

		private void rdoBtnColor_Checked(object sender, RoutedEventArgs e)
		{
			ColorBandSetEditMode = ColorBandSetEditMode.Colors;
		}

		private void rdoBtnBands_Checked(object sender, RoutedEventArgs e)
		{
			ColorBandSetEditMode = ColorBandSetEditMode.Bands;
		}

		#endregion

		#region Dependency Properties

		public static readonly DependencyProperty ColorBandSetEditModeProperty = DependencyProperty.Register(
			"ColorBandSetEditMode",
			typeof(ColorBandSetEditMode),
			typeof(ColorBandSetEditModeControl),
			new FrameworkPropertyMetadata()
			{
				PropertyChangedCallback = OnEditModeChanged,
				BindsTwoWayByDefault = true,
				DefaultValue = ColorBandSetEditMode.Offsets
			});

		public ColorBandSetEditMode ColorBandSetEditMode
		{
			get => (ColorBandSetEditMode)GetValue(ColorBandSetEditModeProperty);
			set => SetValue(ColorBandSetEditModeProperty, value);
		}

		private static void OnEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = (ColorBandSetEditMode)e.OldValue;
			var newValue = (ColorBandSetEditMode)e.NewValue;

			if (oldValue != newValue)
			{
				if (d is ColorBandSetEditModeControl uc)
				{
					switch (newValue)
					{
						case ColorBandSetEditMode.Offsets:
							uc.rdoBtnOffset.IsChecked = true;
							break;
						case ColorBandSetEditMode.Colors:
							uc.rdoBtnColor.IsChecked = true;
							break;
						case ColorBandSetEditMode.Bands:
							uc.rdoBtnBands.IsChecked = true;
							break;
						default:
							uc.rdoBtnOffset.IsChecked = true;
							break;
					}
				}
			}
		}

		#endregion


	}

}
