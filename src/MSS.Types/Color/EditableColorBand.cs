using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class EditableColorBand : INotifyPropertyChanged/*, IEquatable<ColorBandJr?>, IEqualityComparer<ColorBandJr>, ICloneable*/, IEditableObject
	{
		private int _cutOff;
		private int _previousCutoff;
		private double _percentage;

		private EditableColorBand? _copy;
		private bool _isInEditMode;

		#region Constructor

		public EditableColorBand()
		{
			_cutOff = 0;
			_previousCutoff = 0;
			_percentage = 0;

			_copy = null;
			_isInEditMode = false;
		}

		public EditableColorBand(int cutOff, int previousCutoff, double percentage)
		{
			_cutOff= cutOff;
			_previousCutoff = previousCutoff;
			_percentage = percentage;
		}

		#endregion

		#region Public Properties

		public int Cutoff
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

		public string CutoffA
		{
			get => "hi";
			set
			{
			}
		}

		public int PreviousCutoff
		{
			get => _previousCutoff;
			set
			{
				if (value != _previousCutoff)
				{
					_previousCutoff = value;
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

		public int BucketWidth => Cutoff - PreviousCutoff;

		#endregion

		#region Public Methods

		//object ICloneable.Clone()
		//{
		//	return Clone();
		//}

		public EditableColorBand Clone()
		{
			var result = new EditableColorBand(Cutoff, PreviousCutoff, Percentage);

			return result;
		}

		#endregion

		#region IEditable Object Support

		public void BeginEdit()
		{
			if (_copy == null)
			{ 
				_copy = new EditableColorBand(Cutoff, PreviousCutoff, Percentage);
			}

			IsInEditMode = true;
		}

		public void CancelEdit()
		{
			if (_copy != null)
			{
				Cutoff = _copy.Cutoff;
				PreviousCutoff = _copy.PreviousCutoff;
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
			return $"Cutoff: {_cutOff}, Prev: {_previousCutoff}, Percentage: {_percentage}";
		}

		#region IEquatable and IEqualityComparer Support

		//public override bool Equals(object? obj)
		//{
		//	return Equals(obj as ColorBandJr);
		//}

		//public bool Equals(ColorBandJr? other)
		//{
		//	var result = other != null
		//		&& _cutOff == other.Cutoff
		//		&& _previousCutoff == other.PreviousCutoff
		//		&& _percentage == other.Percentage;

		//	return result;
		//}

		//public override int GetHashCode()
		//{
		//	return HashCode.Combine(_cutOff, _previousCutoff, _percentage);
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
