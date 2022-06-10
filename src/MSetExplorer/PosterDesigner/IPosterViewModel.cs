using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IPosterViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler? PropertyChanged;

		SizeInt CanvasSize { get; set; }
		SizeInt LogicalDisplaySize { get; set; }

		Poster? CurrentPoster { get; }
		string? CurrentPosterName { get; }
		bool CurrentPosterOnFile { get; }
		bool CurrentPosterIsDirty { get; }

		ColorBandSet? ColorBandSet { get; set; }

		VectorInt DisplayPosition { get; set; }
		double DisplayZoom { get; set; }
		//double MinimumDisplayZoom { get; }

		JobAreaAndCalcSettings JobAreaAndCalcSettings { get; }

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		//void UpdateMapCoordinates(RRectangle coords);
		void UpdateColorBandSet(ColorBandSet colorBandSet);

		bool PosterOpen(string name);
		void PosterSave();
		bool PosterSaveAs(string name, string? description);
		void PosterClose();
	}
}
