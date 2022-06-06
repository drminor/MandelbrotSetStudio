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
		double DisplayZoom { get; set; }

		Poster? CurrentPoster { get; }
		string? CurrentPosterName { get; }
		bool CurrentPosterOnFile { get; }
		bool CurrentPosterIsDirty { get; }

		JobAreaAndCalcSettings JobAreaAndCalcSettings { get; }

		ColorBandSet? CurrentColorBandSet { get; }

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateColorBandSet(ColorBandSet colorBandSet);
		void UpdateMapCoordinates(RRectangle coords);

		bool PosterOpen(string name);
		void PosterSave();
		bool PosterSaveAs(string name, string? description);
		void PosterClose();
	}
}
