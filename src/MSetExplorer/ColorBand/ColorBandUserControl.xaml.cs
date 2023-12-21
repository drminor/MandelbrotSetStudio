using System.Diagnostics;
using System.Windows.Controls;

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

				Validation.AddErrorHandler(txtCutoff, OnCutoffError);

				txtCutoff.LostFocus += TxtCutoff_LostFocus;

				Debug.WriteLine("The ColorBand UserControl is now loaded");
			}
		}

		private void ColorBandUserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			Validation.RemoveErrorHandler(txtCutoff, OnCutoffError);
			txtCutoff.LostFocus -= TxtCutoff_LostFocus;
		}

		#endregion

		#region Event Handlers

		private void OnCutoffError(object? sender, ValidationErrorEventArgs e)
		{
			_vm.ColorBandUserControlHasErrors = true;
		}

		private void TxtCutoff_LostFocus(object sender, System.Windows.RoutedEventArgs e)
		{
			_vm.ColorBandUserControlHasErrors = HasCutoffError();
		}

		#endregion

		#region Public Methods

		public bool HasCutoffError()
		{
			var errors = Validation.GetErrors(txtCutoff);

			var cntr = 0;
			foreach (var validationError in errors)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Validation Error {cntr++}: {validationError.ErrorContent} for binding: {validationError.BindingInError}. Source: exception: {validationError.Exception} or rule: {validationError.RuleInError}.");
			}

			return errors.Count > 0;
		}

		#endregion

		//#region IDisposable Support

		//private bool _disposedValue;

		//protected virtual void Dispose(bool disposing)
		//{
		//	if (!_disposedValue)
		//	{
		//		if (disposing)
		//		{
		//			Validation.RemoveErrorHandler(txtCutoff, OnCutoffError);
		//		}

		//		_disposedValue = true;
		//	}
		//}

		//public void Dispose()
		//{
		//	Dispose(disposing: true);
		//	GC.SuppressFinalize(this);
		//}

		//#endregion
	}
}
