using FSTypes;

namespace MClient
{
	public interface IClientConnector
	{
		void ReceiveImageData(string connectionId, MapSectionResult mapSectionResult, bool isFinalSection);

		void ConfirmJobCancel(string connectionId, int jobId);
	}
}