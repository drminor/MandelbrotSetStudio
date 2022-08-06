using System;
using System.Collections.Generic;
using System.Linq;

namespace MSS.Common
{
	public class JobTreePath : ICloneable
	{
		#region Static Members

		private static readonly JobTreePath? _nullValue;

		static JobTreePath()
		{
			_nullValue = null;
		}

		public static JobTreePath? NullPath => _nullValue;

		#endregion

		#region Constructor

		public JobTreePath(JobTreeItem term)
		{
			Terms = new List<JobTreeItem> { term };
		}

		public JobTreePath(IEnumerable<JobTreeItem> terms)
		{
			Terms = new List<JobTreeItem>(terms);
		}

		#endregion

		#region Public Properties

		public List<JobTreeItem> Terms { get; init; }

		public int Count => Terms.Count;

		public JobTreeItem LastTerm => Terms[^1];

		public JobTreeItem ParentTerm => Terms[^2];

		public JobTreeItem GrandparentTerm => Terms[^3];

		#endregion

		#region Public Methods

		public JobTreePath GetParentPath()
		{
			return new JobTreePath(Terms.SkipLast(1));
		}

		public JobTreePath GetGrandParentPath()
		{
			return new JobTreePath(Terms.SkipLast(2));
		}

		public JobTreePath Combine(JobTreePath jobTreePath)
		{
			return Combine(jobTreePath.Terms);
		}

		public JobTreePath Combine(JobTreeItem jobTreeItem)
		{
			return Combine( new[] { jobTreeItem });
		}

		public JobTreePath Combine(IEnumerable<JobTreeItem> jobTreeItems)
		{
			var result = Clone();
			result.Terms.AddRange(jobTreeItems);
			return result;
		}

		#endregion


		#region Overrides, Conversion Operators and ICloneable Support

		public static implicit operator List<JobTreeItem>?(JobTreePath? jobTreePath) => jobTreePath == null ? null : jobTreePath.Terms;

		public static explicit operator JobTreePath(List<JobTreeItem> terms) => new JobTreePath(terms);

		public override string ToString() => string.Join('\\', Terms);

		object ICloneable.Clone()
		{
			return Clone();
		}

		public JobTreePath Clone()
		{
			return new JobTreePath(Terms);
		}

		#endregion

	}
}