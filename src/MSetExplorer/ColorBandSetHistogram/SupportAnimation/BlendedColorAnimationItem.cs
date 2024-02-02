using MSS.Types;
using System;
using System.Collections.Generic;
using System.Windows;

namespace MSetExplorer
{
	internal class BlendedColorAnimationItem : IRectAnimationItem
	{
		public double _msPerPixel;

		public BlendedColorAnimationItem(CbListViewItem sourceListViewItem, CbListViewItem? destinationListViewItem, double msPerPixel)
		{
			_msPerPixel = msPerPixel;
			RectTransitions = new List<RectTransition>();


			SourceListViewItem = sourceListViewItem;
			DestinationListViewItem = destinationListViewItem;

			Source = sourceListViewItem.CbRectangle.CbBlendedColorPair.Container;

			Destination = destinationListViewItem?.CbRectangle.CbBlendedColorPair.Container
				?? GetOffScreenRect(sourceListViewItem);

			SourceIsWider = Source.Width > Destination.Width;

			StartingPos = Source;
			Current = StartingPos;
			Elasped = 0;
		}

		#region Public Properties

		public CbListViewItem SourceListViewItem { get; init; }
		public string Name => SourceListViewItem.Name;
		public CbListViewItem? DestinationListViewItem { get; init; }

		public List<RectTransition> RectTransitions { get; init; }

		public Rect Source { get; init; }
		public Rect Destination { get; init; }

		public bool SourceIsWider { get; init; }

		public Rect StartingPos { get; set; }
		public Rect PosAfterLift { get; set; }
		public Rect PosBeforeDrop { get; set; }

		public Rect Current { get; set; }
		public double Elasped { get; set; }

		//public Rect PosAfterShift1 { get; set; }
		//public Rect PosAfterShift2 { get; set; }

		//public Rect PosAfterResize3 { get; set; }

		//public Size Size1 { get; set; }

		//public double ShiftAmount1 { get; set; }
		//public double ShiftAmount2 { get; set; }
		//public double ShiftAmount3 { get; set; }

		//public TimeSpan ShiftDuration1 { get; set; }
		//public TimeSpan ShiftDuration2 { get; set; }
		//public TimeSpan ShiftDuration3 { get; set; }

		//public bool SourceIsWiderThanDest => Source.Width > Destination.Width;

		#endregion

		#region Public Methods

		public void BuildTimelineX(Rect to)
		{
			var dist = Math.Abs(to.X - Current.X);
			var durationMs = dist * _msPerPixel;

			var rt = new RectTransition(Current, to, Elasped, durationMs);

			RectTransitions.Add(rt);
			Current = to;
			Elasped += durationMs;
		}

		public void BuildTimelineW(Rect to)
		{
			var dist = Math.Abs(Current.Width - to.Width);
			var durationMs = dist * _msPerPixel;

			var rt = new RectTransition(Current, to, Elasped, durationMs);

			RectTransitions.Add(rt);
			Current = to;
			Elasped += durationMs;
		}

		public void BuildTimelineX(double shiftAmount)
		{
			var rect = new Rect(Current.X + shiftAmount, Current.Y, Current.Width, Current.Height);
			BuildTimelineX(rect);
		}

		public void BuildTimelineXAnchorRight(double shiftAmount)
		{
			var rect = new Rect(Current.X + shiftAmount, Current.Y, Current.Width - shiftAmount, Current.Height);
			BuildTimelineX(rect);
		}

		public void BuildTimelineW(double shiftAmount)
		{
			var rect = new Rect(Current.X, Current.Y, Current.Width + shiftAmount, Current.Height);
			BuildTimelineW(rect);
		}
		public void MoveSourceToDestination()
		{
			if (DestinationListViewItem != null)
			{
				var newCopy = SourceListViewItem.CbRectangle.CbBlendedColorPair.Clone();

				if (DestinationListViewItem.ColorBand.IsLast)
				{
					newCopy.EndColor = ColorBandColor.Black;
				}

				DestinationListViewItem.CbRectangle.CbBlendedColorPair = newCopy;

				SourceListViewItem.CbRectangle.CbBlendedColorPair.TearDown();
			}
		}

		public double GetDistance()
		{
			var result = Destination.Left - Source.Left;

			return result;
		}

		public double GetShiftDistanceLeft()
		{
			var result = PosBeforeDrop.Left - PosAfterLift.Left;
			return result;
		}

		public double GetShiftDistanceRight()
		{
			var result = PosBeforeDrop.Right - PosAfterLift.Right;
			return result;
		}

		#endregion

		private static Rect GetOffScreenRect(CbListViewItem source)
		{
			// The destination is just off the edge of the visible portion of the canvas.
			var sourceRect = source.CbRectangle.ColorPairContainer;

			var width = source.CbRectangle.Width * source.CbRectangle.ContentScale.Width;
			var destinationPosition = new Point(sourceRect.X + width + 5, sourceRect.Top);

			var destRect = new Rect(destinationPosition, sourceRect.Size);

			return destRect;
		}
	}
}
