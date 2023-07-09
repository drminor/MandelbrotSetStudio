using MSS.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandSetPlotControl.xaml
	/// </summary>
	public partial class ColorBandSetPlotControl : UserControl
	{
		#region Private Fields

		//private bool DRAW_OUTLINE = false;
		//private Rectangle _outline;
		private ICbsHistogramViewModel _vm;

		private readonly bool _useDetailedDebug;


		#endregion

		#region Constructor

		public ColorBandSetPlotControl()
		{
			_useDetailedDebug = true;

			_vm = (CbsHistogramViewModel)DataContext;

			Loaded += ColorBandSetPlotControl_Loaded;
			SizeChanged += ColorBandSetPlotControl_SizeChanged;

			InitializeComponent();

			if (_useDetailedDebug)
			{
				Debug.WriteLine($"Hi.");
			}
		}

		private void ColorBandSetPlotControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (_vm != null)
			{
				var cntrlSize = new SizeDbl(ActualWidth, ActualHeight);
				Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetPlotControl_SizeChanged. Control: {cntrlSize}, Canvas:{_vm.CanvasSize}, ViewPort: {_vm.ViewportSize}, Unscaled: {_vm.UnscaledExtent}.");
			}
		}

		private void ColorBandSetPlotControl_Loaded(object sender, RoutedEventArgs e)
		{


			Debug.WriteLine("The ColorBandSetPlot UserControl is now loaded.");

		}

		#endregion

		#region Button Handlers

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			DisplayPlot();
		}

		#endregion

		#region Private Methods

		private void DisplayPlot()
		{
			//double[] dataX = new double[] { 1, 2, 3, 4, 5 };
			//double[] dataY = new double[] { 1, 4, 9, 16, 25 };

			var (dataX, dataY) = GetPlotData1();

			WpfPlot1.Plot.AddScatter(dataX, dataY);

			WpfPlot1.Plot.Title("Hi There");
			WpfPlot1.Plot.XLabel("XLabel");
			WpfPlot1.Plot.YLabel("YLabel");

			WpfPlot1.Refresh();
		}

		private (double[] dataX, double[] dataY) GetPlotData1()
		{
			//ClearHistogramItems();

			var colorBandSet = _vm.ColorBandSet;
			var startPtr = _vm.StartPtr;
			var endPtr = _vm.EndPtr;

			var startingIndex = colorBandSet[startPtr].StartingCutoff;
			var endingIndex = colorBandSet[endPtr].Cutoff;
			//var highCutoff = colorBandSet.HighCutoff;

			var hEntries = _vm.GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

			if (hEntries.Length < 1)
			{
				Debug.WriteLine($"WARNING: The Histogram is empty.");
				return (new double[0], new double[0]);
			}

			var dataX = new double[hEntries.Length];
			var dataY = new double[hEntries.Length];

			for (var hPtr = 0; hPtr < hEntries.Length; hPtr++)
			{
				var hEntry = hEntries[hPtr];
				dataX[hPtr] = hEntry.Key;
				dataY[hPtr] = hEntry.Value;
			}

			return (dataX, dataY);
		}

		private (double[] dataX, double[] dataY) GetPlotData2()
		{
			//ClearHistogramItems();

			var colorBandSet = _vm.ColorBandSet;
			var startPtr = _vm.StartPtr;
			var endPtr = _vm.EndPtr;

			var startingIndex = colorBandSet[startPtr].StartingCutoff;
			var endingIndex = colorBandSet[endPtr].Cutoff;
			//var highCutoff = colorBandSet.HighCutoff;

			//var rn = 1 + endingIndex - startingIndex;
			//if (Math.Abs(LogicalDisplaySize.Width - rn) > 20)
			//{
			//	Debug.WriteLineIf(_useDetailedDebug, $"The range of indexes does not match the Logical Display Width. Range: {endingIndex - startingIndex}, Width: {LogicalDisplaySize.Width}.");
			//	return;
			//}

			//LogicalDisplaySize = new SizeInt(rn + 10, _canvasSize.Height);

			//var w = (int)Math.Round(UnscaledExtent.Width);

			//DrawHistogramBorder(w, _histDispHeight);

			var hEntries = _vm.GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

			if (hEntries.Length < 1)
			{
				Debug.WriteLine($"WARNING: The Histogram is empty.");
				return (new double[0], new double[0]);
			}

			//var maxV = hEntries.Max(x => x.Value) + 5; // Add 5 to reduce the height of each line.
			//var vScaleFactor = _histDispHeight / (double)maxV;

			//var geometryGroup = new GeometryGroup();

			var dataX = new double[hEntries.Length];
			var dataY = new double[hEntries.Length];

			for (var hPtr = 0; hPtr < hEntries.Length; hPtr++)
			{
				var hEntry = hEntries[hPtr];

				//var x = 1 + hEntry.Key - startingIndex;
				//var height = hEntry.Value * vScaleFactor;
				//geometryGroup.Children.Add(BuildHLine(x, height));

				dataX[hPtr] = hEntry.Key;
				dataY[hPtr] = hEntry.Value;
			}

			//var hTestEntry = hEntries[^1];

			return (dataX, dataY);

		}

		private IEnumerable<KeyValuePair<int, int>> GetPlotData()
		{
			var colorBandSet = _vm.ColorBandSet;
			var startPtr = _vm.StartPtr;
			var endPtr = _vm.EndPtr;

			var startingIndex = colorBandSet[startPtr].StartingCutoff;
			var endingIndex = colorBandSet[endPtr].Cutoff;
			//var highCutoff = colorBandSet.HighCutoff;

			var result = _vm.GetKeyValuePairsForBand(startingIndex, endingIndex);

			return result;
		}

		#endregion
	}
}
