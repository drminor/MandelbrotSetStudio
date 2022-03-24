using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Collections.Specialized;

using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandSetViewModel : IColorBandSetViewModel
	{
		private readonly ObservableCollection<MapSection> _mapSections;
		private readonly SynchronizationContext _synchronizationContext;
		private readonly MapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private double _rowHeight;
		private double _itemWidth;

		private Project _currentProject;
		private ColorBandSet _colorBandSet;
		//private ColorBand _selectedColorBand;

		#region Constructor

		public ColorBandSetViewModel(ObservableCollection<MapSection> mapSections)
		{
			_mapSections = mapSections;
			_synchronizationContext = SynchronizationContext.Current;
			Histogram = new HistogramA(0);
			_mapSectionHistogramProcessor = new MapSectionHistogramProcessor(Histogram);

			_rowHeight = 60;
			_itemWidth = 180;
			CurrentProject = null;
			_colorBandSet = null;
			//ColorBands = new ObservableCollection<ColorBand>();
			//SelectedColorBand = null;

			_mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public double RowHeight
		{
			get => _rowHeight;
			set { _rowHeight = value; OnPropertyChanged(nameof(IColorBandSetViewModel.RowHeight)); }
		}

		public double ItemWidth
		{
			get => _itemWidth;
			set { _itemWidth = value; OnPropertyChanged(nameof(IColorBandSetViewModel.ItemWidth)); }
		}

		//public ObservableCollection<ColorBand> ColorBands
		//{
		//	get => _colorBandSet != null ? _colorBandSet.ColorBands : new ObservableCollection<ColorBand>();
		//	set
		//	{

		//	}
		//}

		public Project CurrentProject
		{
			get => _currentProject;
			set
			{
				if (value != _currentProject)
				{
					_currentProject = value;

					// Clone this to keep changes made here from updating the Project's copy.
					ColorBandSet = value.CurrentColorBandSet.Clone();
					OnPropertyChanged(nameof(CurrentProject));
				}
			}
		}

		//public ColorBand SelectedColorBand
		//{
		//	get
		//	{
		//		if (_colorBandSet == null)
		//		{
		//			return null;
		//		}

		//		var ci = CollectionViewSource.GetDefaultView(ColorBandSet)?.CurrentItem;

		//		if (ci == null)
		//		{
		//			return null;
		//		}
		//		else
		//		{
		//			return (ColorBand)ci;
		//		}
		//	}

		//	set
		//	{
		//		_selectedColorBand = value;
		//		OnPropertyChanged(nameof(SelectedColorBand));
		//	}
		//}

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;

			//private set
			//{
			//	if (value != _colorBandSet)
			//	{
			//		_colorBandSet = value;
			//		OnPropertyChanged(nameof(IColorBandSetViewModel.ColorBandSet));
			//	}
			//}

			//private set
			//{
			//	Debug.WriteLine($"ColorBandViewModel is having is ColorBandSet updated. Current = {_colorBandSet?.SerialNumber}, New = {value?.SerialNumber}");
			//	if (value == null)
			//	{
			//		if (_colorBandSet != null)
			//		{
			//			ClearTheCollection(ColorBands);
			//			_colorBandSet = value;
			//			Histogram.Reset();
			//			Debug.WriteLine("ColorBandViewModel is clearing its collection. (non-null => null.)");
			//			OnPropertyChanged(nameof(IColorBandSetViewModel.ColorBandSet));
			//		}
			//	}
			//	else
			//	{
			//		if (_colorBandSet == null || _colorBandSet != value)
			//		{
			//			ClearTheCollection(ColorBands);
			//			Histogram.Reset(value.HighCutOff + 1);
			//			PopulateHistorgram(_mapSections, Histogram);

			//			foreach (var c in value)
			//			{
			//				c.PropertyChanged += ColorBand_PropertyChanged;
			//				ColorBands.Add(c);
			//			}

			//			var view = CollectionViewSource.GetDefaultView(ColorBands);
			//			_ = view.MoveCurrentTo(ColorBands.FirstOrDefault());

			//			var upDesc = _colorBandSet == null ? "(null => non-null.)" : "(non-null => non-null.)";
			//			Debug.WriteLine($"ColorBandViewModel is updating its collection. {upDesc}");
			//			_colorBandSet = value;

			//			OnPropertyChanged(nameof(IColorBandSetViewModel.ColorBandSet));
			//		}
			//	}
			//}

			private set
			{
				Debug.WriteLine($"ColorBandViewModel is having is ColorBandSet updated. Current = {_colorBandSet?.SerialNumber}, New = {value?.SerialNumber}");
				if (value == null)
				{
					if (_colorBandSet != null)
					{
						Debug.WriteLine("ColorBandViewModel is clearing its collection. (non-null => null.)");

						_mapSectionHistogramProcessor.ProcessingEnabled = false;

						UnWatchTheCollection(_colorBandSet);

						_colorBandSet = value;
						Histogram.Reset();
						OnPropertyChanged(nameof(ColorBandSet));
						//OnPropertyChanged(nameof(ColorBands));
					}
				}
				else
				{
					//if (_colorBandSet == null || _colorBandSet != value)
					//{
						var upDesc = _colorBandSet == null ? "(null => non-null.)" : "(non-null => non-null.)";
						Debug.WriteLine($"ColorBandViewModel is updating its collection. {upDesc}");

						if (_colorBandSet != null)
						{
							UnWatchTheCollection(_colorBandSet);
						}

						_mapSectionHistogramProcessor.ProcessingEnabled = false;
						_colorBandSet = value;
						Histogram.Reset(value.HighCutOff + 1);
						PopulateHistorgram(_mapSections, Histogram);
						_mapSectionHistogramProcessor.ProcessingEnabled = true;

						WatchTheCollection(_colorBandSet);

						var view = CollectionViewSource.GetDefaultView(ColorBandSet);
						_ = view.MoveCurrentTo(ColorBandSet.FirstOrDefault());

						OnPropertyChanged(nameof(ColorBandSet));
					//}
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
						OnPropertyChanged(nameof(HighCutOff));
					}
				}
			}
		}

		public IHistogram Histogram { get; private set; }

		#endregion

		#region Event Handlers

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (ColorBandSet.Count == 0)
			{
				return;
			}

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				Histogram.Reset();
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				// Add items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					_mapSectionHistogramProcessor.AddWork(isAddOperation: true, mapSection, HandleHistogramUpdate);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
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

		private void HistogramChanged(object _)
		{
			double t = 0;
			foreach(var cb in ColorBandSet)
			{
				cb.Percentage = Math.Round(t, 4);
				t += 3.9;
			}
		}

		private void ColorBand_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (sender is ColorBand cb)
			{
				if (e.PropertyName == nameof(ColorBand.BlendStyle))
				{
					if (cb.BlendStyle == ColorBandBlendStyle.Next)
					{
						var idx = ColorBandSet.IndexOf(cb);

						if (idx != ColorBandSet.Count - 1)
						{
							cb.ActualEndColor = ColorBandSet[idx + 1].StartColor;
						}
					}
					else
					{
						cb.ActualEndColor = cb.BlendStyle == ColorBandBlendStyle.End ? cb.EndColor : cb.StartColor;
					}
				}
				else if (e.PropertyName == nameof(ColorBand.ActualEndColor))
				{
					if (cb.BlendStyle == ColorBandBlendStyle.End)
					{
						cb.EndColor = cb.ActualEndColor;
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void DeleteSelectedItem()
		{
			Debug.WriteLine($"Getting ready to remove an item. The list is: {_colorBandSet}.");

			var view = CollectionViewSource.GetDefaultView(ColorBandSet);

			if (view != null)
			{
				var x = view.CurrentItem as ColorBand;

				if (x != null)
				{
					//var idx = _colorBandSet.IndexOf(x);

					//if (!view.MoveCurrentToLast())
					//{
					//	Debug.WriteLine("Could not position view to next item.");
					//}

					//if (idx >= _colorBandSet.Count - 1)
					//{
					//	idx = _colorBandSet.Count - 1;
					//}

					//view.MoveCurrentToPosition(idx + 2);

					view.MoveCurrentToLast();

					var s = _colorBandSet.Remove(x);

					if (!s)
					{
						Debug.WriteLine("Could not remove the item.");
					}

					//var idx1 = _colorBandSet.IndexOf((ColorBand)view.CurrentItem);

					//Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {idx1}. The new list is: {_colorBandSet}.");



					//view.Refresh();
				}
			}
		}

		public void InsertItem()
		{
			var view = CollectionViewSource.GetDefaultView(ColorBandSet);
			view.Refresh();
			view.MoveCurrentToFirst();


			//var prevCutOff = SelectedColorBand.PreviousCutOff;
			//var cutOff = SelectedColorBand.CutOff;

			//if (cutOff - prevCutOff > 1)
			//{
			//	var idx = ColorBandSet.IndexOf(SelectedColorBand);

			//	var newCutoff = prevCutOff + (cutOff - prevCutOff) / 2;
			//	var newItem = new ColorBand(newCutoff, ColorBandColor.White, ColorBandBlendStyle.End, ColorBandColor.Black);

			//	_colorBandSet.Insert(idx, newItem);
			//}
		}

		public void ApplyChanges()
		{
			//_colorBandSet = new ColorBandSet(ColorBands);

			// Create a new copy with a new serial number to load a new ColorMap.
			//var newSet = _colorBandSet.CreateNewCopy();

			//if (new ColorBandSetComparer().Equals(_colorBandSet, newSet))
			//{
			//	Debug.WriteLine("The new ColorBandSet is sound.");
			//} 
			//else
			//{
			//	throw new InvalidOperationException("Creating a new copy of the ColorBands produces a result different that the current collection of ColorBands.");
			//}

			//Debug.Assert(new ColorBandSetComparer().Equals(_colorBandSet, newSet), "Creating a new copy of the ColorBands produces a result different that the current collection of ColorBands.");

			if (ColorBandsWereUpdatedProperly(_colorBandSet, out var newSet, out var mismatchedLines))
			{
				Debug.WriteLine("The new ColorBandSet is sound.");
			}
			else
			{
				Debug.WriteLine("Creating a new copy of the ColorBands produces a result different that the current collection of ColorBands.");
				Debug.WriteLine($"Updated: {_colorBandSet}, new: {newSet}");
				Debug.WriteLine($"The mismatched lines are: {string.Join(", ", mismatchedLines.Select(x => x.ToString()).ToArray())}");
			}

			_colorBandSet = newSet;
			//var view = CollectionViewSource.GetDefaultView(ColorBandSet);
			//view.Refresh();

			OnPropertyChanged(nameof(ColorBandSet));
		}

		private bool ColorBandsWereUpdatedProperly(ColorBandSet colorBandSet, out ColorBandSet pVersion, out IList<int> mismatchedLines)
		{
			pVersion = colorBandSet.CreateNewCopy();
			var result = new ColorBandSetComparer().EqualsExt(colorBandSet, pVersion, out mismatchedLines);
			return result;
		}

		//public void Test1()
		//{
		//	//var newColorBandSet = ColorBandSet.CreateNewCopy();
		//	//var len = newColorBandSet.Count;

		//	//var ocb = newColorBandSet[len - 3];
		//	//var ocb1 = newColorBandSet[1];
		//	//var ncb = new ColorBand(ocb.CutOff + 50, ocb1.StartColor, ocb1.BlendStyle, ocb1.EndColor);

		//	//newColorBandSet.Insert(len - 2, ncb);

		//	//ColorBandSet = newColorBandSet;

		//	Debug.WriteLine($"There are {Histogram[Histogram.UpperBound]} points that reached the target iterations.");

		//}

		//public void Test2()
		//{
		//	var newColorBandSet = new ColorBandSet();
		//	ColorBandSet = newColorBandSet;
		//}

		//public void Test3()
		//{
		//	var newColorBandSet = new ColorBandSet();

		//	newColorBandSet.Insert(0, new ColorBand(100, new ColorBandColor("#FF0000"), ColorBandBlendStyle.Next, new ColorBandColor("#00FF00")));

		//	ColorBandSet = newColorBandSet;
		//}

		//public void Test4()
		//{
		//	var newColorBandSet = new ColorBandSet();

		//	newColorBandSet.Insert(0, new ColorBand(100, new ColorBandColor("#FF0000"), ColorBandBlendStyle.Next, new ColorBandColor("#000000")));
		//	newColorBandSet.Insert(0, new ColorBand(50, new ColorBandColor("#880000"), ColorBandBlendStyle.Next, new ColorBandColor("#000000")));

		//	ColorBandSet = newColorBandSet;

		//}

		#endregion

		#region Private Methods

		private void WatchTheCollection(IList<ColorBand> colorBands)
		{
			foreach (var c in colorBands)
			{
				//c.PropertyChanged += ColorBand_PropertyChanged;
			}
		}

		private void UnWatchTheCollection(IList<ColorBand> colorBands)
		{
			foreach(var c in colorBands)
			{
				//c.PropertyChanged -= ColorBand_PropertyChanged;
			}
		}

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


		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
