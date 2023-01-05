﻿namespace MSetGenP
{
	public class MathOpCounts
	{
		public MathOpCounts()
		{
			NumberOfMCarries = 0;
			NumberOfACarries = 0;
			NumberOfSplits = 0;
			NumberOfGetCarries = 0;

			NumberOfGrtrThanOpsFP = 0;
			NumberOfGrtrThanOps = 0;
		}

		public int NumberOfMCarries { get; set; }
		public int NumberOfACarries { get; set; }
		public long NumberOfSplits { get;  set; }
		public long NumberOfGetCarries { get; set; }

		public long NumberOfGrtrThanOpsFP { get; set; }
		public long NumberOfGrtrThanOps { get; set; }

	}
}
