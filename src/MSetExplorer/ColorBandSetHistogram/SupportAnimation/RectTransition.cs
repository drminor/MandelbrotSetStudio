using System;
using System.Windows;

namespace MSetExplorer
{
	internal class RectTransition
	{

		public RectTransition(Rect from, Rect to, double beginMs, double durationMs)
		{
			From = from;
			To = to;
			BeginMs = beginMs;
			DurationMs = durationMs;
		}

		public Rect From { get; init; }
		public Rect To { get; init; }

		public double BeginMs { get; init; }

		public double DurationMs { get; init; }

	}
}
