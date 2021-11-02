using MSS.Types;

namespace MSetInfoRepo
{
	public static class MSetInfoReaderWriter
	{
		public static MSetInfo Read(string path)
		{
			MFileInfo mFileInfo = ReadFromJson(path);
			MSetInfo result = MFileHelper.GetMSetInfo(mFileInfo);

			return result;
		}

		public static void Write(MSetInfo mSetInfo, string path)
		{

		}

		private static MFileInfo ReadFromJson(string path)
		{
			MFileInfo mFileInfo = MFileReaderWriter.Read(path);
			return mFileInfo;
		}

	}
}
