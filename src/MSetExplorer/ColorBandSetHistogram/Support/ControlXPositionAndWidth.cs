using System;

namespace MSetExplorer
{
	public struct ControlXPositionAndWidth : IEquatable<ControlXPositionAndWidth>
	{
		public double XPosition { get; init; }
		public double Width { get; init; }

		public ControlXPositionAndWidth(double xPosition, double width)
		{
			XPosition = xPosition;
			Width = width;
		}

		#region IEquatable and ToString Support

		public override string? ToString()
		{
			var result = $"XPosition: {XPosition}, Width: {Width}";
			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ControlXPositionAndWidth width && Equals(width);
		}

		public bool Equals(ControlXPositionAndWidth other)
		{
			//return XPosition == other.XPosition && Width == other.Width;

			var isDifferent = ScreenTypeHelper.IsDoubleChanged(XPosition, other.XPosition) || ScreenTypeHelper.IsDoubleChanged(Width, other.Width);

			return !isDifferent;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(XPosition, Width);
		}

		public static bool operator ==(ControlXPositionAndWidth left, ControlXPositionAndWidth right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ControlXPositionAndWidth left, ControlXPositionAndWidth right)
		{
			return !(left == right);
		}

		#endregion
	}
}
