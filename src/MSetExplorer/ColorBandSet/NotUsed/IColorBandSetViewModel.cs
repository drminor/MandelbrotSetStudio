using MSS.Types;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;

namespace MSetExplorer
{
	public interface IColorBandSetViewModel
	{
		event EventHandler<ColorBandSetUpdateRequestedEventArgs>? ColorBandSetUpdateRequested;


		ColorBandSet? ColorBandSet { get; set; }
		ListCollectionView ColorBandsView { get; set; }
		ColorBand? CurrentColorBand { get; set; }

		PercentageBand? BeyondTargetSpecs { get; }

		bool IsDirty { get; }
		bool IsEnabled { get; set; }

		bool HighlightSelectedBand { get; set; }
		bool UseEscapeVelocities { get; set; }
		bool UseRealTimePreview { get; set; }

		Visibility WindowVisibility { get; set; }


		double ItemWidth { get; set; }
		double RowHeight { get; set; }

		ColorBandSetEditMode EditMode { get; set; }

		void ApplyChanges();
		void ApplyChanges(int newTargetIterations);
		void RevertChanges();
		void RefreshPercentages();

		bool TryInsertNewItem(out int index);
		bool TryDeleteSelectedItem();

		IDictionary<int, int> GetHistogramForColorBand(int index);

		//bool UpdateCutoff(int colorBandIndex, int newCutoff);


	}
}