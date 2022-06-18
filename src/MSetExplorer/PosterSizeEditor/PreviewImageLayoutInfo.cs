using MSS.Common;
using MSS.Types;
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

		//public double BeforeX => OriginalImageArea.X1 - NewImageArea.X1;
		//public double AfterX => NewImageArea.X2 - OriginalImageArea.X2;

		//public double BeforeY => OriginalImageArea.Y1 - NewImageArea.Y1;
		//public double AfterY => NewImageArea.Y2 - OriginalImageArea.Y2;

		#endregion

		#region Public Methods

		public void Update()
		{
			Debug.WriteLine($"Edit Poster Size Layout Update: NewMapArea: {NewMapArea}, ContainerSize: {ContainerSize}.");

			// Determine Size and Location of "enclosing" rectangle, representing the new size
			var scaleFactor = RMapHelper.GetSmallestScaleFactor(NewMapArea.Size, ContainerSize);
			var newImageSize = NewMapArea.Size.Scale(scaleFactor);
			NewImageArea = newImageSize.PlaceAtCenter(ContainerSize); // This centers the Preview Map and Enclosing rectangle on the Control.

			// Determine Size and Location of the PreviewImage, representing the current size.
			var originalImageSize = OriginalMapSize.Scale(scaleFactor);
			var originalImagePos = NewMapArea.Position.Scale(scaleFactor);
			OriginalImageArea = new RectangleDbl(originalImagePos, originalImageSize);

			// Get the scale factor needed to reduce the logical preview image area into its "container" rectangle
			//var insideScaleFactor = RMapHelper.GetSmallestScaleFactor(newImageSize, originalImageSize);
			var insideScaleFactor = RMapHelper.GetSmallestScaleFactor(originalImageSize, newImageSize);

			// Get the scale factor needed to reduce the actual preview image's bitmap to the container
			var previewImageScaleFactor = RMapHelper.GetSmallestScaleFactor(PreviewImageSize, originalImageSize);

			ScaleFactorForPreviewImage = previewImageScaleFactor;

			Debug.WriteLine($"NewImageArea: {NewImageArea}, OriginalImageArea: {OriginalImageArea}, insideSF: {insideScaleFactor}, previewSF: {ScaleFactorForPreviewImage}.");
		}

		#endregion

		#region Unused

		//public static SizeDbl GetNewImageSizePreserveAspect(SizeDbl newImageSize, SizeDbl originalImageSize)
		//{
		//	var wRat = newImageSize.Width / originalImageSize.Width;
		//	var hRat = newImageSize.Height / originalImageSize.Height;

		//	SizeDbl result;

		//	if (originalImageSize.Height * wRat < originalImageSize.Width * hRat)
		//	{
		//		result = new SizeDbl(newImageSize.Width, originalImageSize.Height * wRat);
		//	}
		//	else
		//	{
		//		result = new SizeDbl(originalImageSize.Width * hRat, newImageSize.Height);
		//	}

		//	return result;
		//}

		//public static SizeDbl GetLargestScaledSizeToFit(SizeDbl sizeToFit, SizeDbl containerSize)
		//{
		//	var wRat = containerSize.Width / sizeToFit.Width; // Scale Factor to multiply item being fitted to get container units.
		//	var hRat = containerSize.Height / sizeToFit.Height;

		//	// Use the smallest scale factor on both to get the new size

		//	SizeDbl result;

		//	if (wRat <= hRat)
		//	{
		//		result = new SizeDbl(containerSize.Width, sizeToFit.Height * wRat);
		//	}
		//	else
		//	{
		//		result = new SizeDbl(sizeToFit.Width * hRat, containerSize.Height);
		//	}

		//	return result;
		//}

		#endregion

	}
}
