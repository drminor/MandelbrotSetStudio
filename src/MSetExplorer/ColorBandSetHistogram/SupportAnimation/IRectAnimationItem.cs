using System.Collections.Generic;
using System.Windows;

namespace MSetExplorer
{
	internal interface IRectAnimationItem
	{
		Rect Current { get; set; }
		Rect Destination { get; init; }
		CbListViewItem? DestinationListViewItem { get; init; }
		double Elasped { get; set; }
		string Name { get; }
		Rect PosAfterLift { get; set; }
		Rect PosBeforeDrop { get; set; }
		List<RectTransition> RectTransitions { get; init; }
		Rect Source { get; init; }
		bool SourceIsWider { get; init; }
		CbListViewItem SourceListViewItem { get; init; }
		Rect StartingPos { get; set; }

		void BuildTimelineW(double shiftAmount);
		void BuildTimelineW(Rect to);
		void BuildTimelineX(double shiftAmount);
		void BuildTimelineX(Rect to);
		void BuildTimelineXAnchorRight(double shiftAmount);
		double GetDistance();
		double GetShiftDistanceLeft();
		double GetShiftDistanceRight();
		void MoveSourceToDestination();
	}
}