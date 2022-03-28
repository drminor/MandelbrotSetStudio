using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class ColorBandJr : INotifyPropertyChanged/*, IEquatable<ColorBandJr?>, IEqualityComparer<ColorBandJr>, ICloneable*/, IEditableObject
	{
		private int _cutOff;
		private int _previousCutOff;
		private double _percentage;

		private ColorBandJr? _copy;
		private bool _isInEditMode;

		#region Constructor

		public ColorBandJr()
		{
			_cutOff = 0;
			_previousCutOff = 0;
			_percentage = 0;

			_copy = null;
			_isInEditMode = false;
		}

		public ColorBandJr(int cutOff, int previousCutOff, double percentage)
		{
			_cutOff= cutOff;
			_previousCutOff = previousCutOff;
			_percentage = percentage;
		}

		#endregion

		#region Public Properties

		public int CutOff
		{
			get => _cutOff;
			set
			{
				if (value != _cutOff)
				{
					_cutOff = value;
					OnPropertyChanged();
				}
			}
		}

		public string CutOffA
		{
			get => "hi";
			set
			{
			}
		}

		public int PreviousCutOff
		{
			get => _previousCutOff;
			set
			{
				if (value != _previousCutOff)
				{
					_previousCutOff = value;
					OnPropertyChanged();
				}
			}
		}

		public double Percentage
		{
			get => _percentage;
			set
			{
				if (value != _percentage)
				{
					_percentage = value;
					OnPropertyChanged();
				}
			}
		}

		public int BucketWidth => CutOff - PreviousCutOff;

		#endregion

		#region Public Methods

		//object ICloneable.Clone()
		//{
		//	return Clone();
		//}

		public ColorBandJr Clone()
		{
			var result = new ColorBandJr(CutOff, PreviousCutOff, Percentage);

			return result;
		}

		#endregion

		#region IEditable Object Support

		public void BeginEdit()
		{
			if (_copy == null)
			{ 
				_copy = new ColorBandJr(CutOff, PreviousCutOff, Percentage);
			}

			IsInEditMode = true;
		}

		public void CancelEdit()
		{
			if (_copy != null)
			{
				CutOff = _copy.CutOff;
				PreviousCutOff = _copy.PreviousCutOff;
				Percentage = _copy.Percentage;
			}
			else
			{
				throw new InvalidOperationException("_copy is null on Cancel Edit.");
			}

			IsInEditMode = false;
		}

		public void EndEdit()
		{
			_copy = null;
			IsInEditMode = false;
		}

		public bool IsInEditMode
		{
			get { return _isInEditMode; }
			private set
			{
				if (_isInEditMode != value)
				{
					_isInEditMode = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		public override string ToString()
		{
			return $"CutOff: {_cutOff}, Prev: {_previousCutOff}, Percentage: {_percentage}";
		}

		#region IEquatable and IEqualityComparer Support

		//public override bool Equals(object? obj)
		//{
		//	return Equals(obj as ColorBandJr);
		//}

		//public bool Equals(ColorBandJr? other)
		//{
		//	var result = other != null
		//		&& _cutOff == other.CutOff
		//		&& _previousCutOff == other.PreviousCutOff
		//		&& _percentage == other.Percentage;

		//	return result;
		//}

		//public override int GetHashCode()
		//{
		//	return HashCode.Combine(_cutOff, _previousCutOff, _percentage);
		//}

		//public bool Equals(ColorBandJr? x, ColorBandJr? y)
		//{
		//	if (x is null)
		//	{
		//		return y is null;
		//	}
		//	else
		//	{
		//		return x.Equals(y);
		//	}
		//}

		//public int GetHashCode([DisallowNull] ColorBandJr obj)
		//{
		//	return obj.GetHashCode();
		//}

		//public static bool operator ==(ColorBandJr? left, ColorBandJr? right)
		//{
		//	return EqualityComparer<ColorBandJr>.Default.Equals(left, right);
		//}

		//public static bool operator !=(ColorBandJr? left, ColorBandJr? right)
		//{
		//	return !(left == right);
		//}

		#endregion

		#region NotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
