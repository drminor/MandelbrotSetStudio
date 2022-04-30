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
		//bool CanSaveProject { get; }
		bool CurrentProjectOnFile { get; }
		bool CurrentProjectIsDirty { get; }
		bool IsCurrentJobIdChanged { get; }

		Job CurrentJob { get; }
		bool CanGoBack { get; }
		bool CanGoForward { get; }

		ColorBandSet CurrentColorBandSet { get; }

		// Job Methods
		bool GoBack();
		bool GoForward();

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateColorBandSet(ColorBandSet colorBandSet);
		void UpdateTargetInterations(int targetIterations);
		void UpdateMapCoordinates(RRectangle coords);
		RRectangle? GetUpdateCoords(TransformType transformType, RectangleInt newArea);

		// Project Methods
		void ProjectStartNew(MSetInfo mSetInfo, ColorBandSet colorBandSet);
		bool ProjectOpen(string name);
		void ProjectSave();
		void ProjectSaveAs(string name, string? description);

}
}