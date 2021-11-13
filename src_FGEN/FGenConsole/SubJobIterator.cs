using MqMessages;

namespace FGenConsole
{
	internal class SubJobIterator
	{
		private readonly Job _job;
		private readonly SizeInt _size;

		private int _vSectionPtr;
		private int _hSectionPtr;

		public bool IsCompleted { get; private set; }

		public SubJobIterator(Job job)
		{
			_job = job;
			_size = job.FJobRequest.Area.Size;
			Reset();
		}

		public SubJob GetNextSubJob()
		{
			if (IsCompleted) return null;

			if (_hSectionPtr > _size.W - 1)
			{
				_hSectionPtr = 0;
				_vSectionPtr++;

				if (_vSectionPtr > _size.H - 1)
				{
					IsCompleted = true;
					return null;
				}
			}

			//System.Diagnostics.Debug.WriteLine($"Creating SubJob for hSection: {_hSectionPtr}, vSection: {_vSectionPtr}.");
			SubJob result = new SubJob(_job, new KPoint(_hSectionPtr++, _vSectionPtr));
			return result;
		}

		public void Reset()
		{
			IsCompleted = false;
			_vSectionPtr = 0;
			_hSectionPtr = 0;
		}
	}
}
