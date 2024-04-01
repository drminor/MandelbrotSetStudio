using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MSS.Common
{
	public class ColorBandSetStore
	{

		private readonly List<ColorBandSet> _colorBandSets;
		private readonly IDictionary<int, TargetIterationColorMapRecord> _lookupColorBandSetByTargetIteration;
		private ColorBandSet _currentColorBandSet;



		public ColorBandSetStore()
			: this(new ColorBandSet())
		{ }

		public ColorBandSetStore(ColorBandSet colorBandSet)
			: this(new List<ColorBandSet> { colorBandSet },
				  JobOwnerHelper.LoadTargetIterationColorMapRecords(
					  new List<TargetIterationColorMapRecord> {
						  new TargetIterationColorMapRecord(colorBandSet.TargetIterations, colorBandSet.Id, colorBandSet.DateCreatedUtc)
					  }),
				  colorBandSet
				  )
		{ }

		public ColorBandSetStore(List<ColorBandSet> colorBandSets, List<TargetIterationColorMapRecord> targetIterationColorMapRecords, ObjectId currentColorBandSetId)
			: this(colorBandSets, 
				  JobOwnerHelper.LoadTargetIterationColorMapRecords(targetIterationColorMapRecords), 
				  colorBandSets.FirstOrDefault(x => x.Id == currentColorBandSetId) ?? throw new ArgumentException($"No ColorBandSet found in the list with Id: {currentColorBandSetId}"))
		{ }

		public ColorBandSetStore(IJobOwner jobOwner)
			: this(jobOwner.GetColorBandSets(), jobOwner.GetTargetIterationColorMapRecords(), jobOwner.CurrentJob.ColorBandSetName, jobOwner.CurrentJob.TargetIterations, jobOwner.CurrentJob.ColorBandSetVersion)
		{ }

		public ColorBandSetStore(List<ColorBandSet> colorBandSets, List<TargetIterationColorMapRecord> targetIterationColorMapRecords, string name, int targetIterations, int? version)
			: this(colorBandSets,
				JobOwnerHelper.LoadTargetIterationColorMapRecords(targetIterationColorMapRecords),
				 FindOrCreateColorBandSet(name, version, targetIterations, colorBandSets))
		{ }

		public ColorBandSetStore(List<ColorBandSet> colorBandSets, IDictionary<int, TargetIterationColorMapRecord> lookupColorBandSetByTargetIteration, ColorBandSet currentColorBandSet)
		{
			_colorBandSets = colorBandSets;
			_lookupColorBandSetByTargetIteration = lookupColorBandSetByTargetIteration;
			_currentColorBandSet = currentColorBandSet;
		}

		#region Public Properties

		public ColorBandSetResolutionStrategy ColorBandSetResolutionStrategy { get; set; }

		public ColorBandSet CurrentColorBandSet
		{
			get => _currentColorBandSet;
			set
			{
				if (value != _currentColorBandSet)
				{
					var newCbs = value;

					if (!ColorBandSetExists(newCbs))
					{
						_colorBandSets.Add(newCbs);
					}
					else
					{
						Debug.WriteLine("Not adding the new Value!!!");
					}

					MakeDefault(value);

					_currentColorBandSet = newCbs;
				}
				else
				{
					if (MakeDefault(value))
					{
						Debug.WriteLine($"WARNING: The Default ColorBandSet for {value.TargetIterations} is being set HOWEVER the CurrentColorBandSet already had this same value.");
					}
					else
					{
						Debug.WriteLine($"Not setting the CurrentColorBandSet, the CurrentColorBandSet is already updated.");
					}
				}
			}
		}

		#endregion

		#region Public Methods


		public List<TargetIterationColorMapRecord> GetTargetIterationColorMapRecords()
		{
			return _lookupColorBandSetByTargetIteration.Values.ToList();
		}

		public List<ColorBandSet> GetColorBandSets()
		{
			return _colorBandSets;
		}

		public ColorBandSet? GetColorBandSet(ObjectId id)
		{
			var result = _colorBandSets.FirstOrDefault(x => x.Id == id);
			return result;
		}

		public ColorBandSet? GetColorBandSet(string name, int targetIterations, int? version)
		{
			ColorBandSet? result;
			if (version.HasValue)
			{
				result = _colorBandSets.FirstOrDefault(x => x.Name == name && x.TargetIterations == targetIterations && x.Version == version.Value);
			}
			else
			{
				result = _colorBandSets.FirstOrDefault(x => x.Name == name && x.TargetIterations == targetIterations);
			}

			return result;
		}

		public bool ColorBandSetExists(ColorBandSet colorBandSet)
		{
			var exists = _colorBandSets.Any(x => x.TargetIterations == colorBandSet.TargetIterations && x.Name == colorBandSet.Name && x.Version == colorBandSet.Version);
			return exists;
		}

		public void Add(ColorBandSet colorBandSet, bool makeDefault)
		{
			if (!ColorBandSetExists(colorBandSet))
			{
				_colorBandSets.Add(colorBandSet);
			}

			JobOwnerHelper.AddIteratationColorMapRecord(colorBandSet, _lookupColorBandSetByTargetIteration, makeDefault);
		}

		public bool RemoveColorBandSet(ColorBandSet colorBandSet)
		{
			var result = _colorBandSets.Remove(colorBandSet);

			if (result)
			{
				if (_lookupColorBandSetByTargetIteration.TryGetValue(colorBandSet.TargetIterations, out var timcmr))
				{
					if (timcmr.ColorBandSetId == colorBandSet.Id)
					{
						_lookupColorBandSetByTargetIteration.Remove(colorBandSet.TargetIterations);
					}
				}
			}

			return result;
		}

		public ColorBandSet Load(string name, int? version, int targetIterations, out bool wasUpdated)
		{
			ColorBandSet result;

			if (ColorBandSetResolutionStrategy == ColorBandSetResolutionStrategy.PerProject)
			{
				var testResult = JobOwnerHelper.LoadColorBandSet(CurrentColorBandSet, targetIterations, "Updating the CurrentColorBandSet", _colorBandSets, _lookupColorBandSetByTargetIteration);

				if (testResult == null)
				{
					result = JobOwnerHelper.FindOrCreateColorBandSet(name, version, targetIterations, _colorBandSets, out wasUpdated, out _);
				}
				else
				{
					wasUpdated = false;
					result = testResult;
				}
			}
			else
			{
				result = JobOwnerHelper.FindOrCreateColorBandSet(name, version, targetIterations, _colorBandSets, out wasUpdated, out _);
			}

			CurrentColorBandSet = result;

			return result;
		}

		// Returns true if the default was updated.
		public bool MakeDefault(ColorBandSet colorBandSet)
		{
			if (_lookupColorBandSetByTargetIteration.TryGetValue(colorBandSet.TargetIterations, out var ticmr))
			{
				if (colorBandSet.Id == ticmr.ColorBandSetId)
				{
					return false;
				}

				_lookupColorBandSetByTargetIteration[colorBandSet.TargetIterations] = new TargetIterationColorMapRecord(colorBandSet.TargetIterations, colorBandSet.Id, colorBandSet.DateCreatedUtc);
			}
			else
			{
				_lookupColorBandSetByTargetIteration.Add(colorBandSet.TargetIterations, new TargetIterationColorMapRecord(colorBandSet.TargetIterations, colorBandSet.Id, colorBandSet.DateCreatedUtc));

			}

			return true;
		}

		#endregion

		private static ColorBandSet FindOrCreateColorBandSet(string name, int? version, int targetIterations, List<ColorBandSet> colorBandSets)
		{
			var result = JobOwnerHelper.FindOrCreateColorBandSet(name, version, targetIterations, colorBandSets, out var wasUpdated, out var wasCreated);

			if (wasCreated)
			{
				colorBandSets.Add(result);
			}

			return result;
		}
	}
}
