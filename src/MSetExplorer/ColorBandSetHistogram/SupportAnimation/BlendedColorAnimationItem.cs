﻿using MSS.Types;
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

		public bool _useDetailedDebug = false;

		public BlendedColorAnimationItem(CbListViewItem? sourceListViewItem, CbListViewItem? destinationListViewItem, double msPerPixel)
		{
			_msPerPixel = msPerPixel;
			RectTransitions = new List<RectTransition>();

			SourceListViewItem = sourceListViewItem;
			DestinationListViewItem = destinationListViewItem;

			if (sourceListViewItem != null)
			{
				Name = sourceListViewItem.Name;

				StartingPos = sourceListViewItem.CbColorBlock.CbColorPair.Container;
				_scaleX = sourceListViewItem.CbColorBlock.ContentScale.Width;
				_colorBandIndex = sourceListViewItem.ColorBandIndex;

				DestinationPos = destinationListViewItem?.CbColorBlock.CbColorPair.Container
					?? GetOffScreenRect(sourceListViewItem);
			}
			else if (destinationListViewItem != null)
			{
				Name = destinationListViewItem.Name;

				StartingPos = sourceListViewItem?.CbColorBlock.CbColorPair.Container
					?? GetOffScreenRect(destinationListViewItem);

				DestinationPos = destinationListViewItem.CbColorBlock.CbColorPair.Container;
				_scaleX = destinationListViewItem.CbColorBlock.ContentScale.Width;
				_colorBandIndex = destinationListViewItem.ColorBandIndex;
			}
			else
			{
				throw new ArgumentNullException("One of the source or destination ListViewItem must be non-null.");
			}

			Current = StartingPos;
			Elasped = 0;
		}

		#region Public Properties

		public string Name { get; init; }
		public CbListViewItem? SourceListViewItem { get; init; }
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

		public void BuildTimelinePos(Rect to, double velocityMultiplier = 1)
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


		public void MoveSourceToDestination()
		{
			if (DestinationListViewItem != null && SourceListViewItem != null)
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
			var result = DestinationPos.Left - StartingPos.Left;

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

		public double GetOriginalShiftDistanceLeft()
		{
			var result = PosBeforeDrop.Left - PosAfterLift.Left;
			return result;
		}

		public double GetOriginalShiftDistanceRight()
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
