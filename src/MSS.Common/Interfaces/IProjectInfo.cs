using MongoDB.Bson;
using System.ComponentModel;

namespace MSS.Common
{
	public interface IProjectInfo : IJobOwnerInfo, INotifyPropertyChanged
	{
		ObjectId ProjectId { get; }
	}
}