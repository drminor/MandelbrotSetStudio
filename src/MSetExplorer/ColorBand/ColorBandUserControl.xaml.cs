using MSS.Types;
using System.Diagnostics;
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

		private void ColorBandUserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
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

		private void ColorBandUserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
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

		private void TxtStartCutoff_LostFocus(object sender, System.Windows.RoutedEventArgs e)
		{
			_vm.ColorBandUserControlHasErrors = HasCutoffError();
		}

		private void TxtEndCutoff_LostFocus(object sender, System.Windows.RoutedEventArgs e)
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

		#endregion
	}
}
