using MSS.Types.MSet;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapCalcSettingsControl.xaml
	/// </summary>
	public partial class MapCalcSettingsControl : UserControl
	{
		private MSetInfoViewModel _vm;

		public MapCalcSettingsControl()
		{
			_vm = (MSetInfoViewModel)DataContext;
			InitializeComponent();
			Loaded += MapCalcSettingsControl_Loaded;
		}

		private void MapCalcSettingsControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapCalcSettingsControl is being loaded.");
				return;
			}
			else
			{
				_vm = (MSetInfoViewModel)DataContext;

				txtTargetIterations.LostFocus += TxtTargetInterations_LostFocus;

				//Debug.WriteLine("The MapCalcSettingsControl is now loaded");
			}
		}

		private void TxtTargetInterations_LostFocus(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(txtTargetIterations.Text, out var newValue))
			{
				_vm.TargetIterations = newValue;
				_vm.TriggerIterationUpdate();
			}
		}

		#region Dependency Properties

		public static readonly DependencyProperty MSetInfoProperty = DependencyProperty.Register(
			"MSetInfo",
			typeof(MSetInfo),
			typeof(MapCalcSettingsControl),
			new FrameworkPropertyMetadata()
			{
				PropertyChangedCallback = OnMSetInfoChanged,
				BindsTwoWayByDefault = true,
				DefaultValue = null
			});

		public MSetInfo MSetInfo
		{
			get => (MSetInfo)GetValue(MSetInfoProperty);
			set => SetValue(MSetInfoProperty, value);
		}

		private static void OnMSetInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = (MSetInfo)e.OldValue;
			var newValue = (MSetInfo)e.NewValue;

			if (oldValue != newValue)
			{
				((MapCalcSettingsControl)d).UpdateOurDataContext(newValue);
			}
		}

		private void UpdateOurDataContext(MSetInfo mSetInfo)
		{
			if (_vm != null)
			{
				_vm.MSetInfo = mSetInfo;
			}
		}

		#endregion
	}
}
