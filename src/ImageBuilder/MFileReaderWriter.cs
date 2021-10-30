
using MFile;
using System.IO;

using System.Text.Json;

namespace ImageBuilder
{
    public class MFileReaderWriter
    {
        public MFileInfo Read(string path)
        {
            string jsonContent = File.ReadAllText(path);

            JsonSerializerOptions jsonSerializerOptions = GetReadOptions();
            MFileInfo result = JsonSerializer.Deserialize<MFileInfo>(jsonContent, jsonSerializerOptions);
            return result;
        }

        private JsonSerializerOptions GetReadOptions()
        {
            var options = new JsonSerializerOptions();
            return options;
        }

        public void Write(MFileInfo mFileInfo, string path)
        {
            var jsonSerializerOptions = GetWriteOptions();
            string jsonContent = JsonSerializer.Serialize(mFileInfo, jsonSerializerOptions);

            File.WriteAllText(path, jsonContent);
        }

        private JsonSerializerOptions GetWriteOptions()
		{
            var options = new JsonSerializerOptions { WriteIndented = true };
            return options;
        }

    }
}
