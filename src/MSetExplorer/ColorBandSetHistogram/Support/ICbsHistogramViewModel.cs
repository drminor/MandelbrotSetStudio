﻿using MSS.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

		bool EditingCutoffs { get; set; }
		bool EditingColors { get; set; }

		bool ColorBandUserControlHasErrors { get; set; }

		ColorBandSet ColorBandSet { get; set; }
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

		//Visibility WindowVisibility { get; set; }

		void ApplyChanges();
		void ApplyChanges(int newTargetIterations);
		void RevertChanges();
		void RefreshPercentages();

		bool TryMoveCurrentColorBandToNext();
		bool TryMoveCurrentColorBandToPrevious();

		void AdvanceEditMode();
		void RetardEditMode(); 

		bool TryInsertNewItem(ColorBand colorBand, out int index);

		//bool TryDeleteItem(ColorBand colorBand);


		//int GetIndexOf(ColorBand colorBand);

		bool TestInsertItem(int colorBandIndex, [NotNullWhen(true)] out ColorBandSetEditOperation? colorBandSetEditOperation);
		void CompleteCutoffInsertion(int index);
		void CompleteColorInsertion(int index);
		void CompleteBandInsertion(int index);

		bool TestDeleteItem(int colorBandIndex, [NotNullWhen(true)] out ColorBandSetEditOperation? colorBandSetEditOperation);
		void CompleteCutoffRemoval(int index);
		void CompleteColorRemoval(int index);
		void CompleteBandRemoval(int index);

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
		
		bool RefreshDisplay();

		int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset);
		int? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale);

		int? MoveTo(VectorDbl displayPosition);

		//CbListView SelectedItems { get; }

		//void ClearSelectedItems();
	}
}