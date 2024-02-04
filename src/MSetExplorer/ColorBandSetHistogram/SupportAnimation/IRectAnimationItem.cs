using System.Collections.Generic;
using System.Windows;

namespace MSetExplorer
{
	internal interface IRectAnimationItem
	{
		string Name { get; }

		CbListViewItem? SourceListViewItem { get; init; }
		CbListViewItem? DestinationListViewItem { get; init; }

		Rect StartingPos { get; set; }
		Rect PosAfterLift { get; set; }
		Rect PosBeforeDrop { get; set; }
		Rect DestinationPos { get; init; }

		List<RectTransition> RectTransitions { get; init; }

		Rect Current { get; set; }
		double Elasped { get; set; }

		void BuildTimelineW(double shiftAmount);
		void BuildTimelineW(Rect to);

		void BuildTimelineX(double shiftAmount);
		void BuildTimelinePos(Rect to, double veclocityMultiplier = 1);

		void BuildTimelineXAnchorRight(double shiftAmount);
		
		double GetDistance();
		double GetShiftDistanceLeft();
		double GetShiftDistanceRight();
		
		void MoveSourceToDestination();
	}
}