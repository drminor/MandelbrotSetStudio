using System;

namespace MSS.Types
{
	public interface IBlendVals_NotUsed 
	{
		int BlendAndPlace(double factor, Span<byte> destination);
	}
}