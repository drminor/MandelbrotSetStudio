using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSetExplorer
{
	public class AppNavResult
	{
		public AppNavOperation AppNavOperation { get; init; }
		public string? Target { get; init; }
	}

	public enum AppNavOperation
	{
		Explore,
		Design,
		Configure
	}
}
