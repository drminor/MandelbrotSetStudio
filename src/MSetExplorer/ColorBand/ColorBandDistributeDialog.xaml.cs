using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandDistributeDialog.xaml
	/// </summary>
	public partial class ColorBandDistributeDialog : Window
	{
		private int _startIndex;
		private int _endIndex;
		private int _newColorBandCount;

		private int _maxIndex;
		private int? _initialCBCount;
		private int _maxNewColorBandCount;

		#region Constructor

		public ColorBandDistributeDialog(int startIndex, int endIndex, int maxIndex, int? initialCBCount, int maxNewColorBandCount)
		{
			_startIndex = startIndex;
			_endIndex = endIndex;
			_newColorBandCount = -1;    // No updates yet, make AreChangesPending return true.

			_maxIndex = maxIndex;
			_initialCBCount = initialCBCount;
			_maxNewColorBandCount = maxNewColorBandCount;

			ContentRendered += ColorBandDistributeDialog_ContentRendered;

			InitializeComponent();
		}

		#endregion

		#region Public Events


		public event EventHandler? ApplyChangesRequested;


		#endregion

		#region Event Handlers

		private void ColorBandDistributeDialog_ContentRendered(object? sender, EventArgs e)
		{
			ContentRendered -= ColorBandDistributeDialog_ContentRendered;

			txtStartingIndex.Text = _startIndex.ToString();
			txtEndingIndex.Text = _endIndex.ToString();

			int newCBCount;

			if (_initialCBCount.HasValue)
			{
				newCBCount = _initialCBCount.Value;
			}
			else
			{
				newCBCount = 1 + _endIndex - _startIndex;
			}

			txtNewNumberOfBands.Text = newCBCount.ToString();

			if (newCBCount > 1)
			{
				txtBlkDivide1Band.Text = $"Divide {newCBCount} Bands";
			}
		}

		#endregion

		#region Public Properties

		public int? NewColorBandCount
		{
			get
			{
				if (int.TryParse(txtNewNumberOfBands.Text, out var newNumber))
				{
					return newNumber;
				}
				else
				{
					return null;
				}
			}
		}

		public int? StartIndex
		{
			get
			{ 
				if (int.TryParse(txtStartingIndex.Text, out var newStartIndex))
				{
					return newStartIndex;
				}
				else
				{
					return null;
				}
			}		
		}

		public int? EndIndex
		{
			get
			{
				if (int.TryParse(txtEndingIndex.Text, out var newEndIndex))
				{
					if (newEndIndex <= _maxIndex)
						return newEndIndex;
					else
					{
						return _maxIndex;
					}
				}
				else
				{
					return null;
				}
			}
		}

		#endregion

		#region Button Handlers

		private void btnApplyChanges_Click(object sender, RoutedEventArgs e)
		{
			if (AreChangesPending())
			{
				if (NewColorBandCount > _maxNewColorBandCount)
				{
					Debug.WriteLine($"The new ColorBandCount exceeded the max allowed: {_maxNewColorBandCount}.");
					return;
				}

				if (StartIndex.HasValue && EndIndex.HasValue && NewColorBandCount.HasValue)
				{
					ApplyChangesRequested?.Invoke(this, EventArgs.Empty);

					_startIndex = StartIndex.Value;
					_endIndex = EndIndex.Value;
					_newColorBandCount = NewColorBandCount.Value;
				}
			}
		}

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			if (StartIndex.HasValue && EndIndex.HasValue && NewColorBandCount.HasValue)
			{
				if (NewColorBandCount > _maxNewColorBandCount)
				{
					Debug.WriteLine($"The new ColorBandCount exceeded the max allowed: {_maxNewColorBandCount}.");
					return;
				}

				DialogResult = AreChangesPending();
				Close();
			}
		}

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion

		private bool AreChangesPending()
		{
			var result = StartIndex != _startIndex || EndIndex != _endIndex || NewColorBandCount != _newColorBandCount;

			return result;
		}
	}
}
