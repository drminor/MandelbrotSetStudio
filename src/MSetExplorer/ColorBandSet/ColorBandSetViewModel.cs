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
using System.Text;
using System.Threading;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandSetViewModel : INotifyPropertyChanged
	{
		private readonly ObservableCollection<MapSection> _mapSections;
		private readonly SynchronizationContext _synchronizationContext;
		private readonly MapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private double _rowHeight;
		private double _itemWidth;

		private Project _currentProject;

		//private ObservableCollection<ColorBand> _colorBands;

		private ColorBandSet _colorBandSet;

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

			//_colorBands = null;
			//ColorBands = new ObservableCollection<ColorBand>();

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

		//public ObservableCollection<ColorBand> ColorBands
		//{
		//	get => _colorBands;
		//	private set
		//	{
		//		//if (_colorBands != null)
		//		//{
		//		//	var view = CollectionViewSource.GetDefaultView(_colorBands);
		//		//	view.CurrentChanged -= View_CurrentChanged;
		//		//}

		//		_colorBands = value;
				
		//		//if (_colorBands != null)
		//		//{
		//		//	var view = CollectionViewSource.GetDefaultView(_colorBands);
		//		//	view.CurrentChanged += View_CurrentChanged;
		//		//	view.MoveCurrentToFirst();
		//		//}

		//		OnPropertyChanged();
		//	}
		//}

		//public ObservableCollection<ColorBand> ColorBands
		//{
		//	get => _colorBands;
		//	private set
		//	{
		//		_colorBands = value;
		//		OnPropertyChanged();
		//	}
		//}

		public ListCollectionView ColorBandsView
		{
			get => (ListCollectionView) CollectionViewSource.GetDefaultView(_colorBandSet.ColorBands);
			set
			{

			}
		}

		public ColorBandSet ColorBandSet
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

						//UnWatchTheCollection(_colorBandSet);

						_colorBandSet = value;
						Histogram.Reset();

						//SelectedColorBand = null;
						//_colorBands.Clear();

						OnPropertyChanged(nameof(ColorBandSet));
						OnPropertyChanged(nameof(ColorBandsView));
						//OnPropertyChanged(nameof(ColorBands));
					}
				}
				else
				{
					if (_colorBandSet == null || _colorBandSet != value)
					{
						var upDesc = _colorBandSet == null ? "(null => non-null.)" : "(non-null => non-null.)";
						Debug.WriteLine($"ColorBandViewModel is updating its collection. {upDesc}. The new ColorBandSet has SerialNumber: {value.SerialNumber}.");

						//if (_colorBandSet != null)
						//{
						//	UnWatchTheCollection(_colorBandSet);
						//}

						_mapSectionHistogramProcessor.ProcessingEnabled = false;
						_colorBandSet = value;
						Histogram.Reset(value.HighCutOff + 1);
						PopulateHistorgram(_mapSections, Histogram);
						_mapSectionHistogramProcessor.ProcessingEnabled = true;

						//WatchTheCollection(_colorBandSet);

						//_colorBands.Clear();
						//foreach(var cc in _colorBandSet)
						//{
						//	_colorBands.Add(cc);
						//}

						var view = ColorBandsView;
						_ = view.MoveCurrentTo(_colorBandSet.FirstOrDefault());

						//SelectedColorBand = ((ColorBand)view.CurrentItem).Clone();

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

		#endregion

		#region Event Handlers

		private void MapSections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_colorBandSet.Count == 0)
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
			foreach (var cb in _colorBandSet.ColorBands)
			{
				cb.Percentage = Math.Round(t, 4);
				t += 3.9;
			}
		}

		//private void SelectedColorBand_PropertyChanged(object sender, PropertyChangedEventArgs e)
		//{
		//	var editableView = GetEditableView();
		//	var view = (ICollectionView)editableView;

		//	view.CurrentChanged -= View_CurrentChanged;
		//	var cb = (ColorBand)view.CurrentItem;


		//	if (e.PropertyName == nameof(ColorBand.CutOff))
		//	{
		//		HandleUpdatedCutOff(SelectedColorBand, cb, editableView);
		//	}
		//	else if (e.PropertyName == nameof(ColorBand.StartColor))
		//	{
		//		HandleUpdatedStartColor(SelectedColorBand, cb, editableView);
		//	}
		//	else if (e.PropertyName == nameof(ColorBand.BlendStyle))
		//	{
		//		HandleUpdatedBlendStyle(SelectedColorBand, cb, editableView);
		//	}
		//	else if (e.PropertyName == nameof(ColorBand.ActualEndColor))
		//	{
		//		HandleUpdatedEndColor(SelectedColorBand, cb, editableView);
		//	}

		//	var wasMoved = view.MoveCurrentTo(cb);

		//	view =  CollectionViewSource.GetDefaultView(ColorBands);

		//	view.CurrentChanged += View_CurrentChanged;
		//}

		//private void HandleUpdatedCutOff(ColorBand source, ColorBand target, IEditableCollectionView editableView)
		//{

		//}

		//private void HandleUpdatedStartColor(ColorBand source, ColorBand target, IEditableCollectionView editableView)
		//{
		//	editableView.EditItem(target);
		//	target.StartColor = source.StartColor;
		//	editableView.CommitEdit();

		//}

		//private void HandleUpdatedBlendStyle(ColorBand source, ColorBand target, IEditableCollectionView editableView)
		//{
		//	//if (updatedCb.BlendStyle == ColorBandBlendStyle.End)
		//	//{
		//	//	updatedCb.EndColor = updatedCb.ActualEndColor;
		//	//}

		//}

		//private void HandleUpdatedEndColor(ColorBand source, ColorBand target, IEditableCollectionView editableView)
		//{
		//	////view.MoveCurrentToLast();

		//	//var idx = ColorBandSet.IndexOf(cb);

		//	//var cbToUpdate = _colorBandSet[idx];

		//	//cbToUpdate.EndColor = updatedCb.EndColor;
		//	//cbToUpdate.ActualEndColor = updatedCb.ActualEndColor;
		//	//cbToUpdate.PreviousCutOff = updatedCb.PreviousCutOff;

		//}

		#endregion

		#region Public Methods

		public void DeleteSelectedItem()
		{
			var view = ColorBandsView;

			Debug.WriteLine($"Getting ready to remove an item. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			if (view != null)
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

					//Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {idx1}. The new list is: {_colorBandSet}.");
					Debug.WriteLine($"Removed item at former index: {idx}. The new index is: {idx1}. The view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

					//view.Refresh();
				}
			}
		}

		public void InsertItem()
		{
			Debug.WriteLine($"At InsertItem, the view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			//_colorBandSet.Fix();

			//view.Refresh();
			//view.MoveCurrentToFirst();


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
			if (_colorBandSet == null)
			{
				return;
			}

			//Debug.WriteLine($"Applying changes, the view is {GetViewAsString()}\nOur model is {GetModelAsString()}");

			// Create a new copy with a new serial number to load a new ColorMap.
			var newSet = new ColorBandSet(ColorBandSet);

			CheckThatColorBandsWereUpdatedProperly(_colorBandSet, newSet, throwOnMismatch: false);

			ColorBandSet = newSet;
		}

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

		private void CheckThatColorBandsWereUpdatedProperly(ColorBandSet colorBandSet, ColorBandSet goodCopy, bool throwOnMismatch)
		{
			var theyMatch = new ColorBandSetComparer().EqualsExt(colorBandSet, goodCopy, out var mismatchedLines);

			if (theyMatch)
			{
				Debug.WriteLine("The new ColorBandSet is sound.");
			}
			else
			{
				Debug.WriteLine("Creating a new copy of the ColorBands produces a result different that the current collection of ColorBands.");
				Debug.WriteLine($"Updated: {_colorBandSet}, new: {goodCopy}");
				Debug.WriteLine($"The mismatched lines are: {string.Join(", ", mismatchedLines.Select(x => x.ToString()).ToArray())}");

				if (throwOnMismatch)
				{
					throw new InvalidOperationException("ColorBandSet update mismatch.");
				}
			}
		}

		private string GetViewAsString()
		{
			var view = ColorBandsView;
			var colorBands = view.SourceCollection.Cast<ColorBand>().ToList();
			var result = GetString(colorBands);

			return result;
		}

		private string GetModelAsString()
		{
			var result = GetString(_colorBandSet.ColorBands);

			return result;
		}

		private string GetString(ICollection<ColorBand> colorBands)
		{
			var sb = new StringBuilder();

			foreach (var cb in colorBands)
			{
				_ = sb.AppendLine(cb.ToString());
			}

			return sb.ToString();
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
