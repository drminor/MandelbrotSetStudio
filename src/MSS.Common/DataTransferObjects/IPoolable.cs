using System;

namespace MSS.Common.DataTransferObjects
{
	public interface IPoolable : IDisposable
	{
		void ResetObject();
	}
}
