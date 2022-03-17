﻿using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		private static readonly bool _keepDisplaySquare = true;

		private readonly IMapLoaderManager _mapLoaderManager;

		private readonly IScreenSectionCollection _screenSectionCollection;

		private SizeInt _canvasSize;
		private VectorInt _canvasControlOffset;

		private Job _currentJob;
		private ColorMap _colorMap;

		#region Constructor

		public MapDisplayViewModel(IMapLoaderManager mapLoaderManager, SizeInt blockSize)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapLoaderManager.MapSectionReady += MapLoaderManager_MapSectionReady;

			BlockSize = blockSize;

			MapSections = new ObservableCollection<MapSection>();

			_screenSectionCollection = new ScreenSectionCollection(BlockSize);
			ImageSource = new DrawingImage(_screenSectionCollection.DrawingGroup);

			MapSections.CollectionChanged += MapSections_CollectionChanged;

			ContainerSize = new SizeDbl(1050, 1050);

			//_containerSize = new SizeDbl(1050, 1050);
			//CanvasSize = new SizeInt(1024, 1024);
			//var screenSectionExtent = new SizeInt(12, 12);

			CanvasControlOffset = new VectorInt();
		}

		#endregion

		#region Public Properties

		public event EventHandler<MapViewUpdateRequestedEventArgs> MapViewUpdateRequested;

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource { get; init; }

		public Job CurrentJob
		{
			get => _currentJob;
			set
			{
				if (value != _currentJob)
				{
					_currentJob = value;
					HandleCurrentJobChanged(_currentJob);
				}
			}
		}

		public ColorMap ColorMap
		{
			get => _colorMap;
			set
			{
				if (value != _colorMap)
				{
					_colorMap = value;
					HandleColorMapChanged(_colorMap);
				}
			}
		}

		public SizeInt BlockSize { get; }

		private SizeDbl _containerSize;
		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				_containerSize = value;
				var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(value, BlockSize, _keepDisplaySquare);
				_screenSectionCollection.CanvasSizeInWholeBlocks = sizeInWholeBlocks.Inflate(2);

				CanvasSize = sizeInWholeBlocks.Scale(BlockSize);
			}
		}

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (value != _canvasSize)
				{
					_canvasSize = value;
					OnPropertyChanged();
				}
			}
		}

		public VectorInt CanvasControlOffset
		{ 
			get => _canvasControlOffset;
			set { _canvasControlOffset = value; OnPropertyChanged(); }
		}

		public ObservableCollection<MapSection> MapSections { get; }

		#endregion

		#region Public Methods

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			var newArea = e.Area;
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, newArea));
		}

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
		{
			var offset = e.Offset;

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Invert();
			var newArea = new RectangleInt(new PointInt(invOffset), CanvasSize);
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.Pan, newArea));
		}

		#endregion

		#region Event Handlers

		private void MapLoaderManager_MapSectionReady(object sender, MapSection e)
		{
			// TODO: Use a lock on MapSectionReady to avoid race conditions as the ColorMap is applied.
			MapSections.Add(e);
		}

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				_screenSectionCollection.HideScreenSections();
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				// Add items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
					var pixels = MapSectionHelper.GetPixelArray(mapSection.Counts, mapSection.Size, _colorMap, !mapSection.IsInverted);
					_screenSectionCollection.Draw(mapSection.BlockPosition, pixels);
				}
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					if (!_screenSectionCollection.Hide(mapSection))
					{
						Debug.WriteLine($"While handling the MapSections_CollectionChanged:Remove, the MapDisplayViewModel cannot Hide the MapSection: {mapSection}.");
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private void HandleColorMapChanged(ColorMap colorMap)
		{
			var loadedSections = GetMapSectionsSnapShot();
			foreach (var mapSection in loadedSections)
			{
				//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
				var pixels = MapSectionHelper.GetPixelArray(mapSection.Counts, mapSection.Size, colorMap, !mapSection.IsInverted);
				_screenSectionCollection.Draw(mapSection.BlockPosition, pixels);
			}
		}

		private void HandleCurrentJobChanged(Job curJob)
		{
			_mapLoaderManager.StopCurrentJob();

			if (ShouldAttemptToReuseLoadedSections(curJob))
			{
				var sectionsRequired = MapSectionHelper.CreateEmptyMapSections(curJob);
				var loadedSections = GetMapSectionsSnapShot();

				// Avoid requesting sections already drawn
				var sectionsToLoad = GetNotYetLoaded(sectionsRequired, loadedSections);

				// Remove from the screen sections that are not part of the updated view.
				var uResults = UpdateMapSectionCollection(MapSections, sectionsRequired, out var shiftAmount);
				var cntRemoved = uResults.Item1;
				var cntRetained = uResults.Item2;
				var cntUpdated = uResults.Item3;

				Debug.WriteLine($"Panning: requesting {sectionsToLoad.Count} new sections, removing {cntRemoved}, retaining {cntRetained}, updating {cntUpdated}, shifting {shiftAmount}.");

				_screenSectionCollection.Shift(shiftAmount);
				CanvasControlOffset = curJob?.CanvasControlOffset ?? new VectorInt();
				RedrawSections(MapSections);
				_mapLoaderManager.Push(curJob, sectionsToLoad);
			}
			else
			{
				Debug.WriteLine($"Clearing Display. TransformType: {curJob.TransformType}.");
				MapSections.Clear();
				CanvasControlOffset = curJob?.CanvasControlOffset ?? new VectorInt();
				_mapLoaderManager.Push(curJob);
			}
		}

		private bool ShouldAttemptToReuseLoadedSections(Job job)
		{
			if (MapSections.Count == 0 || job.ParentJob is null || job.TransformType == TransformType.IterationUpdate || job.TransformType == TransformType.ColorMapUpdate)
			{
				return false;
			}

			return job.Subdivision.SamplePointDelta == job.ParentJob.Subdivision.SamplePointDelta;
		}

		private IList<MapSection> GetNotYetLoaded(IList<MapSection> source, IReadOnlyList<MapSection> current)
		{
			IList<MapSection> result = new List<MapSection>();

			foreach(var mapSection in source)
			{
				if (!current.Any(x => x == mapSection))
				{
					result.Add(mapSection);
				}
			}

			return result;
		}

		private Tuple<int, int, int> UpdateMapSectionCollection(ObservableCollection<MapSection> source, IList<MapSection> newSet, out VectorInt shiftAmount)
		{
			var cntRemoved = 0;
			var cntRetained = 0;
			var cntUpdated = 0;

			IList<MapSection> toBeRemoved = new List<MapSection>();
			shiftAmount = new VectorInt();

			foreach (var mapSection in source)
			{
				var matchingNewSection = newSet.FirstOrDefault(x => x == mapSection);
				if(matchingNewSection == null)
				{
					toBeRemoved.Add(mapSection);
					cntRemoved++;
				}
				else
				{
					var diff = matchingNewSection.BlockPosition.Sub(mapSection.BlockPosition);

					if (cntRetained == 0)
					{
						shiftAmount = diff;
					}
					else
					{
						if (shiftAmount != diff)
						{
							throw new InvalidOperationException($"The MapSection Collection contains inconsistent block positions. Compare: {diff} and {shiftAmount}.");
							//Debug.WriteLine("The MapSection Collection contains inconsistent block positions.");
						}
					}

					cntRetained++;

					if (mapSection.BlockPosition != matchingNewSection.BlockPosition)
					{
						mapSection.BlockPosition = matchingNewSection.BlockPosition;
						cntUpdated++;
					}
				}
			}

			foreach(var mapSection in toBeRemoved)
			{
				if (!source.Remove(mapSection))
				{
					Debug.WriteLine($"Could not remove MapSection: {mapSection}.");
				}
			}

			return new Tuple<int, int, int>(cntRemoved, cntRetained, cntUpdated);
		}

		private IReadOnlyList<MapSection> GetMapSectionsSnapShot()
		{
			return new ReadOnlyCollection<MapSection>(MapSections);
		}

		private void RedrawSections(IEnumerable<MapSection> source)
		{
			foreach (var mapSection in source)
			{
				//Debug.WriteLine($"About to redraw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
				_screenSectionCollection.Redraw(mapSection.BlockPosition);
			}
		}

		#endregion
	}
}
