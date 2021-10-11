using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CountsRepo
{
	class SerializationHelper
	{
		public static bool Serialize<T>(T value, ref StringBuilder sb)
		{
			if (value == null)
				return false;

			try
			{
				XmlSerializer xmlserializer = new XmlSerializer(typeof(T));
				using (XmlWriter writer = XmlWriter.Create(sb))
				{
					xmlserializer.Serialize(writer, value);
					writer.Close();
				}
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return false;
			}
		}

		public static bool Deserialize<T>(string xml, out T obj)
		{
			if (xml == null)
			{
				obj = default(T);
				return false;
			}

			try
			{
				XmlSerializer xmlserializer = new XmlSerializer(typeof(T));
				using (TextReader reader = new StringReader(xml))
				{
					obj = (T)xmlserializer.Deserialize(reader);
				}

				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				obj = default(T);
				return false;
			}
		}


	}
}
