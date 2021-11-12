using MSS.Types;
using MSS.Types.MSetOld;

namespace MSetInfoRepo
{
	public static class MSetInfoReaderWriter
	{
		public static MSetInfoOld Read(string path)
		{
			MFileInfo mFileInfo = ReadFromJson(path);
			MSetInfoOld result = MFileHelper.GetMSetInfo(mFileInfo);

			return result;
		}

		public static void Write(MSetInfoOld mSetInfo, string path)
		{

		}

		private static MFileInfo ReadFromJson(string path)
		{
			MFileInfo mFileInfo = MFileReaderWriter.Read(path);
			return mFileInfo;
		}

	}
}
