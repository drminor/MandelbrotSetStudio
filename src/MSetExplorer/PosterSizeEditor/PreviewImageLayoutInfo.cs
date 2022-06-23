using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	public class PreviewImageLayoutInfo
	{
		public PreviewImageLayoutInfo()
		{
		}

		public PreviewImageLayoutInfo(SizeDbl imageSize, SizeDbl previewImageSize, SizeDbl containerSize)
		{
			OriginalMapSize = imageSize;
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
			//var placementMode = ((int)Math.Round(NewMapArea.X1)) % 4;
			Debug.WriteLine($"Edit Poster Size Layout Update: NewMapArea: {NewMapArea}, ContainerSize: {ContainerSize}.");

			//var originalToNewScaleFactor = RMapHelper.GetSmallestScaleFactor(OriginalMapSize, NewMapArea.Size);
			//var adjOriginalMapSize = OriginalMapSize.Scale(originalToNewScaleFactor);

			// Create a rectangle that encloses both the new and original maps
			var originalMapArea = new RectangleDbl(new PointDbl(), OriginalMapSize);
			//var clippedOriginalMapArea = GetOriginalImageClipRegion(NewMapArea, originalMapArea);

			var coma1 = GetIntersect1(NewMapArea, originalMapArea);
			var coma2 = GetIntersect2(NewMapArea, originalMapArea);
			var coma3 = GetIntersect3(NewMapArea, originalMapArea);

			Debug.Assert(coma1 == coma2, "Get Intersect1 and 2 Mismatch.");
			Debug.Assert(coma2 == coma3, "Get Intersect2 and 3 Mismatch.");
			Debug.Assert(coma1 == coma3, "Get Intersect1 and 3 Mismatch.");

			var clippedOriginalMapArea = coma3;

		   //var boundingMapArea = RMapHelper.GetBoundingRectangle(originalMapArea, NewMapArea);
		   var boundingMapArea = RMapHelper.GetBoundingRectangle(clippedOriginalMapArea, NewMapArea);

			// Determine Size and Location of bounding rectangle
			var scaleFactor = RMapHelper.GetSmallestScaleFactor(boundingMapArea.Size, ContainerSize);
			var boundingImageSize = boundingMapArea.Size.Scale(scaleFactor);
			var boundingImageArea = boundingImageSize.PlaceAtCenter(ContainerSize); // This centers the BoundingImageArea on the Control.

			// Determine Size and Location of the original and new images
			var boundingImagePos = boundingImageArea.Position;
			var newImagePos = NewMapArea.Position.Scale(scaleFactor);

			var originalImagePos = TranslateNewAndOrigImages(boundingImagePos, ref newImagePos);

			var originalImageSize = OriginalMapSize.Scale(scaleFactor);
			OriginalImageArea = new RectangleDbl(originalImagePos, originalImageSize);

			var newImageSize = NewMapArea.Size.Scale(scaleFactor);
			NewImageArea = new RectangleDbl(newImagePos, newImageSize);

			// Get the scale factor needed to reduce the actual preview image's bitmap to the container
			var previewImageScaleFactor = RMapHelper.GetSmallestScaleFactor(PreviewImageSize, originalImageSize);
			ScaleFactorForPreviewImage = double.IsInfinity(previewImageScaleFactor) ? 1 : double.IsNaN(previewImageScaleFactor) ? 1 : previewImageScaleFactor;

			// Calculate the area of the OriginalImage that will be retained.
			//var rawPreviewImageClipRegion = GetOriginalImageClipRegion(NewImageArea, OriginalImageArea);
			var rawPreviewImageClipRegion = clippedOriginalMapArea.Scale(scaleFactor);

			PreviewImageClipRegion = rawPreviewImageClipRegion.Translate(OriginalImageArea.Position);
			PreviewImageClipRegionYInverted = ScreenTypeHelper.FlipY(rawPreviewImageClipRegion, OriginalImageArea.Height);

			Debug.WriteLine($"BoundingSize: {boundingImageArea.Size}, NewSize: {newImageSize}, OriginalSize: {originalImageSize}.");
			Debug.WriteLine($"BoundingPos: {boundingImageArea.Position}, NewPos: {newImagePos}, OrigPos: {originalImagePos}");
			//Debug.WriteLine($"ScaleFactors:: OrigToNew: {originalToNewScaleFactor}, BoundingArea: {scaleFactor}, PreviewImage: {previewImageScaleFactor}.");
		}

		private PointDbl TranslateNewAndOrigImages(PointDbl boundingImagePos, ref PointDbl newImagePos)
		{
			double oPosX;
			double nPosX;
			if (newImagePos.X <= 0)
			{
				nPosX = boundingImagePos.X;
				oPosX = nPosX - newImagePos.X;
			}
			else
			{
				oPosX = boundingImagePos.X;
				nPosX = oPosX + newImagePos.X;
			}

			double oPosY;
			double nPosY;
			if (newImagePos.Y <= 0)
			{
				nPosY = boundingImagePos.Y;
				oPosY = nPosY + newImagePos.Y;
			}
			else
			{
				oPosY = boundingImagePos.Y;
				nPosY = oPosY + newImagePos.Y;
			}

			newImagePos = new PointDbl(nPosX, nPosY);
			var originalImagePos = new PointDbl(oPosX, oPosY);

			return originalImagePos;
		}

		//private RectangleDbl GetOriginalImageClipRegion(RectangleDbl newArea, RectangleDbl originalArea)
		//{
		//	Debug.Assert(originalArea.X1 == 0 && originalArea.Y1 == 0);

		//	//var newAreaP1 = newArea.Position.Invert();
		//	//var newAreaP2 = newAreaP1.Translate(new PointDbl(newArea.Size));

		//	//var newAreaP1 = newArea.Point1; // Left, Bottom
		//	//var newAreaP2 = newArea.Point2; // Right, Top

		//	//var X1 = Math.Max(newArea.X1 - originalArea.X1, 0);
		//	//var X1 = Math.Max(newAreaP1.X, 0);
		//	var X1 = Math.Max(newArea.Point1.X - originalArea.Point1.X, 0);

		//	//var X2 = originalArea.Width - Math.Max(originalArea.X2 - newArea.X2, 0);
		//	//var X2 = originalArea.Width - Math.Max(originalArea.Width - newAreaP2.X, 0);
		//	var X2 = Math.Max(newArea.Point2.X, originalArea.Point2.X);

		//	//var Y1 = Math.Max(newArea.Y1 - originalArea.Y1, 0);
		//	//var Y1 = Math.Max(newAreaP1.Y, 0);
		//	var Y1 = Math.Max(newArea.Point1.Y - originalArea.Point1.Y, 0);

		//	//var Y2 = originalArea.Height - Math.Max(originalArea.Y2 - newArea.Y2, 0);
		//	//var Y2 = originalArea.Height - Math.Max(originalArea.Height - newAreaP2.Y, 0);
		//	var Y2 = Math.Max(newArea.Point2.Y, originalArea.Point2.Y);

		//	if (X1 > originalArea.Width || X2 < 0)
		//	{
		//		X1 = 0;
		//		X2 = 0;
		//	}
		//	else if (X2 > originalArea.Width)
		//	{
		//		X2 = originalArea.Width;
		//	}

		//	if (Y1 > originalArea.Height || Y2 < 0)
		//	{
		//		Y1 = 0;
		//		Y2 = 0;
		//	}
		//	else if (Y2 > originalArea.Height)
		//	{
		//		Y2 = originalArea.Width;
		//	}


		//	var clip = new RectangleDbl(X1, X2, Y1, Y2);

		//	return clip;
		//}

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
			Debug.Assert(originalArea.Position == PointDbl.Zero, "The originalArea has a non-zero position.");

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
			var na = ScreenTypeHelper.ConvertToRect(newArea);
			var oa = ScreenTypeHelper.ConvertToRect(originalArea);

			oa.Intersect(na);

			var result = ScreenTypeHelper.ConvertToRectangleDbl(oa);
			return result;
		}
		
		#endregion

		}
	}
