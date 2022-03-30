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

		private ColorBandSet? _currentColorBandSet;

		public Project(string name, string? description, ColorBandSet currentColorBandSet) 
			: this(ObjectId.Empty, name, description, new List<Guid> { currentColorBandSet.SerialNumber }, currentColorBandSet)
		{ }

		public Project(string name, string? description, IList<Guid> colorBandSetIds, ColorBandSet currentColorBandSet)
			: this(ObjectId.Empty, name, description, colorBandSetIds, currentColorBandSet)
		{ }

		public Project(ObjectId id, string name, string? description, IList<Guid> colorBandSetSNs, ColorBandSet currentColorBandSet)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description;
			ColorBandSetSNs = CloneSetIds(colorBandSetSNs);

			_currentColorBandSet = currentColorBandSet;
			ColorBandSetIsDirty = false;
		}

		public DateTime DateCreated => Id.CreationTime;

		public bool OnFile => Id != ObjectId.Empty;

		public ColorBandSet? CurrentColorBandSet
		{
			get => _currentColorBandSet;
			set
			{
				if (value != _currentColorBandSet)
				{
					if (value != null && !ColorBandSetSNs.Contains(value.SerialNumber))
					{
						ColorBandSetSNs.Add(value.SerialNumber);
					}

					_currentColorBandSet = value;

					ColorBandSetIsDirty = true;
				}
			}
		}

		public bool ColorBandSetIsDirty { get; set; }

		private Collection<Guid> CloneSetIds(IList<Guid> setSNs)
		{
			Collection<Guid> result;

			if (setSNs == null || setSNs.Count == 0)
			{
				result = new Collection<Guid>();
			}
			else
			{
				var temp = new List<Guid>(setSNs); // Creates a copy
				result = new Collection<Guid>(temp); // Wraps the given list
			}

			return result;
		}
	}
}
