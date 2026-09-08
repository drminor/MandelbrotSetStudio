using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class ColorMapCountInfo : INotifyPropertyChanged
	{
		#region Constructor

		public ColorMapCountInfo(int countVal)
		{
			CountVal = countVal;
		}

		#endregion

		#region Public Properties

		public int CountVal { get; init; }

		#endregion

		#region ToString Support

		public override string ToString()
		{
			var result = $"C:{CountVal}-X-X";
			return result;
		}

		#endregion

		#region NotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
