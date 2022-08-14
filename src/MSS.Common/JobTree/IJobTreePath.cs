using MSS.Types;

namespace MSS.Common
{
	public interface IJobTreePath : IJobTreeBranch
	{
		bool IsHome { get; }
		bool IsRoot { get; }
		JobTreeItem Item { get; }

		JobTreePath CreateSiblingPath(JobTreeItem child);

		bool IsActiveAlternate { get; }
		bool IsParkedAlternate { get; }
		TransformType TransformType { get; }

	}
}