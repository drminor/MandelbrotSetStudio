using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMapProjectViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler? PropertyChanged;

		SizeInt CanvasSize { get; set; }

		Project? CurrentProject { get; }

		string? CurrentProjectName { get; }
		bool CurrentProjectOnFile { get; }
		bool CurrentProjectIsDirty { get; }
		bool IsCurrentJobIdChanged { get; }

		Job CurrentJob { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		ColorBandSet CurrentColorBandSet { get; }

		// Job Methods
		bool GoBack(bool skipPanJobs);
		bool GoForward(bool skipPanJobs);

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateColorBandSet(ColorBandSet colorBandSet);
		//void UpdateTargetInterations(int targetIterations);
		void UpdateMapCoordinates(RRectangle coords);
		JobAreaInfo? GetUpdatedJobAreaInfo(TransformType transformType, RectangleInt newArea);

		// Project Methods
		void ProjectStartNew(RRectangle coords, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings);
		bool ProjectOpen(string name);
		void ProjectSave();
		bool ProjectSaveAs(string name, string? description);
		void ProjectClose();

		long? DeleteMapSectionsSinceLastSave();

		void PosterCreate(Poster poster);

	}
}