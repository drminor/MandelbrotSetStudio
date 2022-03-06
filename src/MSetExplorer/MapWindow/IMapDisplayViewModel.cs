using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler PropertyChanged;

		Project CurrentProject { get; set; }

		SizeInt BlockSize { get; }
		ImageSource ImageSource { get; }
		ObservableCollection<MapSection> MapSections { get; }

		// These may need to be dependency properties
		VectorInt CanvasControlOffset { get; set; }
		void SetCanvasSize(SizeInt canvasSize);
		void SetMapInfo(MSetInfo mSetInfo);

		// These will become ICommands
		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

		// These will be part of the JobStack control
		IEnumerable<Job> Jobs { get; }
		Job CurrentJob { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		void GoBack();
		void GoForward();

		void LoadJobStack(IEnumerable<Job> jobs);
		void UpdateJob(Job oldJob, Job newJob);
	}
}