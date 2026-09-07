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

		//private bool _useColorBlendDialog;
		//private bool _useColorSpaceDialog;

		private readonly bool _useDetailedDebug = false;

		#region Constructor

		public ColorBandUserControl()
		{
			_vm = (ICbsHistogramViewModel)DataContext;
			//_useColorBlendDialog = true;
			//_useColorSpaceDialog = false;

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

				if (cb.BlendStyle == ColorBandBlendStyle.None)
				{
					if (ShowColorPicker(pos, startColor, out var selectedColorS3))
					{
						cbcButtonControl1.Color = selectedColorS3;
					}
				}
				else
				{
					//var endColor = cb.EndColor;
					var endColor = cb.ActualEndColor;

					if (ShowColorBlendDialog(pos, startColor, out var selectedColorS1, endColor, out var selectedColorE1, cb.BlendMethod, out var blendMethod))
					{
						cbcButtonControl1.Color = selectedColorS1;
						cbcButtonControl2.Color = selectedColorE1;
						cbcButtonControl1.BlendMethod = blendMethod;

						if (cb.BlendStyle == ColorBandBlendStyle.Next)
						{
							cbcButtonControl2.EffectiveColor = selectedColorE1;
						}
					}
				}

			}
		}

		private void EndColor_CustomMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var cb = _vm?.CurrentColorBand;

			if (cb != null)
			{
				var pos = e.GetPosition(relativeTo: cbcButtonControl2.Canvas);


				if (cb.BlendStyle == ColorBandBlendStyle.None)
				{
					var endColor = cb.EndColor;
					if (ShowColorPicker(pos, endColor, out var selectedColorS3))
					{
						cbcButtonControl1.Color = selectedColorS3;
					}
				}
				else
				{
					var startColor = cb.StartColor;

					//var endColor = cb.EndColor;
					var endColor = cb.ActualEndColor;

					if (ShowColorBlendDialog(pos, startColor, out var selectedColorS1, endColor, out var selectedColorE1, cb.BlendMethod, out var blendMethod))
					{
						cbcButtonControl1.Color = selectedColorS1;
						cbcButtonControl2.Color = selectedColorE1;
						cbcButtonControl1.BlendMethod = blendMethod;

						if (cb.BlendStyle == ColorBandBlendStyle.Next)
						{
							cbcButtonControl2.EffectiveColor = selectedColorE1;
						}
					}
				}
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

			txtStartCutoff.IsEnabled = !colorBand.IsFirst;
			txtEndCutoff.IsEnabled = !colorBand.IsLast;

			//SetupBlendStyleComboBox(colorBand.IsLast);
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

		private bool ShowColorBlendDialog(Point pos, ColorBandColor initalColor1, out ColorBandColor selectedColor1, 
			ColorBandColor initalColor2, out ColorBandColor selectedColor2, 
			ColorBandBlendMethod initialBlendMethod, out ColorBandBlendMethod blendMethod)
		{
			var colorBlendDialog = new ColorBlendDialog(initalColor1, initalColor2, initialBlendMethod);

			var sp = PointToScreen(pos);

			colorBlendDialog.Left = sp.X - colorBlendDialog.Width - 225;
			colorBlendDialog.Top = sp.Y - colorBlendDialog.Height - 25;

			if (colorBlendDialog.ShowDialog() == true)
			{
				selectedColor1 = colorBlendDialog.SelectedColor1;
				selectedColor2 = colorBlendDialog.SelectedColor2;
				blendMethod = colorBlendDialog.BlendMethod;
				return true;
			}
			else
			{
				selectedColor1 = initalColor1;
				selectedColor2 = initalColor2;
				blendMethod = initialBlendMethod;
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
