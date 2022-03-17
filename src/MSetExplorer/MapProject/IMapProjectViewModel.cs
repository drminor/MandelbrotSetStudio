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
		bool CanSaveProject { get; }
		bool CurrentProjectIsDirty { get; }

		//event EventHandler CurrentJobChanged;
		Job CurrentJob { get; }
		bool CanGoBack { get; }
		bool CanGoForward { get; }
		bool GoBack();
		bool GoForward();

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateTargetInterations(int targetIterations, int iterationsPerRequest);
		//void UpdateColorBands(ColorBandSet colorBandSet);

		void ProjectStartNew(MSetInfo mSetInfo, ColorBandSet colorBandSet);

		//void ProjectCreate(string projectName, string description, MSetInfo mSetInfo);
		void ProjectCreate(string name, string description, IEnumerable<Guid> colorBandSetIds, ColorBandSet currentColorBandSet);

		bool ProjectOpen(string name);
		void ProjectSave();
		
		void ProjectSaveAs(string name, string description, IEnumerable<Guid> colorBandSetIds, ColorBandSet currentColorBandSet);

		void ProjectUpdateName(string name);
		void ProjectUpdateDescription(string description);

	}
}