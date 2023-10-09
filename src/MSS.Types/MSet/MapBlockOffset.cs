using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;

namespace MSS.Types.MSet
{
	[DataContract]
	public class MapBlockOffset : IEquatable<MapBlockOffset?>, IEqualityComparer<MapBlockOffset?>
	{
		[DataMember(Order = 1)]
		public long XHi;

		[DataMember(Order = 2)]
		public long XLo;

		[DataMember(Order = 3)]
		public long YHi;

		[DataMember(Order = 4)]
		public long YLo;

		#region Constructors

		public MapBlockOffset() : this(0, 0, 0, 0) { }

		public MapBlockOffset(long xHi, long xLo, long yHi, long yLo)
		{
			XHi = xHi;
			XLo = xLo;
			YHi = yHi;
			YLo = yLo;
		}

		public MapBlockOffset(long[] x, long[] y)
		{
			Debug.Assert(x.Length == 2 && y.Length == 2, "The X and Y arrays must have exactly 2 elements.");

			XHi = x[0];
			XLo = x[1];
			YHi = y[0];
			YLo = y[1];
		}

		#endregion

		#region Public Methods

		public (BigInteger x, BigInteger y) GetBigIntegers()
		{
			var x = BigIntegerHelper.FromLongs(new long[] { XHi, XLo });
			var y = BigIntegerHelper.FromLongs(new long[] { YHi, YLo });

			return (x, y);
		}

		#endregion

		#region To String 

		public override string? ToString()
		{
			var sb = new StringBuilder();

			sb.Append("X:");
			AppendStringVals(XHi, XLo, sb);

			sb.Append(", Y:");
			AppendStringVals(YHi, YLo, sb);

			return sb.ToString();
		}

		private void AppendStringVals(long hi, long lo, StringBuilder sb)
		{
			if (hi == 0)
			{
				sb.Append(lo.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				sb.Append(hi.ToString(CultureInfo.InvariantCulture))
					.Append(", ")
					.Append(lo.ToString(CultureInfo.InvariantCulture));
			}
		}

		#endregion

		#region IEquatable / IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as MapBlockOffset);
		}

		public bool Equals(MapBlockOffset? other)
		{
			return other is not null &&
				   XHi == other.XHi &&
				   XLo == other.XLo &&
				   YHi == other.YHi &&
				   YLo == other.YLo;
		}

		public bool Equals(MapBlockOffset? x, MapBlockOffset? y)
		{
			if (x == null)
			{
				return y == null;
			}
			else
			{
				return x.Equals(y);
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(XHi, XLo, YHi, YLo);
		}

		public int GetHashCode([DisallowNull] MapBlockOffset? obj)
		{
			return HashCode.Combine(obj.XHi, obj.XLo, obj.YHi, obj.YLo);
		}

		public static bool operator ==(MapBlockOffset? left, MapBlockOffset? right)
		{
			return EqualityComparer<MapBlockOffset>.Default.Equals(left, right);
		}

		public static bool operator !=(MapBlockOffset? left, MapBlockOffset? right)
		{
			return !(left == right);
		}

		#endregion
	}
}
