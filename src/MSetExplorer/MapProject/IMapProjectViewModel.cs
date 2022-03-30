using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMapProjectViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler? PropertyChanged;

		SizeInt BlockSize { get; }
		SizeInt CanvasSize { get; set; }

		Project? CurrentProject { get; }
		string? CurrentProjectName { get; }
		bool CanSaveProject { get; }
		bool CurrentProjectIsDirty { get; }

		ColorBandSet? CurrentColorBandSet { get; set; }
		bool CanSaveColorBandSet { get; }
		bool CurrentColorBandSetIsDirty { get; }

		// Job Methods
		Job? CurrentJob { get; }
		bool CanGoBack { get; }
		bool CanGoForward { get; }
		bool GoBack();
		bool GoForward();

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateTargetInterations(int targetIterations, int iterationsPerRequest);

		// Project Methods
		void ProjectStartNew(MSetInfo mSetInfo, ColorBandSet colorBandSet);

		void ProjectCreate(string name, string description, IEnumerable<Guid> colorBandSetIds, ColorBandSet currentColorBandSet);

		bool ProjectOpen(string name);
		void ProjectSave();

		void ProjectSaveAs(string name, string? description, IEnumerable<Guid> colorBandSetIds, ColorBandSet currentColorBandSet);

		void ProjectUpdateName(string name);
		void ProjectUpdateDescription(string description);

		// ColorBand Methods
		bool ColorBandSetOpen(Guid serialNumber);

		void ColorBandSetSave();
		void ColorBandSetSaveAs(string name, string? description, int? versionNumber);

		ColorBandSet? GetColorBandSet(Guid serialNumber);
	}
}