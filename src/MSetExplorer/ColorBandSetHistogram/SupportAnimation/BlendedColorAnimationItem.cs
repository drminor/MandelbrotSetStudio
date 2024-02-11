using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	internal class BlendedColorAnimationItem : IRectAnimationItem
	{
		public double _msPerPixel;
		private double _scaleX;
		private int _colorBandIndex;
		private bool _isForPullColors;

		public bool _useDetailedDebug = false;

		public BlendedColorAnimationItem(CbListViewItem sourceListViewItem, CbListViewItem? destinationListViewItem, double msPerPixel, bool isForPullColors)
		{
			_isForPullColors = isForPullColors;
			_msPerPixel = msPerPixel;
			RectTransitions = new List<RectTransition>();

			SourceListViewItem = sourceListViewItem;
			DestinationListViewItem = destinationListViewItem;

			Name = sourceListViewItem.Name;
			_scaleX = sourceListViewItem.CbRectangle.ContentScale.Width;
			_colorBandIndex = sourceListViewItem.ColorBandIndex;

			StartingPos = sourceListViewItem.CbRectangle.CbBlendedColorPair.Container;

			DestinationPos = destinationListViewItem?.CbRectangle.CbBlendedColorPair.Container
				?? GetOffScreenRect(sourceListViewItem);

			Current = StartingPos;
			Elasped = 0;
		}

		#region Public Properties

		public string Name { get; init; }
		public CbListViewItem SourceListViewItem { get; init; }
		public CbListViewItem? DestinationListViewItem { get; init; }

		public List<RectTransition> RectTransitions { get; init; }

		public Rect StartingPos { get; set; }

		public Rect PosAfterLift { get; set; }
		public Rect PosBeforeDrop { get; set; }
		public Rect DestinationPos { get; init; }

		public Rect Current { get; set; }
		public double Elasped { get; set; }

		#endregion

		#region Public Methods

		public void MoveSourceToDestination()
		{
			if (DestinationListViewItem == null) throw new ArgumentNullException(nameof(DestinationListViewItem));
			if (SourceListViewItem == null) throw new ArgumentNullException(nameof(SourceListViewItem));

			var newCopy = SourceListViewItem.CbRectangle.CbBlendedColorPair.Clone();

			DestinationListViewItem.CbRectangle.CbBlendedColorPair = newCopy;

			if (_isForPullColors)
			{
				if (SourceListViewItem.IsLast)
				{
					DestinationListViewItem.ColorBand.IsLast = false;
					newCopy.EndColor = SourceListViewItem.ColorBand.ActualEndColor;
				}
			}
			else
			{
				if (DestinationListViewItem.IsLast)
				{
					newCopy.EndColor = ColorBandColor.Black;
				}
			}

			SourceListViewItem.CbRectangle.CbBlendedColorPair.TearDown();
		}

		public void BuildTimelinePos(Rect to, double velocityMultiplier = 1)
		{
			var dist = Math.Abs(to.X - Current.X);
			var durationMs = dist * _msPerPixel / velocityMultiplier;

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
			var amount = shiftAmount / _scaleX;
			if (shiftAmount > 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Moving Item: {_colorBandIndex} right {amount}.");
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, "Moving Item: {_colorBandIndex} left {-1 * amount}.");
			}

			var rect = new Rect(Current.X + shiftAmount, Current.Y, Current.Width, Current.Height);
			BuildTimelinePos(rect);
		}

		public void BuildTimelineXAnchorRight(double shiftAmount)
		{
			var amount = shiftAmount / _scaleX;
			if (shiftAmount > 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Moving Item: {_colorBandIndex} right and reducing its width by {amount}.");
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Moving Item: {_colorBandIndex} left and increasing its width by {-1 * amount}.");
			}

			var rect = new Rect(Current.X + shiftAmount, Current.Y, Current.Width - shiftAmount, Current.Height);
			BuildTimelinePos(rect);
		}

		public void BuildTimelineW(double shiftAmount)
		{
			var amount = shiftAmount / _scaleX;
			if (shiftAmount > 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Increasing the Width of Item: {_colorBandIndex} by {amount}.");
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Decreasing the Width of Item: {_colorBandIndex} by {-1 * amount}.");
			}

			var rect = new Rect(Current.X, Current.Y, Current.Width + shiftAmount, Current.Height);
			BuildTimelineW(rect);
		}

		public double GetDistance()
		{
			var result = Math.Abs(DestinationPos.Left - StartingPos.Left);

			return result;
		}

		public double GetShiftDistanceLeft()
		{
			var result = PosBeforeDrop.Left - Current.Left;
			return result;
		}

		public double GetShiftDistanceRight()
		{
			var result = PosBeforeDrop.Right - Current.Right;
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
