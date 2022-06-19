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

		#endregion

		#region Public Methods

		public void Update()
		{
			Debug.WriteLine($"Edit Poster Size Layout Update: NewMapArea: {NewMapArea}, ContainerSize: {ContainerSize}.");

			// Create a rectangle that encloses both the new and original maps
			var originalMapArea = new RectangleDbl(new PointDbl(), OriginalMapSize);

			var boundingMapArea = RMapHelper.GetBoundingRectangle(originalMapArea, NewMapArea);

			// Determine Size and Location of bounding rectangle
			var scaleFactor = RMapHelper.GetSmallestScaleFactor(boundingMapArea.Size, ContainerSize);
			var boundingImageSize = boundingMapArea.Size.Scale(scaleFactor);
			var boundingImageArea = boundingImageSize.PlaceAtCenter(ContainerSize); // This centers the BoundingImageArea on the Control.

			// Determine Size and Location of the original image.
			var originalImageSize = OriginalMapSize.Scale(scaleFactor);
			var originalImagePos = NewMapArea.Position.Scale(scaleFactor); // Relative to the NewImageArea == Point 0,0 within the BoundingRectangle
			var originalImagePosTr = originalImagePos.Translate(boundingImageArea.Position);
			OriginalImageArea = new RectangleDbl(originalImagePosTr, originalImageSize);

			// Determine Size and Location of the new image
			var newImageSize = NewMapArea.Size.Scale(scaleFactor);
			var newImagePos = boundingImageArea.Position;
			NewImageArea = new RectangleDbl(newImagePos, newImageSize);

			// Get the scale factor needed to reduce the actual preview image's bitmap to the container
			var previewImageScaleFactor = RMapHelper.GetSmallestScaleFactor(PreviewImageSize, originalImageSize);

			ScaleFactorForPreviewImage = double.IsInfinity(previewImageScaleFactor) ? 1 : double.IsNaN(previewImageScaleFactor) ? 1 : previewImageScaleFactor;

			Debug.WriteLine($"NewImageArea: {NewImageArea}, OriginalImageArea: {OriginalImageArea}, previewSF: {ScaleFactorForPreviewImage}.");
		}

		#endregion

	}
}
