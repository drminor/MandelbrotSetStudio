using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class ColorMapCountInfo : INotifyPropertyChanged
	{
		#region Constructor



		public ColorMapCountInfo(int countVal, int colorMapIndex, double stepFactor, double r, double g, double b, double l, double c, double h)
			: this(countVal, 0, 0, colorMapIndex, stepFactor, r, g, b, l, c, h)
		{
		}

		public ColorMapCountInfo(int countVal, long count, double percentage, int colorMapIndex, double stepFactor, double r, double g, double b, double l, double c, double h)
		{
			CountVal = countVal;
			Count = count;
			Percentage = percentage;

			ColorMapIndex = colorMapIndex;
			StepFactor = stepFactor;

			R = r;
			G = g;
			B = b;

			L = l;
			C = c;
			H = h;
		}

		#endregion

		#region Public Properties

		public int CountVal { get; init; }
		public long Count { get; init; }
		public double Percentage { get; init; }
		public int ColorMapIndex { get; init; }
		public double StepFactor { get; init; }

		public double R { get; init; }
		public double G { get; init; }
		public double B { get; init; }

		public double L { get; init; }
		public double C { get; init; }
		public double H { get; init; }

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
