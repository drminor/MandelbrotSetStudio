using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	public interface IProjectViewModel
	{
		bool InDesignMode { get; }

		event PropertyChangedEventHandler? PropertyChanged;

		SizeInt CanvasSize { get; set; }

		Project? CurrentProject { get; }

		string? CurrentProjectName { get; }
		bool CurrentProjectOnFile { get; }
		bool CurrentProjectIsDirty { get; }

		Job CurrentJob { get; }
		bool IsCurrentJobIdChanged { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		ColorBandSet CurrentColorBandSet { get; set; }
		ColorBandSet? PreviewColorBandSet { get; set; }

		// Job Methods
		bool GoBack(bool skipPanJobs);
		bool GoForward(bool skipPanJobs);

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		//void UpdateMapCoordinates(RRectangle coords);

		MapAreaInfo? GetUpdatedMapAreaInfo(TransformType transformType, RectangleInt screenArea);

		// Project Methods
		void ProjectStartNew(RRectangle coords, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings);
		bool ProjectOpen(string name);
		bool ProjectSave();
		void ProjectSaveAs(string name, string? description);
		void ProjectClose();

		long DeleteMapSectionsForUnsavedJobs();

		bool TryCreatePoster(string name, string? description, SizeInt posterSize, [MaybeNullWhen(false)] out Poster poster);
	}
}