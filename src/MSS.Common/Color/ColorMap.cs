using MSS.Types;
using System;
using System.Linq;

namespace MSS.Common
{
	public class ColorMap
    {
        //private readonly ColorBand[] _colorBands;
        //private readonly ColorBand _highColorEntry;

        private readonly int[] _cutOffs;
        //private readonly int[] _prevCutOffs;
        //private readonly int[] _bucketWidths;

        private readonly int _lastCutOff;

		#region Constructor

		public ColorMap(ColorBandSet colorBandSet)
		{
            if (colorBandSet == null)
			{
                throw new ArgumentNullException(nameof(colorBandSet));
            }

            ColorBandSet = colorBandSet;
            _lastCutOff = colorBandSet.HighColorBand.PreviousCutOff;

            //_colorBands = colorBandSet.Take(colorBandSet.Count - 1).ToArray();
            //_highColorEntry = colorBandSet[^1];

            //_cutOffs = BuildCutOffs(_colorBands);

            _cutOffs = colorBandSet.Take(colorBandSet.Count - 1).Select(x => x.CutOff).ToArray();

            //var pOffsetsAndBucketWidths = BuildPrevCutOffsAndBucketWidths(_colorBands, _highColorEntry);

            //_prevCutOffs = pOffsetsAndBucketWidths.Select(x => x.Item1).ToArray();
            //_bucketWidths = pOffsetsAndBucketWidths.Select(x => x.Item2).ToArray();

            //SetEndColors(_colorBands);
        }

		#endregion

		#region Public Properties

		public ColorBandSet ColorBandSet { get; }
        public Guid SerialNumber => ColorBandSet.SerialNumber;

        #endregion

        #region Public Methods

        public byte[] GetColor(int countVal, double escapeVelocity)
        {
   //         if (countVal > 500)
			//{
   //             countVal = 500;
			//}

            byte[] result;

            var cme = GetColorBand(countVal);

            if (cme.BlendStyle == ColorBandBlendStyle.None)
            {
                result = cme.StartColor.ColorComps;
                return result;
            }

            var botBucketVal = cme.PreviousCutOff; 

            var bucketWidth = cme.BucketWidth;
            var stepFactor = (countVal + escapeVelocity - botBucketVal) / bucketWidth;
            result = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.ActualEndColor.ColorComps, stepFactor);

            return result;
        }

		#endregion

		#region Private Methods

        private ColorBand GetColorBand(int countVal)
		{
            ColorBand result;

            if (countVal >= _lastCutOff)
			{
                result = ColorBandSet.HighColorBand;
			}
            else
			{
                result = ColorBandSet[GetColorMapIndex(countVal)];
			}

            return result;
		}

		private int GetColorMapIndex(int countVal)
		{
			var newIndex = Array.BinarySearch(_cutOffs, countVal);

			if (newIndex < 0)
			{
				newIndex = ~newIndex;
			}
			else
			{
				newIndex++;
			}

			return newIndex;
		}

        private byte[] Interpolate(byte[] cStart, byte[] c1, byte[] c2, double factor)
        {
            if (factor == 0)
            {
                return cStart;
            }
            else
            {
                var rd = cStart[0] + (c2[0] - c1[0]) * factor;
                var gd = cStart[1] + (c2[1] - c1[1]) * factor;
                var bd = cStart[2] + (c2[2] - c1[2]) * factor;

                var r = (int)Math.Round(rd);
                var g = (int)Math.Round(gd);
                var b = (int)Math.Round(bd);

                if (r < 0 || r > 255)
                {
                    //console.log('Bad red value.');
                }

                if (g < 0 || g > 255)
                {
                    //console.log('Bad green value.');
                }

                if (b < 0 || b > 255)
                {
                    //console.log('Bad blue value.');
                }

                var result = new byte[3];
                result[0] = (byte)r;
                result[1] = (byte)g;
                result[2] = (byte)b;

                return result;
            }
        }

        #endregion

        #region Old Not Used

        //private int[] BuildCutOffs(ColorBand[] colorBands)
        //{
        //    var result = new int[colorBands.Length];

        //    for (var ptr = 0; ptr < colorBands.Length; ptr++)
        //    {
        //        result[ptr] = colorBands[ptr].CutOff;
        //    }

        //    return result;
        //}

        //private IList<Tuple<int, int>> BuildPrevCutOffsAndBucketWidths(ColorBand[] colorBands, ColorBand highColorEntry)
        //{
        //    var result = new List<Tuple<int, int>>();

        //    var prevCutOff = 0;

        //    for (var ptr = 0; ptr < colorBands.Length; ptr++)
        //    {
        //        var cutOff = colorBands[ptr].CutOff;
        //        result.Add(new Tuple<int, int>(prevCutOff, cutOff - prevCutOff));

        //        prevCutOff = cutOff;
        //    }

        //    result.Add(new Tuple<int, int>(prevCutOff, highColorEntry.CutOff - prevCutOff));

        //    return result;
        //}

        //   private void SetEndColors(ColorBand[] colorBands)
        //   {
        //       for (var ptr = 0; ptr < colorBands.Length; ptr++)
        //       {
        //           var cmd = colorBands[ptr];

        //           if (cmd.BlendStyle == ColorBandBlendStyle.Next)
        //           {
        //               var endColor = ptr == colorBands.Length - 1
        //	? _highColorEntry.StartColor.Clone()
        //	: colorBands[ptr + 1].StartColor.Clone();

        //colorBands[ptr] = new ColorBand(cmd.CutOff, cmd.StartColor, cmd.BlendStyle, endColor);
        //           }
        //       }
        //   }


        //     private ColorBand GetColorMapEntry(int colorMapIndex)
        //     {
        //         var result = colorMapIndex < _cutOffs.Length ? _colorBands[colorMapIndex] : _highColorEntry;
        //return result;
        //     }

        //     private byte[] GetBlendedColor(ColorBand cme, int countVal, double escapeVelocity)
        //     {
        //         byte[] result;

        //         //var cme = GetColorMapEntry(colorMapIndex);

        //         if (cme.BlendStyle == ColorBandBlendStyle.None)
        //         {
        //             result = cme.StartColor.ColorComps;
        //             return result;
        //         }

        //         var botBucketVal = _prevCutOffs[colorMapIndex];

        //         //int[] cStart;

        //         //if (countVal == botBucketVal)
        //         //{
        //         //	cStart = cme.StartColor.ColorComps;
        //         //}
        //         //else
        //         //{
        //         //	double stepFactor = (-1 + countVal - botBucketVal) / (double)cme.BucketWidth;
        //         //	cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);
        //         //}

        //         ////double stepFactor = (countVal - botBucketVal) / (double)cme.BucketWidth;
        //         ////cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

        //         //double intraStepFactor = escapeVelocity / cme.BucketWidth;
        //         //result = Interpolate(cStart, cme.StartColor.ColorComps, cme.EndColor.ColorComps, intraStepFactor);

        //         var bucketWidth = _bucketWidths[colorMapIndex];
        //         var stepFactor = (countVal + escapeVelocity - botBucketVal) / bucketWidth;
        //         result = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

        //         return result;
        //
        //     }

        #endregion
    }
}
