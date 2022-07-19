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

		Job CurrentJob { get; set; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		ColorBandSet CurrentColorBandSet { get; }

		// Job Methods
		bool GoBack(bool skipPanJobs);
		bool GoForward(bool skipPanJobs);

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateColorBandSet(ColorBandSet colorBandSet);
		void UpdateMapCoordinates(RRectangle coords);

		MapAreaInfo? GetUpdatedMapAreaInfo(TransformType transformType, RectangleInt screenArea);

		// Project Methods
		void ProjectStartNew(RRectangle coords, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings);
		bool ProjectOpen(string name);
		bool ProjectSave();
		void ProjectSaveAs(string name, string? description);
		void ProjectClose();

		long DeleteMapSectionsForUnsavedJobs();

		Poster PosterCreate(string name, string? description, SizeInt posterSize);
	}
}