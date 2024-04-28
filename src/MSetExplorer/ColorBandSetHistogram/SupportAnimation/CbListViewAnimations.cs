using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Windows.UI.WebUI;

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

		//private Action<ColorBandSetEditOperation, int, ColorBand?, ReservedColorBand?> _onAnimationComplete;
		private Action<ColorBandSetEditArgs> _onAnimationComplete;

		private PushColorsAnimationInfo? _pushColorsAnimationInfo1 = null;
		private PullColorsAnimationInfo? _pullColorsAnimationInfo1 = null;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbListViewAnimations(StoryboardDetails storyboardDetails, CbListView cbListView, Action<ColorBandSetEditArgs> onAnimationComplete)
		{
			_storyBoardDetails1 = storyboardDetails;
			_cbListView = cbListView;
			_onAnimationComplete = onAnimationComplete;
		}

		#endregion

		#region Animation Support - Insertions

		// Insert new ColorBand, Pull Colors Down
		public void InsertCutoff(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;
			var reservedColorBand = editArgs.ReservedColorBand!;

			var currentItem = _listViewItems[index];
			var currentArea = currentItem.Area;
			//var onePix = 1 / currentItem.CbRectangle.ContentScale.Width;

			var colorBand = currentItem.ColorBand;
			var prevCutoff = colorBand.PreviousCutoff;
			var newWidth = colorBand.BucketWidth / 2;
			var newCutoff = (prevCutoff ?? 0) + newWidth;
			var newPercentage = colorBand.Percentage / 2;
			var remainingWidth = colorBand.BucketWidth - newWidth;

			// the existing item's percentage is also halved.
			colorBand.Percentage = newPercentage;

			var newStartColor = colorBand.StartColor;
			var endColor = colorBand.EndColor;
			var successorStartColor = colorBand.SuccessorStartColor;

			var newColorBand = new ColorBand(newCutoff, newStartColor, ColorBandBlendStyle.Next, ColorBandBlendMethod.Rgb, endColor, prevCutoff, successorStartColor, newPercentage);
			editArgs.NewColorBands = new ColorBand[] { newColorBand };

			var itemBeingInserted = _cbListView.CreateListViewItem(index, newColorBand);
			//itemBeingInserted.ElevationsAreLocal = true;
			itemBeingInserted.Opacity = 0;

			_listViewItems.Insert(index, itemBeingInserted);
			_cbListView.Reindex(0);

			var newCutoffD = itemBeingInserted.Area.Right;
			var newWidthD = remainingWidth; // currentArea.Width - (itemBeingInserted.Area.Width + onePix);
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

			_storyBoardDetails1.Begin(InsertCutoffPost, editArgs, debounce: true);
		}

		private void InsertCutoffPost(ColorBandSetEditArgs editArgs)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n CutoffInsertion Animation has completed.");

			var index = editArgs.Index;
			if (_pullColorsAnimationInfo1 == null)
			{
				throw new InvalidOperationException("The PullColorsAnimationInfo1 is null.");
			}

			var newLvi = _pullColorsAnimationInfo1.AnimationItemPairs[^1].Item1.SourceListViewItem;
			_pullColorsAnimationInfo1.MoveSourcesToDestinations();

			newLvi.TearDown();
			_storyBoardDetails1.UnregisterName(newLvi.Name);

			_pullColorsAnimationInfo1 = null;

			var prevCb = _listViewItems[index - 1];

			if (prevCb.ColorBand.BlendStyle == ColorBandBlendStyle.Next)
			{
				var cbListViewItem = _listViewItems[index];
				prevCb.CbColorBlock.EndColor = cbListViewItem.CbColorBlock.StartColor;
				prevCb.CbRectangle.EndColor = cbListViewItem.CbRectangle.StartColor;
			}

			//var lvi = _listViewItems[index];
			//var colorBand = lvi.ColorBand;

			// TODO: Use the reservedColorBand from the ColorBandSetEditArgs
			//var reservedColorBand = new ReservedColorBand(newLvi.ColorBand.StartColor, newLvi.ColorBand.BlendStyle, newLvi.ColorBand.BlendMethod, newLvi.ColorBand.EndColor);

			//editArgs.NewColorBand = colorBand;
			_onAnimationComplete(editArgs);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		// Insert Color, Push Colors Up
		public void InsertColor(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;
			Debug.WriteLineIf(_useDetailedDebug, $"AnimateInsertColor. Index = {index}.");

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
			_storyBoardDetails1.Begin(InsertColorPost, editArgs, debounce: true);
		}

		private void InsertColorPost(ColorBandSetEditArgs editArgs)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ColorInsertion Animation has completed.");

			_pushColorsAnimationInfo1?.MoveSourcesToDestinations();
			_pushColorsAnimationInfo1 = null;

			var colorBand = new ColorBand(0, ColorBandColor.White, ColorBandBlendStyle.Next, ColorBandBlendMethod.Rgb, ColorBandColor.White, percentage: double.NaN);

			editArgs.NewColorBands = new ColorBand[] { colorBand };
			_onAnimationComplete(editArgs);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		// Insert new ColorBand
		public void InsertBand(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;

			var currentItem = _listViewItems[index];
			var startingAreaOfCurrentItem = currentItem.Area;

			var colorBand = currentItem.ColorBand;
			var prevCutoff = colorBand.PreviousCutoff;
			var newWidth = colorBand.BucketWidth / 2;
			var newPercentage = colorBand.Percentage / 2;

			// the existing item's percentage is also halved.
			colorBand.Percentage = newPercentage;

			var newCutoff = (prevCutoff ?? 0) + newWidth;

			var newStartColor = ColorBandColor.White;
			var endColor = colorBand.StartColor;
			var successorStartColor = colorBand.StartColor;

			var newColorBand = new ColorBand(newCutoff, newStartColor, ColorBandBlendStyle.Next, ColorBandBlendMethod.Rgb, endColor, prevCutoff, successorStartColor, newPercentage);
			editArgs.NewColorBands = new ColorBand[] { newColorBand };

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

			// Move the Left side of the existing item so that it starts at the new Cutoff, the width is reduced to keep the right side fixed.
			_storyBoardDetails1.AddChangeLeft(currentItem.Name, "Area", from: startingAreaOfCurrentItem, newX0: newCutoff, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(450));

			var curVal = itemBeingInserted.Area;
			var newScaledWidth = 20 / itemBeingInserted.ScaleX;
			var centerPt = new Point(curVal.X + curVal.Width / 2, curVal.Y + curVal.Height / 2);
			var startingArea = new Rect(centerPt.X - (newScaledWidth / 2), centerPt.Y - 30, newScaledWidth, 20);

			_storyBoardDetails1.AddRectAnimation(itemBeingInserted.Name, "Area", from: startingArea, to: curVal, beginTime: TimeSpan.FromMilliseconds(450), duration: TimeSpan.FromMilliseconds(600));

			// Have the new item go from transparent to fully opaque
			_storyBoardDetails1.AddOpacityAnimation(itemBeingInserted.Name, "Opacity", from: 0.3, to: 1.0, beginTime: TimeSpan.FromMilliseconds(450), duration: TimeSpan.FromMilliseconds(600));

			_storyBoardDetails1.Begin(InsertBandPost, editArgs, debounce: true);
		}

		private void InsertBandPost(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n BandInsertion Animation has completed.");

			var lvi = _listViewItems[index];
			lvi.ElevationsAreLocal = false;
			
			//var colorBand = lvi.ColorBand;
			//editArgs.NewColorBand = colorBand;

			_onAnimationComplete(editArgs);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		#endregion

		#region Animation Support - Deletions

		// Delete Cutoff, Push Colors Up
		public void DeleteCutoff(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;
			Debug.WriteLineIf(_useDetailedDebug, $"AnimateDeleteCutoff. Index = {index}.");

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
				_storyBoardDetails1.AddChangeLeft(newFirstItem.Name, "Area", from: curVal, newX0: newXPosition, beginTime: TimeSpan.FromMilliseconds(startPushSyncPoint), duration: TimeSpan.FromMilliseconds(shiftMs));
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
			_storyBoardDetails1.Begin(DeleteCutoffPost, editArgs, debounce: true);
		}

		private void DeleteCutoffPost(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;

			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n CutoffDeletion Animation has completed.");

			_pushColorsAnimationInfo1?.MoveSourcesToDestinations();
			_pushColorsAnimationInfo1 = null;

			var lvi = _listViewItems[index];

			_cbListView.RemoveListViewItem(lvi);
			_cbListView.Reindex(lvi.ColorBandIndex);

			_onAnimationComplete(editArgs);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		// Delete Color, Pull Colors Down
		public void DeleteColor(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;
			Debug.WriteLineIf(_useDetailedDebug, $"AnimateDeleteColor. Index = {index}.");

			// Create a ListViewItem to hold the new source
			var newSourceColorBand = CreateColorBandFromReservedBand(_listViewItems[^1], editArgs.ReservedColorBand!);
			var newLvi = _cbListView.CreateListViewItem(_listViewItems.Count, newSourceColorBand);

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

			_storyBoardDetails1.Begin(DeleteColorPost, editArgs, debounce: true);
		}

		private void DeleteColorPost(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n ColorDeletion Animation has completed.");

			if (_pullColorsAnimationInfo1 == null)
			{
				throw new InvalidOperationException("The PullColorsAnimationInfo1 is null.");
			}

			var newLvi = _pullColorsAnimationInfo1.AnimationItemPairs[^1].Item1.SourceListViewItem;
			_pullColorsAnimationInfo1.MoveSourcesToDestinations();

			newLvi.TearDown();
			_storyBoardDetails1.UnregisterName(newLvi.Name);

			_pullColorsAnimationInfo1 = null;

			// TODO: Use the reservedColorBand from the ColorBandSetEditArgs
			//var reservedColorBand = new ReservedColorBand(newLvi.ColorBand.StartColor, newLvi.ColorBand.BlendStyle, newLvi.ColorBand.BlendMethod, newLvi.ColorBand.EndColor);

			// Update the model
			_onAnimationComplete(editArgs);

			if (_listViewItems.Count > 1)
			{
				var nextToLast = _listViewItems[^2];

				if (nextToLast.ColorBand.BlendStyle == ColorBandBlendStyle.Next)
				{
					nextToLast.EndColor = _listViewItems[^1].StartColor;
				}
			}

			if (index > 0)
			{
				var prevCb = _listViewItems[index - 1];

				if (prevCb.ColorBand.BlendStyle == ColorBandBlendStyle.Next)
				{
					var cbListViewItem = _listViewItems[index];
					prevCb.EndColor = cbListViewItem.StartColor;
				}
			}

			//_cbListView.ReportColorBands("After Animate Delete Color.");
			//_cbListView.ReportListViewItems("After Animate Delete Color.");
		}

		// Delete Band
		public void DeleteBand(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;
			_storyBoardDetails1.RateFactor = 1;

			var itemBeingRemoved = _listViewItems[index];

			// Have the new item go from fully opaque to 1/3 transparent
			_storyBoardDetails1.AddOpacityAnimation(itemBeingRemoved.Name, "Opacity", from: 1.0, to: 0.3, beginTime: TimeSpan.FromMilliseconds(0), duration: TimeSpan.FromMilliseconds(600));

			itemBeingRemoved.ElevationsAreLocal = true;

			var curVal = itemBeingRemoved.Area;
			var newScaledWidth = 20 / itemBeingRemoved.ScaleX;
			var centerPt = new Point(curVal.X + curVal.Width / 2, curVal.Y + curVal.Height / 2);
			var newArea = new Rect(centerPt.X - (newScaledWidth / 2), centerPt.Y - 30, newScaledWidth, 20);

			_storyBoardDetails1.AddRectAnimation(itemBeingRemoved.Name, "Area", from: curVal, to: newArea, beginTime: TimeSpan.FromMilliseconds(0), duration: TimeSpan.FromMilliseconds(600));

			//if (index == 0)
			//{
			//	var newFirstItem = _listViewItems[index + 1];
			//	curVal = newFirstItem.Area;
			//	var newXPosition = 0;

			//	_storyBoardDetails1.AddChangeLeft(newFirstItem.Name, "Area", from: curVal, newX1: newXPosition, beginTime: TimeSpan.FromMilliseconds(600), duration: TimeSpan.FromMilliseconds(450));
			//}
			//else
			//{
			//	var widthOfItemBeingRemoved = itemBeingRemoved.Area.Width;

			//	var preceedingItem = _listViewItems[index - 1];

			//	if (index < _listViewItems.Count)
			//	{
			//		preceedingItem.ColorBand.SuccessorStartColor = _listViewItems[index].ColorBand.StartColor;
			//	}

			//	curVal = preceedingItem.Area;
			//	var newWidth = curVal.Width + widthOfItemBeingRemoved;

			//	_storyBoardDetails1.AddChangeWidth(preceedingItem.Name, "Area", from: curVal, newWidth: newWidth, beginTime: TimeSpan.FromMilliseconds(600), duration: TimeSpan.FromMilliseconds(450));
			//}

			var followingItem = _listViewItems[index + 1];
			curVal = followingItem.Area;
			var newXPosition = itemBeingRemoved.Area.Left;

			_storyBoardDetails1.AddChangeLeft(followingItem.Name, "Area", from: curVal, newX0: newXPosition, beginTime: TimeSpan.FromMilliseconds(600), duration: TimeSpan.FromMilliseconds(450));

			_storyBoardDetails1.Begin(DeleteBandPost, editArgs, debounce: false);
		}

		private void DeleteBandPost(ColorBandSetEditArgs editArgs)
		{
			var index = editArgs.Index;

			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n BandDeletion Animation has completed.");

			var lvi = _listViewItems[index];

			_cbListView.RemoveListViewItem(lvi);
			_cbListView.Reindex(index);

			_onAnimationComplete(editArgs);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		#endregion

		#region Distribute Color Bands

		public void DistributeColorBands(ColorBandSetEditArgs editArgs/*, HistCutoffsSnapShot histCutoffsSnapShot*/)
		{
			if (editArgs.DistributionExpansionAmount == 0)
			{
				DistributeColorBandsStay(editArgs);
			}
			else if (editArgs.DistributionExpansionAmount > 0)
			{
				DistributeColorBandsExpand(editArgs);
			}
			else
			{
				DistributeColorBandsContract(editArgs);
			}
		}

		private void DistributeColorBandsStay(ColorBandSetEditArgs editArgs)
		{
			var startIndex = editArgs.StartingIndex;
			var endIndex = editArgs.EndingIndex ?? throw new ArgumentException("EditArgs.EndingIndex must have a value.");
			var newColorBandsCount = editArgs.NewColorBandsCount;
			Debug.WriteLine($"AnimateDistributeColorBands-Stay. StartIndex: {startIndex}, EndIndex: {endIndex}, Target Number: {newColorBandsCount}.");

			var startCutoff = _listViewItems[startIndex].ColorBand.PreviousCutoff ?? 0;
			var endCutoff = _listViewItems[endIndex].ColorBand.Cutoff;

			var newCutoffs = GetNewCutoffs(startCutoff, endCutoff, newColorBandsCount, out var newBucketWidths, out var newPreviousCutoffs);
			editArgs.UpdatedCutoffs = newCutoffs;
			editArgs.UpdatedPreviousCutoffs = newPreviousCutoffs;

			_storyBoardDetails1.RateFactor = 1;

			var newCutoffsPtr = 0;

			for (var i = startIndex; i <= endIndex; i++)
			{
				var currentItem = _listViewItems[i];
				var startingAreaOfCurrentItem = currentItem.Area;

				var previousCutoff = newPreviousCutoffs[newCutoffsPtr];
				var bucketWidth = newBucketWidths[newCutoffsPtr];

				// Move the Left side of the existing item so that it starts at the new Cutoff, the width is reduced to keep the right side fixed.
				_storyBoardDetails1.AddShiftHorizontal(currentItem.Name, "Area", from: startingAreaOfCurrentItem, newX0: previousCutoff, newWidth: bucketWidth, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(450));
				newCutoffsPtr++;
			}

			_storyBoardDetails1.Begin(DistributeColorBandsStayPost, editArgs, debounce: true);
		}

		private void DistributeColorBandsStayPost(ColorBandSetEditArgs editArgs)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n AnimateDistributeColorBands-Stay.");
			_onAnimationComplete(editArgs);
		}

		private void DistributeColorBandsExpand(ColorBandSetEditArgs editArgs)
		{
			var startIndex = editArgs.StartingIndex;
			var endIndex = editArgs.EndingIndex;
			var newColorBandsCount = editArgs.NewColorBandsCount;
			Debug.WriteLine($"AnimateDistributeColorBands-Expand. StartIndex: {startIndex}, EndIndex: {endIndex}, Adding {editArgs.DistributionExpansionAmount} ColorBands, Target Number: {newColorBandsCount}.");

			var index = editArgs.Index;

			var currentItem = _listViewItems[index];
			var startingAreaOfCurrentItem = currentItem.Area;

			var colorBand = currentItem.ColorBand;
			var prevCutoff = colorBand.PreviousCutoff;
			var newWidth = colorBand.BucketWidth / 2;
			var newPercentage = colorBand.Percentage / 2;

			// the existing item's percentage is also halved.
			colorBand.Percentage = newPercentage;

			var newCutoff = (prevCutoff ?? 0) + newWidth;

			var newStartColor = ColorBandColor.White;
			var endColor = colorBand.StartColor;
			var successorStartColor = colorBand.StartColor;

			DistributeColorBandsExpandPost(editArgs);

		}

		private void DistributeColorBandsExpandPost(ColorBandSetEditArgs editArgs)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n AnimateDistributeColorBands-Expand.");

			_onAnimationComplete(editArgs);

		}

		private void DistributeColorBandsContract(ColorBandSetEditArgs editArgs)
		{
			var startIndex = editArgs.StartingIndex;
			var endIndex = editArgs.EndingIndex;
			var newColorBandsCount = editArgs.NewColorBandsCount;
			Debug.WriteLine($"AnimateDistributeColorBands-Contract. StartIndex: {startIndex}, EndIndex: {endIndex}, Removing: {-1 * editArgs.DistributionExpansionAmount} ColorBands, Target Number: {newColorBandsCount}.");

			var index = editArgs.Index;



			DistributeColorBandsContractPost(editArgs);

		}

		private void DistributeColorBandsContractPost(ColorBandSetEditArgs editArgs)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n AnimateDistributeColorBands-Contract.");

			_onAnimationComplete(editArgs);

		}



		#endregion

		#region Private Methods

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

		private ColorBand CreateColorBandFromReservedBand(CbListViewItem lastListViewItem, ReservedColorBand reservedColorBand)
		{
			var cb = lastListViewItem.ColorBand;
			var previousCutoff = cb.Cutoff;
			var cutOff = cb.Cutoff + 10;

			var	result = new ColorBand(cutOff, reservedColorBand.StartColor, reservedColorBand.BlendStyle, reservedColorBand.BlendMethod, reservedColorBand.EndColor, previousCutoff: previousCutoff, successorStartColor: null, percentage: double.NaN);

			return result;
		}

		private int[] GetNewCutoffs(int start, int end, int newCount, out int[] newBucketWidths, out int[] previousCutoffs)
		{
			if (newCount < 2) throw new ArgumentException("NewCount must be 2 or greator on call to GetNewCutoffs", nameof(newCount));

			double totalWidth = end - start;

			newBucketWidths = new int[newCount];
			previousCutoffs = new int[newCount];

			var result = new int[newCount];
			var prevCutoff = start;

			for (var i = 0; i < newCount - 1; i++)
			{
				var rawCutoff = start + ((i + 1) * totalWidth / newCount);
				var cutoff = (int)Math.Round(rawCutoff, MidpointRounding.ToEven);

				newBucketWidths[i] = cutoff - prevCutoff;
				previousCutoffs[i] = prevCutoff;

				result[i] = cutoff;
				prevCutoff = cutoff;
			}

			result[^1] = end;
			newBucketWidths[^1] = end - prevCutoff;
			previousCutoffs[^1] = result[^2];

			return result;
		}

		#endregion
	}

}
