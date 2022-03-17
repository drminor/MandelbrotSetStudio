using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MSS.Types.MSet
{
	public class Project
	{
		public ObjectId Id { get; init; }
		public string Name { get; set; }
		public string? Description { get; set; }
		public Collection<Guid> ColorBandSetIds { get; init; }
		public ColorBandSet CurrentColorBandSet { get; set; }

		public Project(string name, string? description, ColorBandSet currentColorBandSet) 
			: this(ObjectId.Empty, name, description, new List<Guid> { currentColorBandSet.SerialNumber }, currentColorBandSet)
		{ }

		public Project(string name, string? description, IList<Guid> colorBandSetIds, ColorBandSet currentColorBandSet)
			: this(ObjectId.Empty, name, description, colorBandSetIds, currentColorBandSet)
		{ }

		public Project(ObjectId id, string name, string? description, IList<Guid> colorBandSetIds, ColorBandSet currentColorBandSet)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description;
			ColorBandSetIds = CloneSetIds(colorBandSetIds);
			CurrentColorBandSet = currentColorBandSet.Clone();
		}

		public DateTime DateCreated => Id.CreationTime;

		public bool OnFile => Id != ObjectId.Empty;

		private Collection<Guid> CloneSetIds(IList<Guid> setIds)
		{
			Collection<Guid> result;

			if (setIds == null || setIds.Count == 0)
			{
				result = new Collection<Guid>();
			}
			else
			{
				result = new Collection<Guid>(setIds.Select(x => new Guid(x.ToByteArray())).ToList());
			}

			return result;
		}
	}
}
