using MSS.Types;
using MSS.Types.MSet;
using System;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMapProjectViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler PropertyChanged;

		event EventHandler CurrentJobChanged;

		SizeInt BlockSize { get; }
		SizeInt CanvasSize { get; set; }
		Project Project { get; }

		Job CurrentJob { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		bool GoBack();
		bool GoForward();

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void UpdateTargetInterations(int targetIterations, int iterationsPerRequest);
		void UpdateColorMapEntries(ColorBandSet colorBands);

		void StartNewProject(MSetInfo mSetInfo);
		void SaveProject(string projectName, string description);

		void LoadNewProject(string projectName, string description, MSetInfo mSetInfo);
		void LoadProject(string projectName);
		void SaveLoadedProject();

		void UpdateJob(Job oldJob, Job newJob);
	}
}