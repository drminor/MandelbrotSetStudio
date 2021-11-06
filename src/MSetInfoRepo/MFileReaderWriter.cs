using System.IO;
using System.Text.Json;

namespace MSetInfoRepo
{
	public static class MFileReaderWriter
    {
        internal static MFileInfo Read(string path)
        {
            string jsonContent = File.ReadAllText(path);

            JsonSerializerOptions jsonSerializerOptions = GetReadOptions();
            MFileInfo? result = JsonSerializer.Deserialize<MFileInfo>(jsonContent, jsonSerializerOptions);

            if (result?.ApCoords == null)
			{
                throw new InvalidDataException($"The contents of file: {path} could not be read in as a MFileInfo object.");
			}

            return result;
        }

        private static JsonSerializerOptions GetReadOptions()
        {
            var options = new JsonSerializerOptions();
            return options;
        }

        internal static void Write(MFileInfo mFileInfo, string path)
        {
            var jsonSerializerOptions = GetWriteOptions();
            string jsonContent = JsonSerializer.Serialize(mFileInfo, jsonSerializerOptions);

            File.WriteAllText(path, jsonContent);
        }

        private static JsonSerializerOptions GetWriteOptions()
		{
            var options = new JsonSerializerOptions { WriteIndented = true };
            return options;
        }

    }
}
