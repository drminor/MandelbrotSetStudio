using System;
using System.Globalization;
using System.Windows.Controls;

namespace MSetExplorer
{
	public class CutoffRangeRule : ValidationRule
	{
		public int Min { get; set; }
		public int Max { get; set; }

		public CutoffRangeRule()
		{
		}

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var result = ValidationResult.ValidResult;

			try
			{
				if (value is string s && s.Length > 0)
				{
					if (int.TryParse(s, out var age))
					{
						if ( !(age > Min && age <= Max) )
						{
							return new ValidationResult(false, $"Please enter a cutoff > {Min} and <= {Max}.");
						}
					}
					else
					{
						return new ValidationResult(false, $"Invalid characters.");
					}
				}
			}
			catch (Exception e)
			{
				return new ValidationResult(false, $"Unexpected error: {e.Message}");
			}

			return result;
		}
	}
}
