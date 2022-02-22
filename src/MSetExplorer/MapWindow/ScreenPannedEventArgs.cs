using MSS.Types;
using System;

namespace MSetExplorer
{
	public class ScreenPannedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public SizeInt Offset { get; init; }

		public ScreenPannedEventArgs(TransformType transformType, SizeInt offset)
		{
			TransformType = transformType;
			Offset = offset;
		}
	}

}
