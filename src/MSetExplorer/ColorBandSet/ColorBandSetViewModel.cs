﻿using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandSetViewModel : INotifyPropertyChanged
	{
		private readonly ObservableCollection<MapSection> _mapSections;
		private readonly SynchronizationContext? _synchronizationContext;
		private readonly MapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private double _rowHeight;
		private double _itemWidth;

		private Project? _currentProject;
		private ColorBandSet? _colorBandSet;
		private ListCollectionView? _colorBandsView;

		private bool _isDirty;

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
			_colorBandSet = new ColorBandSet();
			_colorBandsView = null;
			_isDirty = false;

			_mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public double RowHeight
		{
			get => _rowHeight;
			set { _rowHeight = value; OnPropertyChanged(nameof(RowHeight)); }
		}

		public double ItemWidth
		{
			get => _itemWidth;
			set { _itemWidth = value; OnPropertyChanged(nameof(ItemWidth)); }
		}

		public Project? CurrentProject
		{
			get => _currentProject;
			set
			{
				if (value != _currentProject)
				{
					_currentProject = value;

					// Clone this to keep changes made here from updating the Project's copy.
					if (value != null)
					{
						ColorBandSet = value.CurrentColorBandSet.Clone();
					}

					OnPropertyChanged(nameof(CurrentProject));
				}
			}
		}

		public ListCollectionView ColorBandsView
		{
			get
			{
				if (_colorBandsView == null)
				{
					_colorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet?.ColorBands);
					_colorBandsView.SortDescriptions.Add(new SortDescription("CutOff", ListSortDirection.Ascending));
				}

				return _colorBandsView;
			}

			set
			{
				_colorBandsView = value;
				OnPropertyChanged();
			}
		}

		public ColorBandSet? ColorBandSet
		{
			get => _colorBandSet;

			private set
			{
				//Debug.WriteLine($"ColorBandViewModel is having is ColorBandSet updated. Current = {_colorBandSet?.SerialNumber}, New = {value?.SerialNumber}");
				if (value == null)
				{
					if (_colorBandSet != null)
					{
						Debug.WriteLine("ColorBandViewModel is clearing its collection. (non-null => null.)");

						_mapSectionHistogramProcessor.ProcessingEnabled = false;

						_colorBandSet = value;
						Histogram.Reset();

						_colorBandsView = null;
						IsDirty = false;

						OnPropertyChanged(nameof(ColorBandSet));
						OnPropertyChanged(nameof(ColorBandsView));
					}
				}
				else
				{
					if (_colorBandSet == null || _colorBandSet != value)
					{
						var upDesc = _colorBandSet == null ? "(null => non-null.)" : "(non-null => non-null.)";
						Debug.WriteLine($"ColorBandViewModel is updating its collection. {upDesc}. The new ColorBandSet has SerialNumber: {value.SerialNumber}.");

						_mapSectionHistogramProcessor.ProcessingEnabled = false;
						_colorBandSet = value;
						Histogram.Reset(value.HighCutOff + 1);
						PopulateHistorgram(_mapSections, Histogram);
						_mapSectionHistogramProcessor.ProcessingEnabled = true;

						_colorBandsView = null;
						var view = ColorBandsView;
						_ = view.MoveCurrentTo(_colorBandSet.FirstOrDefault());

						IsDirty = false;
						OnPropertyChanged(nameof(ColorBandSet));
						OnPropertyChanged(nameof(ColorBandsView));
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
						OnPropertyChanged(nameof(HighCutOff));
					}
				}
			}
		}

		public IHistogram Histogram { get; private set; }

		public bool IsDirty
		{
			get => _isDirty;

			private set
			{
				if (value != _isDirty)
				{
					_isDirty = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Event Handlers

		private void MapSections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (_colorBandSet != null && _colorBandSet.Count == 0)
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

		private void HistogramChanged(object? _)
		{
			if (_colorBandSet != null)
			{
				double t = 0;
				foreach (var cb in _colorBandSet.ColorBands)
				{
					cb.Percentage = Math.Round(t, 4);
					t += 3.9;
				}
			}
		}

		#endregion

		#region Public Methods

		public void InsertItem(int index, ColorBand newItem)
		{
			//Debug.WriteLine($"At InsertItem, the view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			_colorBandSet?.Insert(index, newItem);

			IsDirty = true;
		}

		public void ItemWasUpdated()
		{
			IsDirty = true;
		}

		public void DeleteSelectedItem()
		{
			var view = ColorBandsView;

			//Debug.WriteLine($"Getting ready to remove an item. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			if (view != null && _colorBandSet != null)
			{
				if (view.CurrentItem is ColorBand curItem)
				{
					var idx = _colorBandSet.IndexOf(curItem);

					if (idx >= _colorBandSet.Count - 1)
					{
						idx = _colorBandSet.Count - 1;
					}

					if (!view.MoveCurrentToPosition(idx))
					{
						Debug.WriteLine("Could not position view to next item.");
					}

					if (!_colorBandSet.Remove(curItem))
					{
						Debug.WriteLine("Could not remove the item.");
					}

					var idx1 = _colorBandSet.ColorBands.IndexOf((ColorBand)view.CurrentItem);

					Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {idx1}. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

					IsDirty = true;
				}
			}
		}

		public void ApplyChanges()
		{
			if (ColorBandSet != null)
			{
				//Debug.WriteLine($"Applying changes, the view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

				// Create a new copy with a new serial number to load a new ColorMap.
				var newSet = ColorBandSet.CreateNewCopy();
				//CheckThatColorBandsWereUpdatedProperly(_colorBandSet, newSet, throwOnMismatch: false);

				Debug.WriteLine($"The ColorBandSetViewModel is Applying changes. The new SerialNumber is {newSet.SerialNumber}, name: {newSet.Name}, version: {newSet.VersionNumber}.");

				ColorBandSet = newSet;
			}
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
			_synchronizationContext?.Post(o => HistogramChanged(o), null);
		}

		//private void CheckThatColorBandsWereUpdatedProperly(ColorBandSet colorBandSet, ColorBandSet goodCopy, bool throwOnMismatch)
		//{
		//	var theyMatch = new ColorBandSetComparer().EqualsExt(colorBandSet, goodCopy, out var mismatchedLines);

		//	if (theyMatch)
		//	{
		//		Debug.WriteLine("The new ColorBandSet is sound.");
		//	}
		//	else
		//	{
		//		Debug.WriteLine("Creating a new copy of the ColorBands produces a result different that the current collection of ColorBands.");
		//		Debug.WriteLine($"Updated: {_colorBandSet}, new: {goodCopy}");
		//		Debug.WriteLine($"The mismatched lines are: {string.Join(", ", mismatchedLines.Select(x => x.ToString()).ToArray())}");

		//		if (throwOnMismatch)
		//		{
		//			throw new InvalidOperationException("ColorBandSet update mismatch.");
		//		}
		//	}
		//}

		private string GetViewAsString()
		{
			var view = ColorBandsView;
			var colorBands = view.SourceCollection.Cast<ColorBand>().ToList();
			var result = GetString(colorBands);

			return result;
		}

		private string GetModelAsString()
		{
			var result = GetString(_colorBandSet?.ColorBands);

			return result;
		}

		private string GetString(ICollection<ColorBand>? colorBands)
		{
			if (colorBands != null)
			{
				var sb = new StringBuilder();

				foreach (var cb in colorBands)
				{
					_ = sb.AppendLine(cb.ToString());
				}

				return sb.ToString();
			}
			else
			{
				return "Empty";
			}
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
