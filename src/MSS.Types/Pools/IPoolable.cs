using System;

namespace MSS.Types
{
	public interface IPoolable : IDisposable
	{
		object DuplicateFrom(object obj);


		void ResetObject();
	}
}
