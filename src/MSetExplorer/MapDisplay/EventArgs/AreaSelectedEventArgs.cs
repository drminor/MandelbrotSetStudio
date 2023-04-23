using MSS.Types;
using System;

namespace MSetExplorer
{
	public class AreaSelectedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }

		public VectorInt PanAmount { get; init; }
		public double Factor { get; init; }

		public bool IsPreview { get; init; }

		//public bool PerformDiagnostics { get; set; }

		public AreaSelectedEventArgs(TransformType transformType, VectorInt panAmount, double factor, bool isPreview = false)
		{
			TransformType = transformType;

			PanAmount = panAmount;
			Factor = factor;

			IsPreview = isPreview;

		}

	}

}
