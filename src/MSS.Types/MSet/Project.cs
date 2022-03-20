﻿using MongoDB.Bson;
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
		public Collection<Guid> ColorBandSetSNs { get; init; }

		private IColorBandSet _currentColorBandSet;

		public Project(string name, string? description, IColorBandSet currentColorBandSet) 
			: this(ObjectId.Empty, name, description, new List<Guid> { currentColorBandSet.SerialNumber }, currentColorBandSet)
		{ }

		public Project(string name, string? description, IList<Guid> colorBandSetIds, IColorBandSet currentColorBandSet)
			: this(ObjectId.Empty, name, description, colorBandSetIds, currentColorBandSet)
		{ }

		public Project(ObjectId id, string name, string? description, IList<Guid> colorBandSetSNs, IColorBandSet currentColorBandSet)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description;
			ColorBandSetSNs = CloneSetIds(colorBandSetSNs);

			_currentColorBandSet = currentColorBandSet;  //.Clone();
		}

		public DateTime DateCreated => Id.CreationTime;

		public bool OnFile => Id != ObjectId.Empty;


		public IColorBandSet CurrentColorBandSet
		{
			get => _currentColorBandSet;
			set
			{
				if (value != _currentColorBandSet)
				{
					if (!ColorBandSetSNs.Contains(value.SerialNumber))
					{
						ColorBandSetSNs.Add(value.SerialNumber);
					}

					_currentColorBandSet = value;
				}
			}
		}


		private Collection<Guid> CloneSetIds(IList<Guid> setSNs)
		{
			Collection<Guid> result;

			if (setSNs == null || setSNs.Count == 0)
			{
				result = new Collection<Guid>();
			}
			else
			{
				result = new Collection<Guid>(setSNs.Select(x => new Guid(x.ToByteArray())).ToList());
			}

			return result;
		}
	}
}
