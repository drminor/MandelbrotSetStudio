using MSS.Common;
using MSS.Types;
using System;
using System.Windows.Media;

namespace MSetExplorer
{
	public class PreviewImageLayoutInfo
	{

		public PreviewImageLayoutInfo(SizeDbl imageSize, SizeDbl previewImageSize, SizeDbl containerSize)
		{
			ImageSize = imageSize;
			PreviewImageSize = previewImageSize;
			ContainerSize = containerSize;

			NewImageArea = new RectangleDbl(new PointDbl(), ImageSize);
		}

		#region Public Properties

		// Environment
		public SizeDbl ImageSize { get; init; }
		public SizeDbl PreviewImageSize { get; init; }

		// Inputs
		public SizeDbl ContainerSize { get; set; }

		public RectangleDbl NewImageArea { get; set; }

		// Outputs
		public RectangleDbl OriginalImageDisplayArea { get; set; } // Size and placement of the preview image, relative to the NewPreviewImageArea

		public RectangleDbl NewImageDisplayArea { get; set; } // Size and placement of a rectangle that encloses the PreviewImageArea, relative to the container  

		public double ScaleFactorForPreviewImage { get; set; } 


		#endregion

		#region Public Methods

		public void Update()
		{
			// ScaleTransform = new SizeDbl(ContainerSize.Width / PreviewImageSize.Width, ContainerSize.Height / PreviewImageSize.Height);

			var scaleFactor = RMapHelper.GetSmallestScaleFactor(NewImageArea.Size, ContainerSize);

			var newImageDisplaySize = NewImageArea.Size.Scale(scaleFactor);
			NewImageDisplayArea = newImageDisplaySize.PlaceAtCenter(ContainerSize);

			var originalImageDisplaySize = ImageSize.Scale(scaleFactor);

			// TODO: don't center, use the NewImageArea offset
			OriginalImageDisplayArea = originalImageDisplaySize.PlaceAtCenter(newImageDisplaySize);

			// Get the scale factor needed to reduce the logical preview image area into its "container" rectangle
			var insideScaleFactor = RMapHelper.GetSmallestScaleFactor(originalImageDisplaySize, newImageDisplaySize);

			// Get the scale factor needed to reduce the actual bitmap to the container
			var previewImageScaleFactor = RMapHelper.GetSmallestScaleFactor(PreviewImageSize, ContainerSize);

			ScaleFactorForPreviewImage = insideScaleFactor * previewImageScaleFactor;
		}

		//public static double GetSmallestScaleFactor(SizeDbl sizeToFit, SizeDbl containerSize)
		//{
		//	var wRat = containerSize.Width / sizeToFit.Width; // Scale Factor to multiply item being fitted to get container units.
		//	var hRat = containerSize.Height / sizeToFit.Height;

		//	var result = Math.Min(wRat, hRat);

		//	return result;
		//}

		//public static RectangleDbl PlaceAtCenter(SizeDbl sizeToFit, SizeDbl containerSize)
		//{
		//	var hDiff = containerSize.Width - sizeToFit.Width;
		//	var vDiff = containerSize.Height - sizeToFit.Height;

		//	var result = new RectangleDbl(new PointDbl(hDiff / 2, vDiff / 2), sizeToFit);

		//	return result;
		//}

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
