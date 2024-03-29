﻿
using System;

namespace MSS.Common
{
	[Flags]
	public enum NodeSelectionType
	{
		SingleNode = 1,
		Preceeding = 2,
		SinglePlusPreceeding = 1 | 2,
		Following = 4,
		SinglePlusFollowing = 1 | 4,
		Run = 1 | 2 | 4,
		Children = 8,
		Branch = 1 | 8,
		Ancestors = 16,
		AllButPreferred = 32,
		SiblingBranches = 64
	}

	// Any given node that has more than one child or changes the zoom is a "Branch Head."
	// The first child of the Root Node, i.e., the Home node is considered to be a "Branch Head."

	// The Branch Head for any given node that is itself not a branch head is defined as
	//		the first ancestor node that has more than one child or changes the zoom.

	// Preceeding is defined to be the set of ancestor nodes of a given node up to, but not including,
	//		the first antecendant Branch Head.

	// Following is defined to be the set of descendant nodes of a given node up to, but not including,
	//		the first descendant Branch Head.

	// Children is defined to be the set of all descendant nodes
	//		if and only if the given node is a branch head.

	// Run is used to specify the set of all nodes preceeding, following, and the given node itself,
	//		if and only if the given node is NOT a branch head. (Use SinglePlusFollowing for a BranchHead.)

	// Branch is defined by the children of a given node plus the given node,
	//		if and only if the given node is a branch head.

	// SiblingBranches is defined by the set of all nodes returned by specifying Branch on each sibling of the given node.
	// This will of course be the empty set if the given node has no siblings.

	// To select Children or Branch for a given non-Branch Head node, first find the Branch Head node for the given node
	//		and then specify Children or Branch for that Branch Head node.

	// Similarily, to select a complete Run, first find the Branch Head node and then specify SinglePlusFollowing.
}
