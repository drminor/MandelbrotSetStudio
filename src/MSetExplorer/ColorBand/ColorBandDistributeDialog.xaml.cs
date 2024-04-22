using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandDistributeDialog.xaml
	/// </summary>
	public partial class ColorBandDistributeDialog : Window
	{
		private int _startIndex;
		private int _endIndex;
		private int _maxIndex;

		public ColorBandDistributeDialog(int startIndex, int endIndex, int maxIndex)
		{
			_startIndex = startIndex;
			_endIndex = endIndex;
			_maxIndex = maxIndex;

			InitializeComponent();
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
				if (int.TryParse(startingIndex.Text, out var newStartIndex))
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
				if (int.TryParse(endingIndex.Text, out var newEndIndex))
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
