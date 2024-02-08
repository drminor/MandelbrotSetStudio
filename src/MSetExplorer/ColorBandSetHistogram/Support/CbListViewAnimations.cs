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

		private Action<ColorBandSetEditOperation, int, object?> _onAnimationComplete;

		private PushColorsAnimationInfo? _pushColorsAnimationInfo1 = null;
		private PullColorsAnimationInfo? _pullColorsAnimationInfo1 = null;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbListViewAnimations(StoryboardDetails storyboardDetails, CbListView cbListView, Action<ColorBandSetEditOperation, int, object?> onAnimationComplete)
		{
			_storyBoardDetails1 = storyboardDetails;
			_cbListView = cbListView;
			_onAnimationComplete = onAnimationComplete;
		}

		#endregion

		#region Animation Support - Insertions

		public void AnimateInsertCutoff(int index)
		{
			//var itemBeingRemoved = _listViewItems[index];

			_storyBoardDetails1.Begin(AnimateInsertCutoffPost, index, debounce: false);
		}

		private void AnimateInsertCutoffPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "CutoffInsertion Animation has completed.");

			var lvi = _listViewItems[index];
			var newCutoff = lvi.CbSectionLine.SectionLineRectangleArea.Right;

			_onAnimationComplete(ColorBandSetEditOperation.InsertCutoff, index, newCutoff);
		}

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

			//_listViewItems[^2].CbRectangle.EndColor = ColorBandColor.Black;
			//_listViewItems[^2].CbColorBlock.EndColor = ColorBandColor.Black;

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

			_onAnimationComplete(ColorBandSetEditOperation.InsertColor, index, colorBand);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		public void AnimateInsertBand(int index)
		{
			var currentItem = _listViewItems[index];
			var startingAreaOfCurrentItem = currentItem.Area;

			var colorBand = currentItem.ColorBand;

			var prevCutoff = colorBand.PreviousCutoff;


			var bandWidth = colorBand.BucketWidth / 2;
			var newCutoff = (prevCutoff ?? 0) + bandWidth;
			var newItem = new ColorBand(newCutoff, ColorBandColor.White, ColorBandBlendStyle.Next, colorBand.StartColor, prevCutoff, colorBand.StartColor, double.NaN);

			var itemBeingInserted = _cbListView.CreateListViewItem(index, newItem);
			itemBeingInserted.Opacity = 0;

			var newArea = itemBeingInserted.Area;
			var newSize = newArea.Size;

			var startSize = new Size(newSize.Width * 0.25, newSize.Height * 0.25);
			var diffSize = new Size(newSize.Width - startSize.Width, newSize.Height - startSize.Height);
			var startVal = new Rect(new Point(newArea.X + diffSize.Width / 2, newArea.Y + diffSize.Height / 2), startSize);

			_listViewItems.Insert(index, itemBeingInserted);

			_cbListView.Reindex(0);

			if (index > 0)
			{ 
				_listViewItems[index - 1].ColorBand.SuccessorStartColor = newItem.StartColor;
			}

			_storyBoardDetails1.RateFactor = 10;

			_storyBoardDetails1.AddMakeOpaque(itemBeingInserted.Name, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(400));

			//_storyBoardDetails1.AddChangeSize(itemBeingInserted.Name, "Area", from: startVal, newSize: newSize, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(300));

			//var widthOfItemBeingInserted = itemBeingInserted.Area.Width;

			var curVal = currentItem.Area;
			//var newWidth = curVal.Width - newSize.Width;

			_storyBoardDetails1.AddChangeLeft(currentItem.Name, "Area", from: startingAreaOfCurrentItem, newX1: newCutoff,  beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(300));

			_storyBoardDetails1.Begin(AnimateInsertBandPost, index, debounce: true);
		}

		private void AnimateInsertBandPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ColorBandInsertion Animation has completed.");

			var lvi = _listViewItems[index];
			var colorBand = lvi.ColorBand;

			_onAnimationComplete(ColorBandSetEditOperation.InsertBand, index, colorBand);

			//_cbListView.Reindex(0);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		#endregion

		#region Animation Support - Deletions

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

			_onAnimationComplete(ColorBandSetEditOperation.DeleteCutoff, index, null);

			_ = _cbListView.SynchronizeCurrentItem();
		}

		public void AnimateDeleteColor(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"AnimateDeleteColor. Index = {index}.");
			//ReportCanvasChildren();
			//ReportColorBands("Top of AnimateDeleteColor", _listViewItems);

			// Create a ListViewItem to hold the new source
			var newColorBand = CreateColorBandFromReservedBand(_listViewItems[^1], reservedColorBand: null);
			var newLvi = _cbListView.CreateListViewItem(_listViewItems.Count, newColorBand);

			// Create the class that will calcuate the 'PullColor' animation details
			_pullColorsAnimationInfo1 = new PullColorsAnimationInfo(LIFT_HEIGHT, ANIMATION_PIXELS_PER_MS);

			for (var i = index; i < _listViewItems.Count; i++)
			{
				var lviDestination = _listViewItems[i];
				var lviSource = i == _listViewItems.Count - 1 ? newLvi : _listViewItems[i + 1];
				_pullColorsAnimationInfo1.Add(lviSource, lviDestination);
			}

			_ = _pullColorsAnimationInfo1.CalculateMovements();

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

			_onAnimationComplete(ColorBandSetEditOperation.DeleteColor, index, null);
		}

		public void AnimateDeleteBand(int index)
		{
			//_storyBoardDetails1.RateFactor = 5;

			var itemBeingRemoved = _listViewItems[index];
			_storyBoardDetails1.AddMakeTransparent(itemBeingRemoved.Name, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(400));

			itemBeingRemoved.ElevationsAreLocal = true;
			var curVal = itemBeingRemoved.Area;
			var newSize = new Size(curVal.Size.Width * 0.25, curVal.Size.Height * 0.25);

			_storyBoardDetails1.AddChangeSize(itemBeingRemoved.Name, "Area", from: curVal, newSize: newSize, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(300));

			if (index == 0)
			{
				var newFirstItem = _listViewItems[index + 1];
				curVal = newFirstItem.Area;
				var newXPosition = 0;

				_storyBoardDetails1.AddChangeLeft(newFirstItem.Name, "Area", from: curVal, newX1: newXPosition, beginTime: TimeSpan.FromMilliseconds(400), duration: TimeSpan.FromMilliseconds(300));
			}
			else
			{
				var widthOfItemBeingRemoved = itemBeingRemoved.Area.Width;

				var preceedingItem = _listViewItems[index - 1];

				if (index < _listViewItems.Count - 1)
				{
					preceedingItem.ColorBand.SuccessorStartColor = _listViewItems[index + 1].ColorBand.StartColor;
				}

				curVal = preceedingItem.Area;
				var newWidth = curVal.Width + widthOfItemBeingRemoved;

				_storyBoardDetails1.AddChangeWidth(preceedingItem.Name, "Area", from: curVal, newWidth: newWidth, beginTime: TimeSpan.FromMilliseconds(400), duration: TimeSpan.FromMilliseconds(300));
			}

			_storyBoardDetails1.Begin(AnimateDeleteBandPost, index, debounce: false);
		}

		private void AnimateDeleteBandPost(int index)
		{
			Debug.WriteLineIf(_useDetailedDebug, "ANIMATION COMPLETED\n BandDeletion Animation has completed.");

			var lvi = _listViewItems[index];

			_cbListView.RemoveListViewItem(lvi);
			_cbListView.Reindex(index);

			_onAnimationComplete(ColorBandSetEditOperation.DeleteBand, index, null);

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

		private ColorBand CreateColorBandFromReservedBand(CbListViewItem lastListViewItem, ReservedColorBand? reservedColorBand)
		{
			ColorBand result;

			var cb = lastListViewItem.ColorBand;

			var previousCutoff = cb.Cutoff;
			var cutOff = cb.Cutoff + 10;

			if (reservedColorBand != null)
			{
				result = new ColorBand(cutOff, reservedColorBand.StartColor, reservedColorBand.BlendStyle, reservedColorBand.EndColor, previousCutoff: previousCutoff, successorStartColor: null, percentage: 0);
			}
			else
			{
				result = new ColorBand()
				{
					Cutoff = cutOff,
					PreviousCutoff = previousCutoff,
					SuccessorStartColor = null,
					Percentage = 0
				};
			}

			return result;
		}

		#endregion
	}

}
