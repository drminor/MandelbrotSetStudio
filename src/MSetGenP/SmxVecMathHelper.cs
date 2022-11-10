
namespace MSetGenP
{
	public class SmxVecMathHelper
	{
		private int _precision;
		#region Constructors

		public SmxVecMathHelper(int precision)
		{
			Precision = precision;
		}

		#endregion

		#region Public Properties

		public int Precision
		{
			get => _precision;
			set
			{
				_precision = value;
				Limbs = SmxMathHelper.GetLimbsCount(_precision);
			}
		}

		public int Limbs { get; private set; }

		#endregion










	}
}
