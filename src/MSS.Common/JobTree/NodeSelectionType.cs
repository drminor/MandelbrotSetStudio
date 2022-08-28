
using System;

namespace MSS.Common
{
	[Flags]
	public enum NodeSelectionType
	{
		SingleNode = 1,
		Preceeding = 2,
		Children = 4,
		Siblings = 8,
		Branch = 1 | 4,
		ContainingBranch = 1 | 2 | 4
	}

	// Preceeding is defined to be the set of nodes prior to the terminal node of a specifed path that do not change the zoom level.
	// The node prior to the terminal node of a specified path that does change the zoom level is the "branch head" for that path.
	// Specifying Branch ( 1 | 4 ) at a given path is the same as specifying Children ( 4 ) at it's parent path.
	// Specifying Containing Branch ( 1 | 2 | 4 ) at a given path is the same as specifying Children ( 4 ) on the "branch head" for that path.
}
