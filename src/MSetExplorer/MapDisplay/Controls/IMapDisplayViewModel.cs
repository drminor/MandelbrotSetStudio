﻿using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		SizeInt BlockSize { get; }

		WriteableBitmap Bitmap { get; }

		//BigVector MapBlockOffset { get; }

		ObservableCollection<MapSection> MapSections { get; }
		AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings { get; set; }

		ColorBandSet ColorBandSet { get; set; }

		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }

		bool HandleContainerSizeUpdates { get; set; }
		SizeDbl ContainerSize { get; set; }
		
		SizeDbl CanvasSize { get; }
		VectorInt CanvasControlOffset { get; set; }

		SizeDbl LogicalDisplaySize { get; }
		double DisplayZoom { get; set; }

		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		event EventHandler<int>? DisplayJobCompleted;

		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

		// New Methods to replace the Update... methods above.
		void SubmitJob(AreaColorAndCalcSettings job);
		void CancelJob();
		void RestartLastJob();
		void ClearDisplay();

	}
}