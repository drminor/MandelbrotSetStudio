using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer.Cbs
{
	using AnimationItemPairList = List<(IRectAnimationItem, IRectAnimationItem)>;

	internal class CbListViewAnimations
	{
		#region Private Fields

		private const double LIFT_HEIGHT = 15;

		private const double ANIMATION_PIXELS_PER_MS = 700 / 1000d;     // 700 pixels per second or 0.7 pixels / millisecond

		private StoryboardDetails _storyBoardDetails1;
		private CbListView _cbListView;
		private List<CbListViewItem> _listViewItems => _cbListView.ListViewItems;

		private Func<ColorBandSetEditOperation, int, ColorBand?, ReservedColorBand?, ReservedColorBand?> _onAnimationComplete;

		private PushColorsAnimationInfo? _pushColorsAnimationInfo1 = null;
		private PullColorsAnimationInfo? _pullColorsAnimationInfo1 = null;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbListViewAnimations(StoryboardDetails storyboardDetails, CbListView cbListView, Func<ColorBandSetEditOperation, int, ColorBand?, ReservedColorBand?, ReservedColorBand?> onAnimationComplete)
		{
			_storyBoardDetails1 = storyboardDetails;
			_cbListView = cbListView;
			_onAnimationComplete = onAnimationComplete;
		}

		#endregion

		#region Animation Support - Insertions

		// Insert Cutoff, Pull Colors Down
		public void AnimateInsertCutoff(int index, ReservedColorBand reservedColorBand)
		{
			var currentItem = _listViewItems[index];
			var currentArea = currentItem.Area;

			var colorBand = currentItem.ColorBand;
			var prevCutoff = colorBand.PreviousCutoff;
			var newWidth = colorBand.BucketWidth / 2;
			var newCutoff = (prevCutoff ?? 0) + newWidth;

			var newStartColor = colorBand.StartColor;
			var endColor = colorBand.EndColor;
			var successorStartColor = colorBand.SuccessorStartColor;

			var newColorBand = new ColorBand(newCutoff, newStartColor, ColorBandBlendStyle.Next, endColor, prevCutoff, successorStartColor, double.NaN);

			var itemBeingInserted = _cbListView.CreateListViewItem(index, newColorBand);
			//itemBeingInserted.ElevationsAreLocal = true;
			itemBeingInserted.Opacity = 0;

			_listViewItems.Insert(index, itemBeingInserted);
			_cbListView.Reindex(0);

			var newCutoffD = itemBeingInserted.Area.Right;
			var newWidthD = currentArea.Width - itemBeingInserted.Area.Width;
			currentItem.Area = new Rect(new Point(newCutoff, currentArea.Y), new Size(newWidthD, currentArea.Height));

			_storyBoardDetails1.RateFactor = 1;

			// Have the new item go from transparent to fully opaque
			_storyBoardDetails1.AddOpacityAnimation(itemBeingInserted.Name, "Opacity", from: 0.1, to: 1.0, beginTime: TimeSpan.FromMilliseconds(0), duration: TimeSpan.FromMilliseconds(500));

			// TODO: Make the ChangeLeft animation complete and then call the second round of animations
			//// Move the Left side of the existing item so that it starts at the new Cutoff, the width is reduced to keep the right side fixed.
			//_storyBoardDetails1.AddChangeLeft(currentItem.Name, "Area", from: startingAreaOfCurrentItem, newX1: newCutoff - 1, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(300));
			//_storyBoardDetails1.Begin();
			//_storyBoardDetails1.RateFactor = 10;

			// Pull Colors Down
			// Create a ListViewItem to hold the new source
			var newSourceColorBand = CreateColorBandFromReservedBand(_listViewItems[^1], reservedColorBand);
			var newLvi = _cbListView.CreateListViewItem(_listViewItems.Count, newSourceColorBand);

			// Create the class that will calcuate the 'PullColor' animation details
			_pullColorsAnimationInfo1 = new PullColorsAnimationInfo(LIFT_HEIGHT, ANIMATION_PIXELS_PER_MS);

			// The first destination is the upper half, which is at index + 1
			for (var i = index; i < _listViewItems.Count; i++)
			{
				var lviDestination = _listViewItems[i];
				var lviSource = i == _listViewItems.Count - 1 ? newLvi : _listViewItems[i + 1];
				_pullColorsAnimationInfo1.Add(lviSource, lviDestination);
			}

			_ = _pullColorsAnimationInfo1.CalculateMovements(beginMs:400);

			ApplyAnimationItemPairs(_pullColorsAnimationInfo1.AnimationItemPairs);

			_storyBoardDetails1.Begin(AnimateInsertCutoffPost, index, debounce: true);
		}

		private void AnimateInsertCutoffPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n CutoffInsertion Animation has completed.");

			if (_pullColorsAnimationInfo1 != null)
			{
				var newLvi = _pullColorsAnimationInfo1.AnimationItemPairs[^1].Item1.SourceListViewItem;
				_pullColorsAnimationInfo1.MoveSourcesToDestinations();

				newLvi.TearDown();
				_storyBoardDetails1.UnregisterName(newLvi.Name);

				_pullColorsAnimationInfo1 = null;
			}

			var prevCb = _listViewItems[index - 1];

			if (prevCb.ColorBand.BlendStyle == ColorBandBlendStyle.Next)
			{
				var cbListViewItem = _listViewItems[index];
				prevCb.CbColorBlock.EndColor = cbListViewItem.CbColorBlock.StartColor;
				prevCb.CbRectangle.EndColor = cbListViewItem.CbRectangle.StartColor;
			}

			var lvi = _listViewItems[index];
			var colorBand = lvi.ColorBand;

			var reservedColorBand = new ReservedColorBand();

			_onAnimationComplete(ColorBandSetEditOperation.InsertCutoff, index, colorBand, reservedColorBand);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		// Insert Color, Push Colors Up
		public void AnimateInsertColor(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"AnimateInsertColor. Index = {index}.");
			//ReportCanvasChildren();
			//ReportColorBands("Top of AnimateDeleteCutoff", _listViewItems);

			// Create the class that will calcuate the 'PushColor' animation details
			_pushColorsAnimationInfo1 = new PushColorsAnimationInfo(LIFT_HEIGHT, ANIMATION_PIXELS_PER_MS);

			for (var i = index; i < _listViewItems.Count; i++)
			{
				var lviSource = _listViewItems[i];
				var lviDestination = i == _listViewItems.Count - 1 ? null : _listViewItems[i + 1];
				_pushColorsAnimationInfo1.Add(lviSource, lviDestination);
			}

			_pushColorsAnimationInfo1.CalculateMovements();

			_storyBoardDetails1.RateFactor = 1;

			ApplyAnimationItemPairs(_pushColorsAnimationInfo1.AnimationItemPairs);

			// Execute the Animation
			_storyBoardDetails1.Begin(AnimateInsertColorPost, index, debounce: true);
		}

		private void AnimateInsertColorPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ColorInsertion Animation has completed.");

			_pushColorsAnimationInfo1?.MoveSourcesToDestinations();
			_pushColorsAnimationInfo1 = null;

			var lvi = _listViewItems[index];
			var colorBand = lvi.ColorBand;

			colorBand.SuccessorStartColor = colorBand.StartColor;

			colorBand.StartColor = ColorBandColor.White;
			colorBand.BlendStyle = ColorBandBlendStyle.Next;
			colorBand.EndColor = ColorBandColor.White;

			_onAnimationComplete(ColorBandSetEditOperation.InsertColor, index, colorBand, null);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		public void AnimateInsertBand(int index)
		{
			var currentItem = _listViewItems[index];
			var startingAreaOfCurrentItem = currentItem.Area;

			var colorBand = currentItem.ColorBand;
			var prevCutoff = colorBand.PreviousCutoff;
			var newWidth = colorBand.BucketWidth / 2;
			var newCutoff = (prevCutoff ?? 0) + newWidth;

			var newStartColor = ColorBandColor.White;
			var endColor = colorBand.StartColor;
			var successorStartColor = colorBand.StartColor;

			var newColorBand = new ColorBand(newCutoff, newStartColor, ColorBandBlendStyle.Next, endColor, prevCutoff, successorStartColor, double.NaN);

			var itemBeingInserted = _cbListView.CreateListViewItem(index, newColorBand);
			itemBeingInserted.ElevationsAreLocal = true;
			itemBeingInserted.Opacity = 0;

			_listViewItems.Insert(index, itemBeingInserted);
			_cbListView.Reindex(0);

			if (index > 0)
			{ 
				_listViewItems[index - 1].ColorBand.SuccessorStartColor = newColorBand.StartColor;
			}

			_storyBoardDetails1.RateFactor = 1;

			var curVal = itemBeingInserted.Area;
			var newScaledWidth = 20 / itemBeingInserted.ScaleX;
			var centerPt = new Point(curVal.X + curVal.Width / 2, curVal.Y + curVal.Height / 2);
			var startingArea = new Rect(centerPt.X - (newScaledWidth / 2), centerPt.Y - 30, newScaledWidth, 20);

			_storyBoardDetails1.AddRectAnimation(itemBeingInserted.Name, "Area", from: startingArea, to: curVal, beginTime: TimeSpan.FromMilliseconds(0), duration: TimeSpan.FromMilliseconds(250));

			// Have the new item go from transparent to fully opaque
			_storyBoardDetails1.AddOpacityAnimation(itemBeingInserted.Name, "Opacity", from: 0.3, to: 1.0, beginTime: TimeSpan.FromMilliseconds(0), duration: TimeSpan.FromMilliseconds(200));

			// Move the Left side of the existing item so that it starts at the new Cutoff, the width is reduced to keep the right side fixed.
			_storyBoardDetails1.AddChangeLeft(currentItem.Name, "Area", from: startingAreaOfCurrentItem, newX1: newCutoff,  beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(150));

			_storyBoardDetails1.Begin(AnimateInsertBandPost, index, debounce: true);
		}

		private void AnimateInsertBandPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n BandInsertion Animation has completed.");

			var lvi = _listViewItems[index];
			lvi.ElevationsAreLocal = false;
			var colorBand = lvi.ColorBand;

			_onAnimationComplete(ColorBandSetEditOperation.InsertBand, index, colorBand, null);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		#endregion

		#region Animation Support - Deletions

		// Delete Cutoff, Push Colors Up
		public void AnimateDeleteCutoff(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"AnimateDeleteCutoff. Index = {index}.");
			//ReportCanvasChildren();
			//ReportColorBands("Top of AnimateDeleteCutoff", _listViewItems);

			// Create the class that will calcuate the 'PushColor' animation details
			_pushColorsAnimationInfo1 = new PushColorsAnimationInfo(LIFT_HEIGHT, ANIMATION_PIXELS_PER_MS);

			for (var i = index; i < _listViewItems.Count; i++)
			{
				var lviSource = _listViewItems[i];
				var lviDestination = i == _listViewItems.Count - 1 ? null : _listViewItems[i + 1];
				_pushColorsAnimationInfo1.Add(lviSource, lviDestination);
			}

			var startPushSyncPoint = _pushColorsAnimationInfo1.CalculateMovements();
			var endPushSyncPoint = _pushColorsAnimationInfo1.GetMaxDuration();
			var shiftMs = endPushSyncPoint - startPushSyncPoint;

			_storyBoardDetails1.RateFactor = 1;

			ApplyAnimationItemPairs(_pushColorsAnimationInfo1.AnimationItemPairs);

			if (index == 0)
			{
				// Pull the left edge of the first band so that it starts at Zero.
				var newFirstItem = _listViewItems[index + 1];
				var curVal = newFirstItem.Area;
				var newXPosition = 0;
				_storyBoardDetails1.AddChangeLeft(newFirstItem.Name, "Area", from: curVal, newX1: newXPosition, beginTime: TimeSpan.FromMilliseconds(startPushSyncPoint), duration: TimeSpan.FromMilliseconds(shiftMs));
			}
			else
			{
				// Widen the band immediately before the band being deleted to take up the available room.
				var itemBeingRemoved = _listViewItems[index];
				var widthOfItemBeingRemoved = itemBeingRemoved.Area.Width;
				var preceedingItem = _listViewItems[index - 1];

				var curVal = preceedingItem.Area;
				var newWidth = curVal.Width + widthOfItemBeingRemoved;
				_storyBoardDetails1.AddChangeWidth(preceedingItem.Name, "Area", from: curVal, newWidth: newWidth, beginTime: TimeSpan.FromMilliseconds(startPushSyncPoint), duration: TimeSpan.FromMilliseconds(shiftMs));
			}

			_listViewItems[^2].CbRectangle.EndColor = ColorBandColor.Black;
			_listViewItems[^2].CbColorBlock.EndColor = ColorBandColor.Black;

			// Execute the Animation
			_storyBoardDetails1.Begin(AnimateDeleteCutoffPost, index, debounce: true);
		}

		private void AnimateDeleteCutoffPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n CutoffDeletion Animation has completed.");

			_pushColorsAnimationInfo1?.MoveSourcesToDestinations();
			_pushColorsAnimationInfo1 = null;

			var lvi = _listViewItems[index];

			_cbListView.RemoveListViewItem(lvi);
			_cbListView.Reindex(lvi.ColorBandIndex);

			_onAnimationComplete(ColorBandSetEditOperation.DeleteCutoff, index, null, null);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		// Delete Color, Pull Colors Down
		public void AnimateDeleteColor(int index, ReservedColorBand reservedColorBand)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"AnimateDeleteColor. Index = {index}.");
			//ReportCanvasChildren();
			//ReportColorBands("Top of AnimateDeleteColor", _listViewItems);

			// Create a ListViewItem to hold the new source
			var newSourceColorBand = CreateColorBandFromReservedBand(_listViewItems[^1], reservedColorBand);
			var newLvi = _cbListView.CreateListViewItem(_listViewItems.Count, newSourceColorBand);

			// Create the class that will calcuate the 'PullColor' animation details
			_pullColorsAnimationInfo1 = new PullColorsAnimationInfo(LIFT_HEIGHT, ANIMATION_PIXELS_PER_MS);

			for (var i = index; i < _listViewItems.Count; i++)
			{
				var lviDestination = _listViewItems[i];
				var lviSource = i == _listViewItems.Count - 1 ? newLvi : _listViewItems[i + 1];
				_pullColorsAnimationInfo1.Add(lviSource, lviDestination);
			}

			_ = _pullColorsAnimationInfo1.CalculateMovements(beginMs: 0);

			_storyBoardDetails1.RateFactor = 1;

			ApplyAnimationItemPairs(_pullColorsAnimationInfo1.AnimationItemPairs);

			_storyBoardDetails1.Begin(AnimateDeleteColorPost, index, debounce: true);
		}

		private void AnimateDeleteColorPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n ColorDeletion Animation has completed.");

			if (_pullColorsAnimationInfo1 != null)
			{
				var newLvi = _pullColorsAnimationInfo1.AnimationItemPairs[^1].Item1.SourceListViewItem;
				_pullColorsAnimationInfo1.MoveSourcesToDestinations();

				newLvi.TearDown();
				_storyBoardDetails1.UnregisterName(newLvi.Name);

				_pullColorsAnimationInfo1 = null;
			}

			if (index > 0)
			{
				var prevCb = _listViewItems[index - 1];

				if (prevCb.ColorBand.BlendStyle == ColorBandBlendStyle.Next)
				{
					var cbListViewItem = _listViewItems[index];
					prevCb.CbColorBlock.EndColor = cbListViewItem.CbColorBlock.StartColor;
					prevCb.CbRectangle.EndColor = cbListViewItem.CbRectangle.StartColor;
				}
			}

			var reservedColorBand = new ReservedColorBand();

			_onAnimationComplete(ColorBandSetEditOperation.DeleteColor, index, null, reservedColorBand);
		}

		// Delete Band
		public void AnimateDeleteBand(int index)
		{
			_storyBoardDetails1.RateFactor = 1; //25

			var itemBeingRemoved = _listViewItems[index];

			// Have the new item go from fully opaque to 1/3 transparent
			_storyBoardDetails1.AddOpacityAnimation(itemBeingRemoved.Name, "Opacity", from: 1.0, to: 0.3, beginTime: TimeSpan.FromMilliseconds(0), duration: TimeSpan.FromMilliseconds(200));

			itemBeingRemoved.ElevationsAreLocal = true;

			var curVal = itemBeingRemoved.Area;
			var newScaledWidth = 20 / itemBeingRemoved.ScaleX;
			var centerPt = new Point(curVal.X + curVal.Width / 2, curVal.Y + curVal.Height / 2);
			var newArea = new Rect(centerPt.X - (newScaledWidth / 2), centerPt.Y - 30, newScaledWidth, 20);

			_storyBoardDetails1.AddRectAnimation(itemBeingRemoved.Name, "Area", from: curVal, to: newArea, beginTime: TimeSpan.FromMilliseconds(0), duration: TimeSpan.FromMilliseconds(200));

			if (index == 0)
			{
				var newFirstItem = _listViewItems[index + 1];
				curVal = newFirstItem.Area;
				var newXPosition = 0;

				_storyBoardDetails1.AddChangeLeft(newFirstItem.Name, "Area", from: curVal, newX1: newXPosition, beginTime: TimeSpan.FromMilliseconds(150), duration: TimeSpan.FromMilliseconds(200));
			}
			else
			{
				var widthOfItemBeingRemoved = itemBeingRemoved.Area.Width;

				var preceedingItem = _listViewItems[index - 1];

				if (index < _listViewItems.Count)
				{
					preceedingItem.ColorBand.SuccessorStartColor = _listViewItems[index].ColorBand.StartColor;
				}

				curVal = preceedingItem.Area;
				var newWidth = curVal.Width + widthOfItemBeingRemoved;

				_storyBoardDetails1.AddChangeWidth(preceedingItem.Name, "Area", from: curVal, newWidth: newWidth, beginTime: TimeSpan.FromMilliseconds(200), duration: TimeSpan.FromMilliseconds(150));
			}

			_storyBoardDetails1.Begin(AnimateDeleteBandPost, index, debounce: false);
		}

		private void AnimateDeleteBandPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n BandDeletion Animation has completed.");

			var lvi = _listViewItems[index];

			_cbListView.RemoveListViewItem(lvi);
			_cbListView.Reindex(index);

			_onAnimationComplete(ColorBandSetEditOperation.DeleteBand, index, null, null);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		private void ApplyAnimationItemPairs(AnimationItemPairList animationItemPairList)
		{
			foreach (var (block, blend) in animationItemPairList)
			{
				foreach (var tl in block.RectTransitions)
				{
					_storyBoardDetails1.AddRectAnimation(block.Name, "ColorBlockArea", tl.From, tl.To, TimeSpan.FromMilliseconds(tl.BeginMs), TimeSpan.FromMilliseconds(tl.DurationMs));
				}

				foreach (var tl in blend.RectTransitions)
				{
					_storyBoardDetails1.AddRectAnimation(blend.Name, "BlendedColorArea", tl.From, tl.To, TimeSpan.FromMilliseconds(tl.BeginMs), TimeSpan.FromMilliseconds(tl.DurationMs));
				}
			}
		}

		//private ColorBand CreateColorBandFromReservedBand(CbListViewItem lastListViewItem, ReservedColorBand? reservedColorBand)
		//{
		//	ColorBand result;

		//	var cb = lastListViewItem.ColorBand;

		//	var previousCutoff = cb.Cutoff;
		//	var cutOff = cb.Cutoff + 10;

		//	if (reservedColorBand != null)
		//	{
		//		result = new ColorBand(cutOff, reservedColorBand.StartColor, reservedColorBand.BlendStyle, reservedColorBand.EndColor, previousCutoff: previousCutoff, successorStartColor: null, percentage: double.NaN);
		//	}
		//	else
		//	{
		//		var startColor = ColorBandColor.White;
		//		var blendStyle = ColorBandBlendStyle.Next;
		//		var endColor = ColorBandColor.White;

		//		result = new ColorBand(cutOff, startColor, blendStyle, endColor, previousCutoff, successorStartColor: null, percentage: double.NaN);
		//	}

		//	return result;
		//}


		private ColorBand CreateColorBandFromReservedBand(CbListViewItem lastListViewItem, ReservedColorBand reservedColorBand)
		{
			var cb = lastListViewItem.ColorBand;
			var previousCutoff = cb.Cutoff;
			var cutOff = cb.Cutoff + 10;

			var	result = new ColorBand(cutOff, reservedColorBand.StartColor, reservedColorBand.BlendStyle, reservedColorBand.EndColor, previousCutoff: previousCutoff, successorStartColor: null, percentage: double.NaN);

			return result;
		}

		#endregion
	}

}
