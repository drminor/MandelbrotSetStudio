using MongoDB.Bson;
using MSS.Types;
using System;
using System.ComponentModel;

namespace MSS.Common
{
	public interface IPosterInfo : INotifyPropertyChanged
	{
		ObjectId PosterId { get; init; }
		string Name { get; set; }
		string? Description { get; set; }
		ObjectId CurrentJobId { get; init; }
		SizeInt Size { get; init; }
		int Bytes { get; init; }

		DateTime DateCreatedUtc { get; init; }
		DateTime LastSavedUtc { get; set; }
		DateTime LastAccessedUtc { get; set; }
	}
}