using ImageBuilderWPF;
using System.IO;

namespace ImageBuilderWPFTest
{
	public class WMPhotoEncodingTest
	{
		[Fact]
		public void Test1()
		{
			var basePath = @"C:\Users\david\Documents";
			var fileName = "Test1a.jxr";
			var filePath = Path.Combine(basePath, fileName);

			var jpegXrBuilder = new WmpBuilder();

			jpegXrBuilder.Test1(filePath);
		}

		[Fact]
		public void Test2()
		{
			var basePath = @"C:\Users\david\Documents";
			var fileName = "Test2a.jxr";
			var filePath = Path.Combine(basePath, fileName);

			var jpegXrBuilder = new WmpBuilder();

			jpegXrBuilder.Test2(filePath);

		}
	}
}