using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class CancelResponse
	{
		[DataMember(Order = 1)]
		public bool RequestWasCancelled { get; set; }

		[DataMember(Order = 2)]
		public string ErrorMessage { get; set; }

		public override string ToString()
		{
			return $"RequestWasCancelled: {RequestWasCancelled}. ErrorMessage: {ErrorMessage}";
		}
	}
}
