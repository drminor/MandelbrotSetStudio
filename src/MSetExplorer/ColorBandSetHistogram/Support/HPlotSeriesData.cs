using MongoDB.Driver.Core.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MSetExplorer.ColorBandSetHistogram.Support
{
	public class HPlotSeriesData : IEquatable<HPlotSeriesData?>
	{
		private static HPlotSeriesData _emptySingleton = new HPlotSeriesData(0);
		public static HPlotSeriesData Empty => _emptySingleton;

		private Guid _valueId;


		#region Constructors

		public HPlotSeriesData(long longLength)
		{
			DataX = BuildXVals(longLength);
			DataY = new double[longLength];

			_valueId = Guid.NewGuid();
		}

		public HPlotSeriesData(HPlotSeriesData source)
		{
			DataX = source.DataX;
			DataY = source.DataY;

			_valueId = Guid.NewGuid();
		}

		#endregion

		#region Public Properties

		public double[] DataX { get; private set; }
		public double[] DataY { get; private set; }

		public int Length => DataX.Length;
		public long LongLength => DataX.LongLength;

		#endregion

		#region Public Methods

		public bool IsEmpty()
		{
			return DataX.Length == 0;
		}

		public void SetYValues(int[] values, out bool bufferWasPreserved)
		{
			if (values.LongLength != LongLength)
			{
				Clear(values.LongLength);
				bufferWasPreserved = false;
			}
			else
			{
				bufferWasPreserved = true;
			}

			Array.Copy(values, DataY, values.LongLength);

			_valueId = Guid.NewGuid();
		}

		public void Clear()
		{
			DataX = Array.Empty<double>();
			DataY = Array.Empty<double>();

			_valueId = Guid.NewGuid();
		}

		public void Clear(long longLength)
		{
			DataX = BuildXVals(longLength);
			DataY = new double[longLength];

			_valueId = Guid.NewGuid();
		}

		#endregion

		#region Private Methods

		private double[] BuildXVals(long extent)
		{
			var result = new double[extent];

			for (int i = 0; i < extent; i++)
			{
				result[i] = i;
			}

			return result;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as HPlotSeriesData);
		}

		public bool Equals(HPlotSeriesData? other)
		{
			return other is not null &&
				   _valueId.Equals(other._valueId);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_valueId);
		}

		public static bool operator ==(HPlotSeriesData? left, HPlotSeriesData? right)
		{
			return EqualityComparer<HPlotSeriesData>.Default.Equals(left, right);
		}

		public static bool operator !=(HPlotSeriesData? left, HPlotSeriesData? right)
		{
			return !(left == right);
		}

		#endregion
	}
}
