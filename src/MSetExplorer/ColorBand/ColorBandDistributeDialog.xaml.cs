using System;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandDistributeDialog.xaml
	/// </summary>
	public partial class ColorBandDistributeDialog : Window
	{
		private int? _startIndex;
		private int? _endIndex;

		private int? _newColorBandCount;
		private int _maxIndex;

		#region Constructor

		public ColorBandDistributeDialog(int? startIndex, int? endIndex, int maxIndex)
		{
			_startIndex = startIndex;
			_endIndex = endIndex;
			_maxIndex = maxIndex;


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

			_newColorBandCount = 1 + _endIndex - _startIndex;

			txtNewNumberOfBands.Text = _newColorBandCount.ToString();

			if (_newColorBandCount > 1)
			{
				txtBlkDivide1Band.Text = $"Divide {_newColorBandCount} Bands";
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
				ApplyChangesRequested?.Invoke(this, EventArgs.Empty);

				_startIndex = StartIndex;
				_endIndex = EndIndex;
				_newColorBandCount = NewColorBandCount;
			}
		}

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = AreChangesPending();
			Close();
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
