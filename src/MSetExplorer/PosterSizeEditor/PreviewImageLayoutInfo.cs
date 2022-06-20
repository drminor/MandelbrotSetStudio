using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	public class PreviewImageLayoutInfo
	{
		public PreviewImageLayoutInfo(SizeDbl imageSize, SizeDbl previewImageSize, SizeDbl containerSize)
		{
			OriginalMapSize = imageSize;
			PreviewImageSize = previewImageSize;
			ContainerSize = containerSize;

			NewMapArea = new RectangleDbl(new PointDbl(), OriginalMapSize);
		}

		#region Public Properties

		// Convenience Properties
		// These are Positive for a new image larger than the original.
		public double BeforeX => NewImageArea.X1 - OriginalImageArea.X1;
		public double AfterX => NewImageArea.X2 - OriginalImageArea.X2;
		public double BeforeY => NewImageArea.Y1 - OriginalImageArea.Y1;
		public double AfterY => NewImageArea.Y2 - OriginalImageArea.Y2;

		// Environment
		public SizeDbl OriginalMapSize { get; init; }
		public SizeDbl PreviewImageSize { get; init; }

		// Inputs
		public SizeDbl ContainerSize { get; set; }
		public RectangleDbl NewMapArea { get; set; }

		// Outputs
		public RectangleDbl OriginalImageArea { get; private set; } // Size and placement of the preview image, relative to the NewImageArea
		public RectangleDbl NewImageArea { get; private set; } // Size and placement of a rectangle that encloses the OriginalImageArea, relative to the container  
		public double ScaleFactorForPreviewImage { get; private set; }

		//public double ScaleFactorForBoundingArea { get; private set; }
		public RectangleDbl PreviewImageClipRegion { get; private set; }
		public RectangleDbl PreviewImageClipRegionYInverted { get; private set; }

		#endregion

		#region Public Methods

		public void Update()
		{
			//var placementMode = ((int)Math.Round(NewMapArea.X1)) % 4;
			Debug.WriteLine($"Edit Poster Size Layout Update: NewMapArea: {NewMapArea}, ContainerSize: {ContainerSize}.");

			//var originalToNewScaleFactor = RMapHelper.GetSmallestScaleFactor(OriginalMapSize, NewMapArea.Size);
			//var adjOriginalMapSize = OriginalMapSize.Scale(originalToNewScaleFactor);

			// Create a rectangle that encloses both the new and original maps
			var originalMapArea = new RectangleDbl(new PointDbl(), OriginalMapSize);

			var boundingMapArea = RMapHelper.GetBoundingRectangle(originalMapArea, NewMapArea);

			// Determine Size and Location of bounding rectangle
			var scaleFactor = RMapHelper.GetSmallestScaleFactor(boundingMapArea.Size, ContainerSize);
			var boundingImageSize = boundingMapArea.Size.Scale(scaleFactor);
			var boundingImageArea = boundingImageSize.PlaceAtCenter(ContainerSize); // This centers the BoundingImageArea on the Control.
			var boundingImagePos = boundingImageArea.Position;

			// Determine Size and Location of the original image.
			var originalImageSize = OriginalMapSize.Scale(scaleFactor);
			var newImageSize = NewMapArea.Size.Scale(scaleFactor);
			var newImagePos = NewMapArea.Position.Scale(scaleFactor);

			// Determine Location of the original and new images
			double oPosX;
			double oPosY;

			double nPosX;
			double nPosY;

			if (newImagePos.X <= 0)
			{
				oPosX = boundingImagePos.X;
				nPosX = boundingImagePos.X - newImagePos.X;
			}
			else
			{
				oPosX = boundingImagePos.X + newImagePos.X;
				nPosX = boundingImagePos.X;
			}

			if (newImagePos.Y <= 0)
			{
				oPosY = boundingImagePos.Y;
				nPosY = boundingImagePos.Y - newImagePos.Y;
			}
			else
			{
				oPosY = boundingImagePos.Y + newImagePos.Y;
				nPosY = boundingImagePos.Y;
			}
			
			var newImagePosTr = new PointDbl(nPosX, nPosY);
			var originalImagePosTr = new PointDbl(oPosX, oPosY);

			OriginalImageArea = new RectangleDbl(originalImagePosTr, originalImageSize);
			NewImageArea = new RectangleDbl(newImagePosTr, newImageSize);

			// Get the scale factor needed to reduce the actual preview image's bitmap to the container
			var previewImageScaleFactor = RMapHelper.GetSmallestScaleFactor(PreviewImageSize, originalImageSize);

			ScaleFactorForPreviewImage = double.IsInfinity(previewImageScaleFactor) ? 1 : double.IsNaN(previewImageScaleFactor) ? 1 : previewImageScaleFactor;

			var rawPreviewImageClipRegion = GetOriginalImageClipRegion();
			PreviewImageClipRegion = rawPreviewImageClipRegion.Translate(OriginalImageArea.Position);
			PreviewImageClipRegionYInverted = ScreenTypeHelper.FlipY(rawPreviewImageClipRegion, OriginalImageArea.Height);

			Debug.WriteLine($"BoundingSize: {boundingImageSize}, NewSize: {newImageSize}, OriginalSize: {originalImageSize}.");
			Debug.WriteLine($"BoundingPos: {boundingImagePos}, NewPos: {newImagePosTr}, OrigPos: {originalImagePosTr}");
			//Debug.WriteLine($"ScaleFactors:: OrigToNew: {originalToNewScaleFactor}, BoundingArea: {scaleFactor}, PreviewImage: {previewImageScaleFactor}.");
		}

		private RectangleDbl GetOriginalImageClipRegion()
		{
			var newImageArea = NewImageArea;
			var originalImageArea = OriginalImageArea;

			var X1 = Math.Max(newImageArea.X1 - originalImageArea.X1, 0);

			var X2 = originalImageArea.Width - Math.Max(originalImageArea.X2 - newImageArea.X2, 0);

			var Y1 = Math.Max(newImageArea.Y1 - originalImageArea.Y1, 0);

			var Y2 = originalImageArea.Height - Math.Max(originalImageArea.Y2 - newImageArea.Y2, 0);

			var clip = new RectangleDbl(X1, X2, Y1, Y2);

			return clip;
		}

		#endregion

	}
}
