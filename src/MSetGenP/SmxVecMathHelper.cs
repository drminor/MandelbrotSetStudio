using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSetGenP
{
	internal class SmxVecMathHelper
	{
		public SmxVecMathHelper(int precision)
		{
			Precision = precision;
			Limbs = SmxMathHelper.GetLimbsCount(precision);
		}

		public SmxVecMathHelper(int precision, int limbs)
		{
			Precision = precision;
			Limbs = limbs;
		}
		
		public int Precision { get; }
		public int Limbs { get; }








	}
}
