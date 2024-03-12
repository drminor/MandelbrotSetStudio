﻿using MongoDB.Bson;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace MSetExplorer
{
	public interface ICbsHistogramViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		event EventHandler<DisplaySettingsInitializedEventArgs>? DisplaySettingsInitialized;
		event EventHandler<ColorBandSetUpdateRequestedEventArgs>? ColorBandSetUpdateRequested;

		ColorBandSetEditMode CurrentCbEditMode { get; set; }
		string CurrentCbEditModeAsString { get; }

		bool ColorBandUserControlHasErrors { get; set; }

		ColorBandSet ColorBandSet { get; set; }

		ObjectId ColorBandSetBeingEditedId { get; }
		ListCollectionView ColorBandsView { get; set; }
		ColorBand? CurrentColorBand { get; set; }
		int CurrentColorBandIndex { get; }
		int CurrentColorBandNumber { get; }
		int ColorBandsCount { get; }

		PercentageBand? BeyondTargetSpecs { get; }

		bool IsDirty { get; }
		bool IsEnabled { get; set; }

		bool HighlightSelectedBand { get; set; }
		bool UseEscapeVelocities { get; set; }
		bool UseRealTimePreview { get; set; }

		bool UsePercentagesLocalSetting { get; set; }
		bool UsePercentagesGlobalSetting { get; }
		bool UsePercentagesGlobally { get; set; }
		string PercentageUseIsGlobalDisplayStr { get; }
		string PercentageUseStatus { get; }

		//Visibility WindowVisibility { get; set; }

		void ApplyChanges();
		void ApplyChanges(int newTargetIterations);
		void RevertChanges();
		bool ApplyHistogram(bool histogramIsFromACompleteMap);

		bool TryMoveCurrentColorBandToNext();
		bool TryMoveCurrentColorBandToPrevious();

		void AdvanceEditMode();
		void RetardEditMode(); 


		bool TestInsertItem(int colorBandIndex);
		void CompleteCutoffInsertion(int index, ColorBand colorBand, ReservedColorBand reservedColorBand);
		ReservedColorBand CompleteColorInsertion(int index, ColorBand colorBand);
		void CompleteBandInsertion(int index, ColorBand colorBand);

		bool TestDeleteItem(int colorBandIndex);
		ReservedColorBand? CompleteCutoffRemoval(int index);
		void CompleteColorRemoval(int index, ReservedColorBand reservedColorBand);
		void CompleteBandRemoval(int index);

		ReservedColorBand PopReservedColorBand();
		void PushReservedColorBand(ReservedColorBand reservedColorBand);

		IDictionary<int, int> GetHistogramForColorBand(ColorBand color);

		HPlotSeriesData SeriesData { get; }

		SizeDbl UnscaledExtent { get; }				// Size of entire content at max zoom (i.e, 4 x Target Iterations)
		SizeDbl ViewportSize { get; set; }			// Size of display area in device independent pixels.
		SizeDbl ContentViewportSize { get; set; }   // Size of visible content

		VectorDbl DisplayPosition { get; }			// The index into the entire content of that pixel at the left edge of the visible area.

		double DisplayZoom { get; set; }			// Content Scale
		double MinimumDisplayZoom { get; set; }
		double MaximumDisplayZoom { get; set; }

		ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
		
		void RefreshDisplay();

		int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset);
		int? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale);

		int? MoveTo(VectorDbl displayPosition);
	}
}