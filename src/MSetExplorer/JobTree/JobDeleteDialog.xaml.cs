using MSS.Common;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for JobDeleteDialog.xaml
	/// </summary>
	public partial class JobDeleteDialog : Window
	{
		public JobDeleteDialog()
		{
			InitializeComponent();
		}

		public NodeSelectionType DeleteType { get; set; }


		#region Button Handlers

		private void SingleButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			DeleteType = NodeSelectionType.SingleNode;
		}

		private void BranchButton_Click(object sender, RoutedEventArgs e)
		{
			DeleteType = NodeSelectionType.Branch;
			DialogResult = true;
		}

		private void PreceedingButton_Click(object sender, RoutedEventArgs e)
		{
			DeleteType = NodeSelectionType.SingleNode | NodeSelectionType.Preceeding;
			DialogResult = true;
		}

		private void FollowingButton_Click(object sender, RoutedEventArgs e)
		{
			DeleteType = NodeSelectionType.SingleNode | NodeSelectionType.Children;
			DialogResult = true;
		}

		private void MakeRootButton_Click(object sender, RoutedEventArgs e)
		{
			DeleteType = NodeSelectionType.SingleNode | NodeSelectionType.Preceeding | NodeSelectionType.Children | NodeSelectionType.Siblings;
			DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		#endregion
	}
}
