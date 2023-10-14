﻿using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		
		//event EventHandler<int>? DisplayJobCompleted;
		event EventHandler<MapViewUpdateCompletedEventArgs>? MapViewUpdateCompleted;


		//event EventHandler<JobProgressInfo>? RequestAdded;
		//event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		event EventHandler<DisplaySettingsInitializedEventArgs>? DisplaySettingsInitialized;

		SizeInt BlockSize { get; }
		ObservableCollection<MapSection> MapSections { get; }
		
		AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings { get; }
		MapAreaInfo? LastMapAreaInfo { get; }

		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; }
		int SelectedColorBandIndex { get; set; }


		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }

		ImageSource ImageSource { get; }
		VectorInt ImageOffset { get; }

		void RaiseMapViewZoomUpdate(AreaSelectedEventArgs e);
		void RaiseMapViewPanUpdate(ImageDraggedEventArgs e);

		MsrJob? SubmitJob(AreaColorAndCalcSettings newValue);
		void SubmitJob(AreaColorAndCalcSettings newValue, SizeDbl posterSize, VectorDbl displayPosition, double displayZoom);

		void CancelJob();
		void PauseJob();
		MsrJob? RestartJob();
		void ClearDisplay();

		SizeDbl UnscaledExtent { get; }
		SizeDbl ViewportSize { get; set; }
		SizeDbl LogicalViewportSize { get; }
		SizeDbl? ContentViewportSize { get; }

		//ValueTuple<VectorDbl, double>? ScaledDisplayPositionYInverted { get; set; }
		//VectorDbl GetCurrentDisplayPosition();
		//double DisplayPositionX { get; set; }
		//double DisplayPositionY { get; set; }

		VectorDbl DisplayPosition { get; }

		double DisplayZoom { get; set; }
		double MinimumDisplayZoom { get; set; }
		double MaximumDisplayZoom { get; set; }

		MsrJob? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset);
		MsrJob? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale);

		MsrJob? MoveTo(VectorDbl contentOffset);
	}
}