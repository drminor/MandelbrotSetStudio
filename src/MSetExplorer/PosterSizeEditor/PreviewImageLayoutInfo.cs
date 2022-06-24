using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	public class PreviewImageLayoutInfo
	{
		public PreviewImageLayoutInfo()
		{ }

		public PreviewImageLayoutInfo(SizeInt imageSize, SizeDbl previewImageSize, SizeDbl containerSize)
		{
			OriginalMapSize = new SizeDbl(imageSize);
			PreviewImageSize = previewImageSize;
			ContainerSize = containerSize;

			//NewMapArea = new RectangleDbl(new PointDbl(), OriginalMapSize);
		}

		#region Public Properties

		//// Convenience Properties
		//// These are Positive for a new image larger than the original.
		//public double BeforeX => NewImageArea.X1 - OriginalImageArea.X1;
		//public double AfterX => NewImageArea.X2 - OriginalImageArea.X2;
		//public double BeforeY => NewImageArea.Y1 - OriginalImageArea.Y1;
		//public double AfterY => NewImageArea.Y2 - OriginalImageArea.Y2;

		public bool IsEmpty => OriginalMapSize.Width == 0;

		// Environment
		public SizeDbl OriginalMapSize { get; init; }
		public SizeDbl PreviewImageSize { get; set; }

		// Inputs
		public SizeDbl ContainerSize { get; set; }
		public RectangleDbl NewMapArea { get; set; }

		// Outputs
		public RectangleDbl OriginalImageArea { get; private set; } // Size and placement of the preview image, relative to the NewImageArea
		public RectangleDbl NewImageArea { get; private set; } // Size and placement of a rectangle that encloses the OriginalImageArea, relative to the container  
		public double ScaleFactorForPreviewImage { get; private set; }

		public RectangleDbl PreviewImageClipRegion { get; private set; }
		public RectangleDbl PreviewImageClipRegionYInverted { get; private set; }

		#endregion

		#region Public Methods

		public void Update()
		{
			Debug.WriteLine($"Edit Poster Size Layout Update: NewMapArea: {NewMapArea}, ContainerSize: {ContainerSize}.");

			// Get the portion of the originalMapArea that will be part of the new Image.
			var originalMapArea = new RectangleDbl(new PointDbl(), OriginalMapSize);
			var clippedOriginalMapArea = GetOriginalImageClipRegion(NewMapArea, originalMapArea);

			// Get a rectangle that will hold both the new and the portion of the original
			var boundingMapArea = RMapHelper.GetBoundingRectangle(clippedOriginalMapArea, NewMapArea);

			// Scale Factor to convert from Map to Screen sizes / positions
			var scaleFactor = RMapHelper.GetSmallestScaleFactor(boundingMapArea.Size, ContainerSize);
			scaleFactor = Math.Max(scaleFactor, 0.001);

			// Determine Size and Location of bounding rectangle
			var boundingImageSize = boundingMapArea.Size.Scale(scaleFactor);
			var boundingImageArea = boundingImageSize.PlaceAtCenter(ContainerSize); // This centers the BoundingImageArea on the Control.

			// Determine Size and Location of the original and new images
			var originalImagePos = clippedOriginalMapArea.Position.Scale(scaleFactor);
			var newImagePos = NewMapArea.Position.Scale(scaleFactor);

			TranslateNewAndOrigImages(boundingImageArea.Position, ref originalImagePos, ref newImagePos);

			var originalImageSize = OriginalMapSize.Scale(scaleFactor);
			OriginalImageArea = ScreenTypeHelper.MakeSafe(new RectangleDbl(originalImagePos, originalImageSize));

			var newImageSize = NewMapArea.Size.Scale(scaleFactor);
			NewImageArea = ScreenTypeHelper.MakeSafe(new RectangleDbl(newImagePos, newImageSize));

			// Get the scale factor needed to reduce the actual preview image's bitmap to the container
			var previewImageScaleFactor = RMapHelper.GetSmallestScaleFactor(PreviewImageSize, originalImageSize);
			ScaleFactorForPreviewImage = double.IsInfinity(previewImageScaleFactor) || double.IsNaN(previewImageScaleFactor) ? 1 : previewImageScaleFactor;

			// Clip the Original Image
			var rawPreviewImageClipRegion = ScreenTypeHelper.MakeSafe(clippedOriginalMapArea.Scale(scaleFactor));
			PreviewImageClipRegion = rawPreviewImageClipRegion.Translate(OriginalImageArea.Position);
			PreviewImageClipRegionYInverted = ScreenTypeHelper.FlipY(rawPreviewImageClipRegion, OriginalImageArea.Height);

			// Diagnostics
			Debug.WriteLine($"BoundingSize: {boundingImageArea.Size.ToString("F2")}, NewSize: {newImageSize.ToString("F2")}, OriginalSize: {originalImageSize.ToString("F2")}.");
			Debug.WriteLine($"BoundingPos: {boundingImageArea.Position.ToString("F2")}, NewPos: {newImagePos.ToString("F2")}, OrigPos: {originalImagePos.ToString("F2")}");
			Debug.WriteLine($"ClipSize: {rawPreviewImageClipRegion.Size.ToString("F2")}, Tr.ClipSize: {PreviewImageClipRegion.Size.ToString("F2")}");
			Debug.WriteLine($"ClipPos: {rawPreviewImageClipRegion.Position.ToString("F2")}, Tr.ClipPos: {PreviewImageClipRegion.Position.ToString("F2")}");
			//Debug.WriteLine($"ScaleFactors:: OrigToNew: {originalToNewScaleFactor}, BoundingArea: {scaleFactor}, PreviewImage: {previewImageScaleFactor}.");
		}

		#endregion

		#region Private Methods

		private void TranslateNewAndOrigImages(PointDbl boundingImagePos, ref PointDbl originalImagePos, ref PointDbl newImagePos)
		{
			// TODO: Take into account position of the originalImagePos

			double oPosX;
			double nPosX;
			if (newImagePos.X <= originalImagePos.X)
			{
				nPosX = boundingImagePos.X;
				oPosX = nPosX - newImagePos.X;
			}
			else
			{
				oPosX = boundingImagePos.X + originalImagePos.X;
				nPosX = oPosX + newImagePos.X;
			}

			double oPosY;
			double nPosY;
			if (newImagePos.Y <= originalImagePos.Y)
			{
				nPosY = boundingImagePos.Y;
				oPosY = nPosY - newImagePos.Y;
			}
			else
			{
				oPosY = boundingImagePos.Y + originalImagePos.Y;
				nPosY = oPosY + newImagePos.Y;
			}

			newImagePos = new PointDbl(nPosX, nPosY);
			originalImagePos = new PointDbl(oPosX, oPosY);
		}

		private RectangleDbl GetOriginalImageClipRegion(RectangleDbl newMapArea, RectangleDbl originalMapArea)
		{
			var coma1 = GetIntersect1(newMapArea, originalMapArea);
			var coma2 = GetIntersect2(newMapArea, originalMapArea);
			var coma3 = ScreenTypeHelper.Intersect(newMapArea, originalMapArea);

			if (coma1 != coma3 || coma2 != coma3)
			{
				Debug.WriteLine($"Comma1 != Comma2 or Comma3 {coma1}, {coma2}, {coma3}.");
			}

			return coma3;
		}


		private RectangleDbl GetIntersect1(RectangleDbl newArea, RectangleDbl originalArea)
		{
			Debug.Assert(originalArea.Position == PointDbl.Zero, "The originalArea has a non-zero position.");

			var X1 = Math.Max(newArea.X1, 0);
			var X2 = Math.Min(newArea.X2, originalArea.Width);
			var Y1 = Math.Max(newArea.Y1, 0);
			var Y2 = Math.Min(newArea.Y2, originalArea.Height);

			if (X1 > originalArea.Width || X2 < 0)
			{
				X1 = 0;
				X2 = 0;
			}

			if (Y1 > originalArea.Height || Y2 < 0)
			{
				Y1 = 0;
				Y2 = 0;
			}

			var clip = new RectangleDbl(X1, X2, Y1, Y2);

			return clip;
		}

		private RectangleDbl GetIntersect2(RectangleDbl newArea, RectangleDbl originalArea)
		{
			//Debug.Assert(originalArea.Position == PointDbl.Zero, "The originalArea has a non-zero position.");

			//float x1 = Mathf.Min(r1.xMax, r2.xMax);
			var x1 = Math.Min(newArea.X2, originalArea.X2);

			//float x2 = Mathf.Max(r1.xMin, r2.xMin);
			var x2 = Math.Max(newArea.X1, originalArea.X1);

			//float y1 = Mathf.Min(r1.yMax, r2.yMax);
			var y1 = Math.Min(newArea.Y2, originalArea.Y2);

			//float y2 = Mathf.Max(r1.yMin, r2.yMin);
			var y2 = Math.Max(newArea.Y1, originalArea.Y1);

			var result = new RectangleDbl
				(
				new PointDbl
					(
					//area.x = Mathf.Min(x1, x2);
					Math.Min(x1, x2),
					//area.y = Mathf.Min(y1, y2);
					Math.Min(y1, y2)
					),
				new SizeDbl
					(
					//area.width = Mathf.Max(0.0f, x1 - x2);
					Math.Max(x1 - x2, 0),
					//area.height = Mathf.Max(0.0f, y1 - y2);
					Math.Max(y1 - y2, 0)
					)
				);

			return result;
		}

		private RectangleDbl GetIntersect3(RectangleDbl newArea, RectangleDbl originalArea)
		{
			return ScreenTypeHelper.Intersect(newArea, originalArea);
		}
		
		#endregion
	}
}
