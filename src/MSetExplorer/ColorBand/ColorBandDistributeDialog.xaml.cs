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
		private int _maxIndex;

		public ColorBandDistributeDialog(int? startIndex, int? endIndex, int maxIndex)
		{
			_startIndex = startIndex;
			_endIndex = endIndex;
			_maxIndex = maxIndex;

			ContentRendered += ColorBandDistributeDialog_ContentRendered;

			InitializeComponent();
		}

		private void ColorBandDistributeDialog_ContentRendered(object? sender, EventArgs e)
		{
			ContentRendered -= ColorBandDistributeDialog_ContentRendered;

			txtStartingIndex.Text = _startIndex.ToString();
			txtEndingIndex.Text = _endIndex.ToString();

			var newTarget = 1 + _endIndex - _startIndex;

			txtNewNumberOfBands.Text = newTarget.ToString();

			if (newTarget > 1)
			{
				txtBlkDivide1Band.Text = $"Divide {newTarget} Bands";
			}
		}

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
					return newEndIndex;
				}
				else
				{
					return null;
				}
			}
		}

		#endregion

		#region Button Handlers

		private void btnOk_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion
	}
}
