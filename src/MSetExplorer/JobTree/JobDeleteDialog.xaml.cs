using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
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
			SelectionType = NodeSelectionType.SingleNode;
		}

		private void BranchButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.Branch;
			DialogResult = true;
		}

		private void SiblingsButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.Siblings;
			DialogResult = true;
		}

		private void PreceedingButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.SingleNode | NodeSelectionType.Preceeding;
			DialogResult = true;
		}

		private void FollowingButton_Click(object sender, RoutedEventArgs e)
		{
			SelectionType = NodeSelectionType.SingleNode | NodeSelectionType.Children;
			DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		#endregion
	}
}
