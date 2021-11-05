
namespace MSS.Types
{
	public class DSize
	{
		public DSize() : this(0, 0) { }

		public DSize(double width, double heigth)
		{
			Width = width;
			Height = heigth;
		}

		public double Width { get; set; }
		public double Height { get; set; }
	}
}
