using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandUserControl.xaml
	/// </summary>
	public partial class ColorBandUserControl : UserControl
	{
		private ICbsHistogramViewModel _vm;

		private readonly bool _useDetailedDebug = false;

		#region Constructor

		public ColorBandUserControl()
		{
			_vm = (ICbsHistogramViewModel)DataContext;

			Loaded += ColorBandUserControl_Loaded;
			Unloaded += ColorBandUserControl_Unloaded;

			InitializeComponent();
		}

		private void ColorBandUserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the ColorBand UserControl is being loaded.");
				return;
			}
			else
			{
				_vm = (ICbsHistogramViewModel)DataContext;
				_vm.PropertyChanged += ViewModel_PropertyChanged;

				Validation.AddErrorHandler(txtStartCutoff, OnCutoffError);
				Validation.AddErrorHandler(txtEndCutoff, OnCutoffError);

				txtStartCutoff.LostFocus += TxtStartCutoff_LostFocus;
				txtEndCutoff.LostFocus += TxtEndCutoff_LostFocus;

				Debug.WriteLine("The ColorBand UserControl is now loaded");
			}
		}

		private void ColorBandUserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			Loaded -= ColorBandUserControl_Loaded;
			Unloaded -= ColorBandUserControl_Unloaded;

			_vm.PropertyChanged -= ViewModel_PropertyChanged;
			Validation.RemoveErrorHandler(txtStartCutoff, OnCutoffError);
			Validation.RemoveErrorHandler(txtEndCutoff, OnCutoffError);

			txtStartCutoff.LostFocus -= TxtStartCutoff_LostFocus;
			txtEndCutoff.LostFocus -= TxtEndCutoff_LostFocus;
		}

		#endregion

		#region Event Handlers

		private void OnCutoffError(object? sender, ValidationErrorEventArgs e)
		{
			_vm.ColorBandUserControlHasErrors = true;
		}

		private void TxtStartCutoff_LostFocus(object sender, RoutedEventArgs e)
		{
			_vm.ColorBandUserControlHasErrors = HasCutoffError();
		}

		private void TxtEndCutoff_LostFocus(object sender, RoutedEventArgs e)
		{
			_vm.ColorBandUserControlHasErrors = HasCutoffError();
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ICbsHistogramViewModel.CurrentColorBand))
			{
				SetupForm(_vm.CurrentColorBand);
			}
		}

		private void StartColor_CustomMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var cb = _vm?.CurrentColorBand;

			if (cb != null)
			{
				var pos = e.GetPosition(relativeTo: cbcButtonControl1.Canvas);

				var startColor = cb.StartColor;

				if (ShowColorPicker(pos, startColor, out var selectedColor))
				{
					cbcButtonControl1.Color = selectedColor;
				}

				//if (ShowColorSpace(pos, startColor, out var selectedColor2))
				//{
				//	cbcButtonControl1.ColorBandColor = selectedColor2;
				//}
			}
		}

		private void EndColor_CustomMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var cb = _vm?.CurrentColorBand;

			if (cb != null)
			{
				var pos = e.GetPosition(relativeTo: cbcButtonControl2.Canvas);

				var endColor = cb.EndColor;

				if (ShowColorPicker(pos, endColor, out var selectedColor))
				{
					cbcButtonControl2.Color = selectedColor;
				}

				//if (ShowColorSpace(pos, endColor, out var selectedColor2))
				//{
				//	cbcButtonControl1.ColorBandColor = selectedColor2;
				//}
			}
		}

		#endregion

		#region Public Methods

		public bool HasCutoffError()
		{
			var errors1 = Validation.GetErrors(txtEndCutoff);

			var cntr1 = 0;
			foreach (var validationError in errors1)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Validation Error {cntr1++}: {validationError.ErrorContent} for binding: {validationError.BindingInError}. Source: exception: {validationError.Exception} or rule: {validationError.RuleInError}.");
			}

			var errors2 = Validation.GetErrors(txtStartCutoff);

			var cntr2 = 0;
			foreach (var validationError in errors2)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Validation Error {cntr2++}: {validationError.ErrorContent} for binding: {validationError.BindingInError}. Source: exception: {validationError.Exception} or rule: {validationError.RuleInError}.");
			}

			return errors1.Count > 0 || errors2.Count > 0;
		}

		#endregion

		#region Private Methods

		private void SetupForm(ColorBand? colorBand)
		{
			if (colorBand == null)
			{
				return;
			}

			if (colorBand.IsFirst)
			{
				txtStartCutoff.IsEnabled = false;
			}
			else
			{
				txtStartCutoff.IsEnabled = true;
				
				if (colorBand.IsLast)
				{
					txtEndCutoff.IsEnabled = false;
				}
				else
				{
					txtEndCutoff.IsEnabled = true;
				}
			}
		}

		private void SetupBlendStyleComboBox(bool isLast)
		{
			if (isLast && cmbBlendStyle.Items.Count == 3)
			{
				cmbBlendStyle.Items.Clear();
				cmbBlendStyle.Items.Add("None");
				cmbBlendStyle.Items.Add("End");
			}

			if (!isLast && cmbBlendStyle.Items.Count == 2)
			{
				cmbBlendStyle.Items.Clear();
				cmbBlendStyle.Items.Add("None");
				cmbBlendStyle.Items.Add("End");
				cmbBlendStyle.Items.Add("Next");
			}
		}

		private void SetCutoffRangeRule()
		{
			Binding binding = BindingOperations.GetBinding(txtEndCutoff, TextBox.TextProperty);
			binding.ValidationRules.Clear();

			//var x = new CutoffRangeRule();
			//x.Min = 5;
			//x.Max = 500;
			//binding.ValidationRules.Add(x);
		}

		private bool ShowColorPicker(Point pos, ColorBandColor initalColor, out ColorBandColor selectedColor)
		{
			var colorPickerDialalog = new ColorPickerDialog(initalColor);

			var sp = PointToScreen(pos);

			colorPickerDialalog.Left = sp.X - colorPickerDialalog.Width - 225;
			colorPickerDialalog.Top = sp.Y - colorPickerDialalog.Height - 25;

			if (colorPickerDialalog.ShowDialog() == true)
			{
				selectedColor = colorPickerDialalog.SelectedColorBandColor;
				return true;
			}
			else
			{
				selectedColor = initalColor;
				return false;
			}
		}

		private bool ShowColorSpace(Point pos, ColorBandColor initalColor, out ColorBandColor selectedColor)
		{
			var colorSpaceDialalog = new ColorSpaceDialog(initalColor);

			var sp = PointToScreen(pos);

			colorSpaceDialalog.Left = sp.X - colorSpaceDialalog.Width - 225;
			colorSpaceDialalog.Top = sp.Y - colorSpaceDialalog.Height - 25;

			if (colorSpaceDialalog.ShowDialog() == true)
			{
				selectedColor = colorSpaceDialalog.SelectedColorBandColor;
				return true;
			}
			else
			{
				selectedColor = initalColor;
				return false;
			}
		}

		#endregion
	}
}
