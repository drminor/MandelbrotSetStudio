using MSS.Common.MSet;
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

		Project? CurrentProject { get; }
		string? CurrentProjectName { get; }
		bool CurrentProjectOnFile { get; }
		bool CurrentProjectIsDirty { get; }
		int GetGetNumberOfDirtyJobs();

		Job CurrentJob { get; }
		bool IsCurrentJobIdChanged { get; }

		ColorBandSet CurrentColorBandSet { get; set; }
		ColorBandSet? PreviewColorBandSet { get; set; }

		// Job Methods
		bool GoBack(bool skipPanJobs);
		bool GoForward(bool skipPanJobs);

		bool CanGoBack(bool skipPanJobs);
		bool CanGoForward(bool skipPanJobs);

		void UpdateMapView(TransformType transformType, VectorInt panAmount, double factor, MapAreaInfo2? diagnosticAreaInfo);

		MapAreaInfo2? GetUpdatedMapAreaInfo(TransformType transformType, RectangleInt screenArea, MapAreaInfo2 currentMapAreaInfo);

		// Project Methods
		void ProjectStartNew(RRectangle coords, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings);

		bool ProjectOpen(string name);
		bool ProjectSave();
		bool ProjectSaveAs(string name, string? description, [MaybeNullWhen(true)] out string errorText);
		void ProjectClose();

		long DeleteMapSectionsForUnsavedJobs();

		bool TryCreatePoster(string name, string? description, SizeInt posterSize, [MaybeNullWhen(false)] out Poster poster);
	}
}