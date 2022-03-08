using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IJobStack
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler PropertyChanged;

		event EventHandler CurrentJobChanged;

		SizeInt BlockSize { get; }
		SizeInt CanvasSize { get; set; }
		Project Project { get; }

		IEnumerable<Job> Jobs { get; }
		Job CurrentJob { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		bool GoBack();
		bool GoForward();

		void UpdateMapView(TransformType transformType, RectangleInt newArea);

		void LoadNewProject(string projectName, MSetInfo mSetInfo);
		void LoadProject(string projectName);
		void SaveProject();

		void UpdateJob(Job oldJob, Job newJob);
	}
}