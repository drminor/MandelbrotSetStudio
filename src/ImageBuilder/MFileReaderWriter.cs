
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


            //ColorMap cm = ColorMap.GetFromColorMapForExport(miwcmfe.ColorMapForExport);
            //var result = new MapInfoWithColorMap(miwcmfe.MapInfo, cm);
        }

        private JsonSerializerOptions GetReadOptions()
        {
            var options = new JsonSerializerOptions();
            return options;
        }

        public string Write(MFileInfo mFileInfo)
        {
            var jsonSerializerOptions = GetWriteOptions();
            string result = JsonSerializer.Serialize(mFileInfo, jsonSerializerOptions);

            return result;
        }

        private JsonSerializerOptions GetWriteOptions()
		{
            var options = new JsonSerializerOptions { WriteIndented = true };
            return options;
        }

        //private static JsonSerializerSettings BuildSerSettings()
        //{
        //    var result = new JsonSerializerSettings
        //    {
        //        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                
        //    };

        //    return result;
        //}




    }
}
