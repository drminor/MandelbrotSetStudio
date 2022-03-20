using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandViewModel : ViewModelBase, IColorBandViewModel
	{
		private readonly ObservableCollection<MapSection> _mapSections;
		private readonly SynchronizationContext _synchronizationContext;
		private readonly MapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private double _rowHeight;
		private double _itemWidth;

		private Project _currentProject;
		private ColorBandSetW _colorBandSet;
		private ColorBandW _selectedColorBand;

		#region Constructor

		public ColorBandViewModel(ObservableCollection<MapSection> mapSections)
		{
			_mapSections = mapSections;
			_synchronizationContext = SynchronizationContext.Current;
			Histogram = new HistogramA(0);
			_mapSectionHistogramProcessor = new MapSectionHistogramProcessor(Histogram);

			_rowHeight = 60;
			_itemWidth = 180;
			CurrentProject = null;
			_colorBandSet = null;
			ColorBands = new ObservableCollection<ColorBandW>();
			SelectedColorBand = null;

			_mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public double RowHeight
		{
			get => _rowHeight;
			set { _rowHeight = value; OnPropertyChanged(nameof(IColorBandViewModel.RowHeight)); }
		}

		public double ItemWidth
		{
			get => _itemWidth;
			set { _itemWidth = value; OnPropertyChanged(nameof(IColorBandViewModel.ItemWidth)); }
		}

		public ObservableCollection<ColorBandW> ColorBands { get; private set; }

		public Project CurrentProject
		{
			get => _currentProject;
			set
			{
				if (value != _currentProject)
				{
					_currentProject = value;
					ColorBandSet = value.CurrentColorBandSet as ColorBandSetW;
					OnPropertyChanged(nameof(IColorBandViewModel.CurrentProject));
				}
			}
		}

		public ColorBandW SelectedColorBand
		{
			get => _selectedColorBand;

			set
			{
				_selectedColorBand = value;
				OnPropertyChanged(nameof(IColorBandViewModel.SelectedColorBand));
			}
		}

		public ColorBandSetW ColorBandSet
		{
			get => _colorBandSet;

			private set
			{
				Debug.WriteLine($"ColorBandViewModel is having is ColorBandSet updated. Current = {_colorBandSet?.SerialNumber}, New = {value?.SerialNumber}");
				if (value == null)
				{
					if (_colorBandSet != null)
					{
						ColorBands.Clear();
						_colorBandSet = value;
						Histogram.Reset();
						Debug.WriteLine("ColorBandViewModel is clearing its collection. (non-null => null.)");
						OnPropertyChanged(nameof(IColorBandViewModel.ColorBandSet));
					}
				}
				else
				{
					if (_colorBandSet == null || _colorBandSet != value)
					{
						ColorBands.Clear();
						Histogram.Reset(value.HighCutOff + 1);
						PopulateHistorgram(_mapSections, Histogram);

						foreach (var c in value)
						{
							ColorBands.Add(c as ColorBandW);
						}

						var view = CollectionViewSource.GetDefaultView(ColorBands);
						_ = view.MoveCurrentTo(ColorBands.FirstOrDefault());


						if (_colorBandSet == null)
						{
							Debug.WriteLine("ColorBandViewModel is updating its collection. (null => non-null.)");
						}
						else
						{
							Debug.WriteLine("ColorBandViewModel is updating its collection. (non-null => non-null.)");
						}
						_colorBandSet = value;

						OnPropertyChanged(nameof(IColorBandViewModel.ColorBandSet));
					}
				}
			}
		}

		public int? HighCutOff
		{
			get => _colorBandSet?.HighCutOff;
			set
			{
				if (value.HasValue)
				{
					if (_colorBandSet != null)
					{
						_colorBandSet.HighCutOff = value.Value;
						OnPropertyChanged(nameof(IColorBandViewModel.HighCutOff));
					}
				}
			}
		}

		public IHistogram Histogram { get; private set; }

		#endregion

		#region Event Handlers

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (ColorBands.Count == 0)
			{
				return;
			}

			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				Histogram.Reset();
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				// Add items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					_mapSectionHistogramProcessor.AddWork(isAddOperation: true, mapSection, HandleHistogramUpdate);
				}
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					_mapSectionHistogramProcessor.AddWork(isAddOperation: false, mapSection, HandleHistogramUpdate);
				}
			}

			//Debug.WriteLine($"There are {Histogram[Histogram.UpperBound - 1]} points that reached the target iterations.");
		}

		private void HistogramChanged(object state)
		{
			double t = 0;
			foreach(var cb in ColorBands)
			{
				cb.Percentage = Math.Round(t, 4);
				t += 3.9;
			}
		}

		#endregion

		#region Public Methods

		public void Test1()
		{
			//var newColorBandSet = ColorBandSet.CreateNewCopy();
			//var len = newColorBandSet.Count;

			//var ocb = newColorBandSet[len - 3];
			//var ocb1 = newColorBandSet[1];
			//var ncb = new ColorBand(ocb.CutOff + 50, ocb1.StartColor, ocb1.BlendStyle, ocb1.EndColor);

			//newColorBandSet.Insert(len - 2, ncb);

			//ColorBandSet = newColorBandSet;

			Debug.WriteLine($"There are {Histogram[Histogram.UpperBound]} points that reached the target iterations.");

		}

		public void Test2()
		{
			var newColorBandSet = new ColorBandSetW();
			ColorBandSet = newColorBandSet;
		}

		public void Test3()
		{
			var newColorBandSet = new ColorBandSetW();

			newColorBandSet.Insert(0, new ColorBandW(100, new ColorBandColor("#FF0000"), ColorBandBlendStyle.Next, new ColorBandColor("#00FF00")));

			ColorBandSet = newColorBandSet;
		}

		public void Test4()
		{
			var newColorBandSet = new ColorBandSetW();

			newColorBandSet.Insert(0, new ColorBandW(100, new ColorBandColor("#FF0000"), ColorBandBlendStyle.Next, new ColorBandColor("#000000")));
			newColorBandSet.Insert(0, new ColorBandW(50, new ColorBandColor("#880000"), ColorBandBlendStyle.Next, new ColorBandColor("#000000")));

			ColorBandSet = newColorBandSet;

		}

		#endregion

		#region Private Methods

		private void PopulateHistorgram(IEnumerable<MapSection> mapSections, IHistogram histogram)
		{
			foreach(var ms in mapSections)
			{
				histogram.Add(ms.Histogram);
			}
		}

		private void HandleHistogramUpdate(MapSection mapSection, IList<double> newPercentages)
		{
			_synchronizationContext.Post(o => HistogramChanged(o), null);
		}

		#endregion
	}
}
