using MongoDB.Bson;
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
		event PropertyChangedEventHandler PropertyChanged;

		SizeInt BlockSize { get; }
		SizeInt CanvasSize { get; set; }

		Project CurrentProject { get; }
		string CurrentProjectName { get; }
		IColorBandSet CurrentColorBandSet { get; set; }
		bool CanSaveProject { get; }
		bool CurrentProjectIsDirty { get; }

		// Job Methods
		Job CurrentJob { get; }
		bool CanGoBack { get; }
		bool CanGoForward { get; }
		bool GoBack();
		bool GoForward();

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateTargetInterations(int targetIterations, int iterationsPerRequest);

		// Project Methods
		void ProjectStartNew(MSetInfo mSetInfo, IColorBandSet colorBandSet);

		void ProjectCreate(string name, string description, IEnumerable<Guid> colorBandSetIds, IColorBandSet currentColorBandSet);

		bool ProjectOpen(string name);
		void ProjectSave();
		
		void ProjectSaveAs(string name, string description, IEnumerable<Guid> colorBandSetIds, IColorBandSet currentColorBandSet);

		void ProjectUpdateName(string name);
		void ProjectUpdateDescription(string description);

		// ColorBand Methods
		ColorBandSetW GetColorBandSet(Guid serialNumber);

	}
}