using MSS.Types;
using System;

namespace MSetExplorer
{
	public class AreaSelectedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }

		public VectorInt PanAmount { get; init; }
		public double Factor { get; init; }

		public RectangleDbl ScreenArea { get; init; }
		public SizeDbl DisplaySize { get; init; }

		public bool IsPreview { get; init; }
		public bool IsPreviewBeingCancelled { get; init; }

		public AreaSelectedEventArgs(TransformType transformType, VectorInt panAmount, double factor, bool isPreview)
			: this(transformType, panAmount, factor, new RectangleDbl(), new SizeDbl(), isPreview)
		{ }

		public AreaSelectedEventArgs(TransformType transformType, VectorInt panAmount, double factor, RectangleDbl screenArea, SizeDbl displaySize, bool isPreview)
		{
			TransformType = transformType;

			PanAmount = panAmount;
			Factor = factor;

			ScreenArea = screenArea;
			DisplaySize = displaySize;

			IsPreview = isPreview;
		}

		public static AreaSelectedEventArgs CreateCancelPreviewInstance(TransformType transformType)
		{
			var result = new AreaSelectedEventArgs(transformType, new VectorInt(), 0.0, isPreview: true)
			{
				IsPreviewBeingCancelled = true
			};

			return result;
		}

	}
}
