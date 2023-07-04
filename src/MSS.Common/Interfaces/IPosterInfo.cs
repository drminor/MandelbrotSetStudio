using MongoDB.Bson;
using MSS.Types;
using System.ComponentModel;

namespace MSS.Common
{
	public interface IPosterInfo : IJobOwnerInfo, INotifyPropertyChanged
	{
		ObjectId PosterId { get; init; }
		
		SizeDbl Size { get; init; }
	}
}