﻿using MSS.Types;
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
	public class ColorBandSetViewModel : ViewModelBase, IColorBandSetViewModel
	{
		private readonly ObservableCollection<MapSection> _mapSections;
		private readonly SynchronizationContext _synchronizationContext;
		private readonly MapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private double _rowHeight;
		private double _itemWidth;

		private Project _currentProject;
		private ColorBandSet _colorBandSet;
		private ColorBand _selectedColorBand;

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
			SelectedColorBand = null;

			_mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

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

		public ObservableCollection<ColorBand> ColorBands => _colorBandSet ?? new ObservableCollection<ColorBand>(); // { get; private set; }

		public Project CurrentProject
		{
			get => _currentProject;
			set
			{
				if (value != _currentProject)
				{
					_currentProject = value;
					ColorBandSet = value.CurrentColorBandSet;
					OnPropertyChanged(nameof(IColorBandSetViewModel.CurrentProject));
				}
			}
		}

		public ColorBand SelectedColorBand
		{
			get => _selectedColorBand;

			set
			{
				_selectedColorBand = value;
				OnPropertyChanged(nameof(IColorBandSetViewModel.SelectedColorBand));
			}
		}

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
						_colorBandSet = value;
						Histogram.Reset();
						OnPropertyChanged(nameof(IColorBandSetViewModel.ColorBandSet));
						OnPropertyChanged(nameof(IColorBandSetViewModel.ColorBands));
					}
				}
				else
				{
					if (_colorBandSet == null || _colorBandSet != value)
					{
						var upDesc = _colorBandSet == null ? "(null => non-null.)" : "(non-null => non-null.)";
						Debug.WriteLine($"ColorBandViewModel is updating its collection. {upDesc}");

						_mapSectionHistogramProcessor.ProcessingEnabled = false;
						Histogram.Reset(value.HighCutOff + 1);
						PopulateHistorgram(_mapSections, Histogram);
						_mapSectionHistogramProcessor.ProcessingEnabled = true;

						_colorBandSet = value;

						var view = CollectionViewSource.GetDefaultView(ColorBands);
						_ = view.MoveCurrentTo(ColorBands.FirstOrDefault());

						OnPropertyChanged(nameof(IColorBandSetViewModel.ColorBandSet));
						OnPropertyChanged(nameof(IColorBandSetViewModel.ColorBands));
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
						OnPropertyChanged(nameof(IColorBandSetViewModel.HighCutOff));
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

		private void HistogramChanged(object _)
		{
			double t = 0;
			foreach(var cb in ColorBands)
			{
				cb.Percentage = Math.Round(t, 4);
				t += 3.9;
			}
		}

		private void ColorBand_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender is ColorBand cb)
			{
				if (e.PropertyName == nameof(ColorBand.BlendStyle))
				{
					if (cb.BlendStyle == ColorBandBlendStyle.Next)
					{
						var idx = ColorBands.IndexOf(cb);

						if (idx != ColorBands.Count - 1)
						{
							cb.ActualEndColor = ColorBands[idx + 1].StartColor;
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
			_colorBandSet.Remove(SelectedColorBand);
		}

		public void InsertItem()
		{
			var prevCutOff = SelectedColorBand.PreviousCutOff;
			var cutOff = SelectedColorBand.CutOff;

			if (cutOff - prevCutOff > 1)
			{
				var idx = ColorBands.IndexOf(SelectedColorBand);

				var newCutoff = prevCutOff + (cutOff - prevCutOff) / 2;
				var newItem = new ColorBand(newCutoff, ColorBandColor.White, ColorBandBlendStyle.End, ColorBandColor.Black);

				_colorBandSet.Insert(idx, newItem);
			}
		}

		public void ApplyChanges()
		{
			_colorBandSet = new ColorBandSet(ColorBands);
			OnPropertyChanged(nameof(IColorBandSetViewModel.ColorBandSet));
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

		private void ClearTheCollection(ObservableCollection<ColorBand> colorBands)
		{
			foreach(var c in colorBands)
			{
				c.PropertyChanged -= ColorBand_PropertyChanged;
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
	}
}
