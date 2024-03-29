﻿using MSS.Common;
using MSS.Types;
using MSS.Common.MSet;
using System.Windows;

namespace MSetExplorer
{
	using JobPathType = ITreePath<JobTreeNode, Job>;

	/// <summary>
	/// Interaction logic for JobDeleteDialog.xaml
	/// </summary>
	public partial class JobDeleteDialog : Window
	{
		private readonly JobPathType _parentPath;

		public JobDeleteDialog(JobPathType parentPath)
		{
			_parentPath = parentPath;
			Loaded += JobDeleteDialog_Loaded;
			InitializeComponent();
		}

		private void JobDeleteDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (_parentPath.Node.Children.Count > 0)
			{
				pnlBranch.Visibility = Visibility.Visible;
				pnlSiblings.Visibility = Visibility.Visible;
				pnlPreceeding.Visibility = Visibility.Collapsed;
				pnlFollowing.Visibility = Visibility.Collapsed;
			}
			else
			{
				pnlBranch.Visibility = Visibility.Collapsed;
				pnlSiblings.Visibility = Visibility.Collapsed;
				pnlPreceeding.Visibility = Visibility.Visible;
				pnlFollowing.Visibility = Visibility.Visible;
			}
		}

		public NodeSelectionType SelectionType { get; set; }

		#region Button Handlers

		private void SingleButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			SelectionType = NodeSelectionType.SingleNode;						// 1
		}

		private void PreceedingButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.SinglePlusPreceeding;             // 1 | 2				
			DialogResult = true;
		}

		private void FollowingButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.SinglePlusFollowing;              // 1 | 4
			DialogResult = true;
		}

		private void RunButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			SelectionType = NodeSelectionType.Run;								// 1 | 2 | 4
		}

		private void BranchButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.Branch;							// 1 | 8
			DialogResult = true;
		}

		private void AncestorsButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.Branch;							// 16
			DialogResult = true;
		}

		private void NonPreferredButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.AllButPreferred;					// 32
			DialogResult = true;
		}

		private void SiblingsButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.SiblingBranches;					// 64
			DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		/* Button Panel Visibility Guide

			B	R
			Y	Y	SingleNode
			N	Y	Preceeding
			N	Y	Following
			Y	Y	Run
			Y	N	Branch
			Y	Y	Ancestors
			Y	Y	AllButPreferred
			Y	N	SiblingBranches


			--Branch--

			SingleNode
			Run

			Branch

			Ancestors
			AllButPreferred

			SiblingBranches

			--Run--

			SingleNode
			Run

			Preceeding
			Following

			Ancestors
			AllButPreferred

		*/

		#endregion
	}
}
