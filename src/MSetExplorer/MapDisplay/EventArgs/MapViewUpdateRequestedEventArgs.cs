using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MapViewUpdateRequestedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public VectorInt PanAmount { get; init; }
		public double Factor { get; init; }

		public MapAreaInfo2 CurrentMapAreaInfo { get; init; }

		public RectangleDbl ScreenArea { get; init; }
		public SizeDbl DisplaySize { get; init; }

		public bool IsPreview { get; init; }
		public bool IsPreviewBeingCancelled { get; init; }

		public MapViewUpdateRequestedEventArgs(TransformType transformType, VectorInt panAmount, double factor, MapAreaInfo2 currentMapAreaInfo, bool isPreview)
		{
			TransformType = transformType;
			PanAmount = panAmount;
			Factor = factor;

			ScreenArea = new RectangleDbl();
			DisplaySize = new SizeDbl();

			CurrentMapAreaInfo = currentMapAreaInfo;
			IsPreview = isPreview;
		}

		public MapViewUpdateRequestedEventArgs(TransformType transformType, VectorInt panAmount, double factor, RectangleDbl screenArea, SizeDbl displaySize, MapAreaInfo2 currentMapAreaInfo, bool isPreview)
		{
			TransformType = transformType;
			PanAmount = panAmount;
			Factor = factor;

			ScreenArea = screenArea;
			DisplaySize = displaySize;

			CurrentMapAreaInfo = currentMapAreaInfo;
			IsPreview = isPreview;
		}

		public static MapViewUpdateRequestedEventArgs CreateCancelPreviewInstance(TransformType transformType)
		{
			var result = new MapViewUpdateRequestedEventArgs(transformType, new VectorInt(), 0.0, new MapAreaInfo2(), isPreview: true)
			{
				IsPreviewBeingCancelled = true
			};

			return result;
		}

	}

}
