﻿using MongoDB.Bson;
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

		ColorBandSet CurrentColorBandSet { get; }


		Job CurrentJob { get; }
		bool CanGoBack { get; }
		bool CanGoForward { get; }

		// Job Methods
		bool GoBack();
		bool GoForward();

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateColors(ColorBandSet colorBandSet);
		void UpdateTargetInterations(int targetIterations);
		void UpdateMapCoordinates(RRectangle coords);
		RRectangle? GetUpdateCoords(TransformType transformType, RectangleInt newArea);

		// Project Methods
		void ProjectStartNew(MSetInfo mSetInfo, ColorBandSet colorBandSet);

		//void ProjectCreate(string name, string description, ObjectId currentColorBandSetId);

		bool ProjectOpen(string name);
		void ProjectSave();
		void ProjectSaveAs(string name, string? description);

		//void ProjectUpdateName(string name);
		//void ProjectUpdateDescription(string description);
	}
}