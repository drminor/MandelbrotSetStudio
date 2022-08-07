using MSS.Types.MSet;
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

		public JobTreePath(JobTreeItem term) : this (new [] { term })
		{ }

		public JobTreePath(IEnumerable<JobTreeItem> terms)
		{
			if (!terms.Any())
			{
				throw new ArgumentException("When creating a JobTreePath, terms must have at least one element.");
			}

			Terms = new List<JobTreeItem>(terms);
		}

		#endregion

		#region Public Properties

		public List<JobTreeItem> Terms { get; init; }

		public int Count => Terms.Count;

		public JobTreeItem LastTerm => Terms[^1];

		public JobTreeItem? ParentTerm => Terms.Count > 1 ?  Terms[^2] : null;

		public JobTreeItem? GrandparentTerm => Terms.Count > 2 ? Terms[^3] : null;

		public JobTreeItem CanvasSizeUpdateParentTerm => Terms[^2];

		public Job Job => Terms[^1].Job;

		#endregion

		#region Public Methods

		public JobTreePath? GetParentPath()
		{
			return Terms.Count > 1 ? new JobTreePath(Terms.SkipLast(1)) : NullPath;
		}

		public JobTreePath? GetGrandparentPath()
		{
			return Terms.Count > 2 ? new JobTreePath(Terms.SkipLast(2)) : NullPath;
		}

		public JobTreePath GetParentPathForCanvasSizeUpdate()
		{
			// This theoretically could throw an exception, but it does then its due to an error in the caller's application logic.
			return new JobTreePath(Terms.SkipLast(1));
		}

		public JobTreePath GetParentPathForParkedAlt()
		{
			// This theoretically could throw an exception, but it does then its due to an error in the caller's application logic.
			return new JobTreePath(Terms.SkipLast(1));
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