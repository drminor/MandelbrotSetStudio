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
		public event PropertyChangedEventHandler PropertyChanged;

		bool InDesignMode { get; }

		ImageSource ImageSource { get; }
		SizeInt BlockSize { get; }
		SizeInt CanvasSize { get; set; }
		VectorInt CanvasControlOffset { get; set; }

		ObservableCollection<MapSection> MapSections { get; }
		IReadOnlyList<MapSection> GetMapSectionsSnapShot();

		public Project CurrentProject { get; set; }

		public IEnumerable<Job> Jobs { get; }
		public Job CurrentJob { get; }

		public bool CanGoBack { get; }
		public bool CanGoForward { get; }

		public void GoBack();
		public void GoForward();

		public void LoadJobStack(IEnumerable<Job> jobs);

		void SetMapInfo(MSetInfo mSetInfo);
		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ScreenPannedEventArgs e);

		public void UpdateJob(Job oldJob, Job newJob);
	}
}