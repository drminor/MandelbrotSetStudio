using System;

namespace MSS.Types
{
	public interface IPoolable : IDisposable
	{
		int ReferenceCount { get; }

		int IncreaseRefCount();

		int DecreaseRefCount();

		//void CopyTo(object obj);
		void ResetObject();
	}
}
