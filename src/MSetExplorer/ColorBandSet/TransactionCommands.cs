using System.Windows.Input;

namespace MSetExplorer
{
	public static class TransactionCommands
    {
        public static readonly RoutedUICommand Edit
            = new("Edit", "Edit", typeof(TransactionCommands));

        public static readonly RoutedUICommand Cancel
            = new("Cancel", "Cancel", typeof(TransactionCommands));

        public static readonly RoutedUICommand Commit
            = new("Commit", "Commit", typeof(TransactionCommands));
    }
}
