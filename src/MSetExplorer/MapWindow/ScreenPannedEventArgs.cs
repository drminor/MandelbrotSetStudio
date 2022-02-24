using MSS.Types;
using System;

namespace MSetExplorer
{
	public class ScreenPannedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public VectorInt Offset { get; init; }

		public ScreenPannedEventArgs(TransformType transformType, VectorInt offset)
		{
			TransformType = transformType;
			Offset = offset;
		}
	}

}
