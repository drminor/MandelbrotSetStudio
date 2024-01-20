using MSS.Types;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	internal class CbListViewItem : DependencyObject
	{
		private ColorBandSelectionType _selectionType;

		private int _colorBandIndex;
		private bool _isCutoffSelected;
		private bool _isColorSelected;
		private bool _isBandSelected;

		private readonly bool _useDetailedDebug = true;

		#region Constructor

		public CbListViewItem(int colorBandIndex, ColorBand colorBand, ColorBandLayoutViewModel colorBandLayoutViewModel, string nameSuffix)
		{
			_colorBandIndex = colorBandIndex;
			ColorBand = colorBand;
			Name = $"CbListViewItem{nameSuffix}";

			// Build the CbRectangle
			var xPosition = colorBand.PreviousCutoff ?? 0;
			var bandWidth = colorBand.BucketWidth;

			var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

			CbRectangle = new CbRectangle(colorBandIndex, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, colorBandLayoutViewModel);

			// Build the Selection Line
			var selectionLinePosition = colorBand.Cutoff;
			CbSectionLine = new CbSectionLine(colorBandIndex, selectionLinePosition, colorBandLayoutViewModel);

			// Build the Color Block
			CbColorBlock = new CbColorBlock(colorBandIndex, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, colorBandLayoutViewModel);

			Area = new Rect(xPosition, 0, bandWidth, colorBandLayoutViewModel.ControlHeight);

			_selectionType = 0;
			_isCutoffSelected = false;
			_isColorSelected = false;
			_isBandSelected = false;
			ColorBand.IsSelected = false;

			Opacity = 1.0;

			ColorBand.PropertyChanged += ColorBand_PropertyChanged;
		}

		#endregion

		#region Public Properties

		public ColorBand ColorBand { get; init; }
		public CbSectionLine CbSectionLine { get; init; }
		public CbColorBlock CbColorBlock { get; init; }
		public CbRectangle CbRectangle { get; init; }

		public int ColorBandIndex
		{
			get => _colorBandIndex;
			set
			{
				_colorBandIndex = value;
				CbRectangle.ColorBandIndex = value;
				CbSectionLine.ColorBandIndex = value;
				CbColorBlock.ColorBandIndex = value;
			}
		}

		public double SectionLinePosition => CbSectionLine.SectionLinePosition;

		public bool IsCurrent
		{
			get => CbRectangle.IsCurrent;
			set => CbRectangle.IsCurrent = value;
		}

		public bool IsFirst => ColorBand.IsFirst;
		public bool IsLast => ColorBand.IsLast;

		#endregion

		#region Public Properties - Selection Support

		public ColorBandSelectionType SelectionType
		{
			get => _selectionType;

			set
			{
				if (value != _selectionType)
				{
					_selectionType = value;

					switch (_selectionType)
					{
						case ColorBandSelectionType.None:
							IsCutoffSelected = false;
							IsColorSelected = false;
							IsBandSelected = false;
							break;
						case ColorBandSelectionType.Cutoff:
							IsCutoffSelected = true;
							IsColorSelected = false;
							IsBandSelected = false;
							break;
						case ColorBandSelectionType.Color:
							IsCutoffSelected = false;
							IsColorSelected = true;
							IsBandSelected = false;
							break;
						case ColorBandSelectionType.Band:
							IsCutoffSelected = false;
							IsColorSelected = false;
							IsBandSelected = true;
							break;
						default:
							break;
					}
				}
			}
		}

		public bool IsCutoffSelected
		{
			get => _isCutoffSelected;
			set
			{
				if (value != _isCutoffSelected)
				{
					_isCutoffSelected = value;	
					ColorBand.IsSelected = value;

					CbSectionLine.IsSelected = value;
				}
			}
		}

		public bool IsColorSelected
		{
			get => _isColorSelected;
			set
			{
				if (value != _isColorSelected)
				{
					_isColorSelected = value;
					ColorBand.IsSelected = value;

					CbColorBlock.IsSelected = value;
				}
			}
		}

		public bool IsBandSelected
		{
			get => _isBandSelected;
			set
			{
				if (value != _isBandSelected)
				{
					_isBandSelected = value;
					ColorBand.IsSelected = value;

					CbRectangle.IsSelected = value;
				}
			}
		}

		public bool IsItemSelected => ColorBand.IsSelected; // IsCutoffSelected | IsColorSelected | IsColorBandSelected;

		public bool SectionLineIsUnderMouse
		{
			get => CbSectionLine.IsUnderMouse;
			set => CbSectionLine.IsUnderMouse = value;
		}

		#endregion

		#region Event Handlers

		private void ColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is ColorBand cb)
			{
				//Debug.WriteLine($"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");

				if (e.PropertyName == nameof(ColorBand.Cutoff))
				{
					// This updates the Cutoff and Width -- to keep the Previous Cutoff the same.
					Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");
					var newWidth = cb.Cutoff - (cb.PreviousCutoff ?? 0);

					Area = new Rect(Area.X, Area.Y, newWidth, Area.Height);
				}
				else if (e.PropertyName == nameof(ColorBand.PreviousCutoff))
				{
					// This updates the XPosition and Width -- to keep the Cutoff the same.
					Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");

					var newX1 = cb.PreviousCutoff ?? 0;
					var newWidth = Area.Right - newX1;

					Area = new Rect(newX1, Area.Y, newWidth, Area.Height);
				}
				else if (e.PropertyName == nameof(ColorBand.StartColor))
				{
					CbColorBlock.StartColor = cb.StartColor;
					CbRectangle.StartColor = cb.StartColor;
				}
				else if (e.PropertyName == nameof(ColorBand.ActualEndColor))
				{
					CbColorBlock.EndColor = cb.ActualEndColor;
					CbRectangle.EndColor = cb.ActualEndColor;
				}
				else
				{
					if (e.PropertyName == nameof(ColorBand.BlendStyle))
					{
						CbColorBlock.HorizontalBlend = cb.BlendStyle != ColorBandBlendStyle.None;
						CbRectangle.Blend = CbColorBlock.HorizontalBlend;
					}
				}

			}
		}

		#endregion

		#region Public Methods

		public void SetIsRectangleUnderMouse(bool newValue, ColorBandSetEditMode editMode)
		{
			if (newValue)
			{
				if (editMode == ColorBandSetEditMode.Cutoffs)
				{
					CbColorBlock.IsUnderMouse = false;
					CbRectangle.IsUnderMouse = false;
				}
				else if (editMode == ColorBandSetEditMode.Colors)
				{
					CbColorBlock.IsUnderMouse = true;
					CbRectangle.IsUnderMouse = false;
				}
				else
				{
					CbColorBlock.IsUnderMouse = false;
					CbRectangle.IsUnderMouse = true;
				}
			}
			else
			{
				CbColorBlock.IsUnderMouse = false;
				CbRectangle.IsUnderMouse = false;
			}
		}

		public void SetIsSectionLineUnderMouse(bool newValue)
		{
			CbSectionLine.IsUnderMouse = newValue;
		}

		public void TearDown()
		{
			ColorBand.PropertyChanged -= ColorBand_PropertyChanged;
			CbSectionLine.TearDown();
			CbColorBlock.TearDown();
			CbRectangle.TearDown();
		}

		#endregion

		#region Dependency Property Getters / Setters

		public string Name
		{
			get => (string)GetValue(NameProperty);
			set => SetCurrentValue(NameProperty, value);
		}

		public Rect Area
		{
			get => (Rect)GetValue(AreaProperty);
			set => SetCurrentValue(AreaProperty, value);
		}

		//public double Cutoff
		//{
		//	get => (double)GetValue(CutoffProperty);
		//	set => SetCurrentValue(CutoffProperty, value);
		//}

		//public double PreviousCutoff
		//{
		//	get => (double)GetValue(PreviousCutoffProperty);
		//	set => SetCurrentValue(PreviousCutoffProperty, value);
		//}

		//public double Width
		//{
		//	get => (double)GetValue(WidthProperty);
		//	set => SetCurrentValue(WidthProperty, value);
		//}

		public double Opacity
		{
			get => (double)GetValue(OpacityProperty);
			set => SetCurrentValue(OpacityProperty, value);
		}

		#endregion

		#region Dependency Properties

		#region Name Dependency Property

		public static readonly DependencyProperty NameProperty =
				FrameworkElement.NameProperty.AddOwner(typeof(CbListViewItem));

		#endregion

		#region Area Dependency Property

		public static readonly DependencyProperty AreaProperty =
				DependencyProperty.Register("Area", typeof(Rect), typeof(CbListViewItem),
					new FrameworkPropertyMetadata(defaultValue: Rect.Empty, propertyChangedCallback: Area_PropertyChanged)
				);

		private static void Area_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			CbListViewItem c = (CbListViewItem)o;

			var oldValue = (Rect)e.OldValue;
			var newValue = (Rect)e.NewValue;

			Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Area for {c.ColorBandIndex} is changing from {oldValue} to {newValue}.");

			if (newValue.IsEmpty)
			{
				return;
			}

			c.CbSectionLine.XPosition = newValue.Right;
			c.CbRectangle.Area = newValue;
			c.CbColorBlock.Area = newValue;
		}

		#endregion

		//#region Cutoff Dependency Property

		//public static readonly DependencyProperty CutoffProperty =
		//		DependencyProperty.Register("Cutoff", typeof(double), typeof(CbListViewItem),
		//			new FrameworkPropertyMetadata(defaultValue: 0.0, propertyChangedCallback: Cutoff_PropertyChanged)
		//		);

		//private static void Cutoff_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	CbListViewItem c = (CbListViewItem)o;

		//	var oldValue = (double)e.OldValue;
		//	var newValue = (double)e.NewValue;

		//	Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Cutoff for {c.ColorBandIndex} is changing from {oldValue.ToString("F2")} to {newValue.ToString("F2")}.");

		//	c.CbSectionLine.XPosition = newValue;
		//}

		//#endregion

		//#region PreviousCutoff Dependency Property

		//public static readonly DependencyProperty PreviousCutoffProperty =
		//		DependencyProperty.Register("PreviousCutoff", typeof(double), typeof(CbListViewItem),
		//			new FrameworkPropertyMetadata(defaultValue: 0.0, propertyChangedCallback: PreviousCutoff_PropertyChanged)
		//		);

		//private static void PreviousCutoff_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	CbListViewItem c = (CbListViewItem)o;

		//	var oldValue = (double)e.OldValue;
		//	var newValue = (double)e.NewValue;

		//	Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: PreviousCutoff for {c.ColorBandIndex} is changing from {oldValue.ToString("F2")} to {newValue.ToString("F2")}.");

		//	// The ColorBand preceeding this one had its Cutoff updated.
		//	// This ColorBand had its PreviousCutoff (aka XPosition) updated.
		//	// This ColorBand's Starting Position (aka XPosition) and Width should be updated to accomodate.

		//	//CbRectangle.XPosition = cb.PreviousCutoff ?? 0;
		//	//CbRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);

		//	// This also updates the width
		//	c.CbRectangle.XPosition = newValue;
		//	c.CbColorBlock.XPosition = newValue;
		//}

		//#endregion

		//#region Width Dependency Property

		//public static readonly DependencyProperty WidthProperty =
		//		DependencyProperty.Register("Width", typeof(double), typeof(CbListViewItem),
		//			new FrameworkPropertyMetadata(defaultValue: 0.0, propertyChangedCallback: Width_PropertyChanged)
		//		);

		//private static void Width_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	CbListViewItem c = (CbListViewItem)o;

		//	var oldValue = (double)e.OldValue;
		//	var newValue = (double)e.NewValue;

		//	Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Width for {c.ColorBandIndex} is changing from {oldValue.ToString("F2")} to {newValue.ToString("F2")}.");

		//	// This ColorBand had its Cutoff updated.

		//	// This also updates the cutoff
		//	c.CbRectangle.Width = newValue;
		//	c.CbColorBlock.Width = newValue;
		//}

		//#endregion

		#region Opacity Dependency Property

		public static readonly DependencyProperty OpacityProperty =
				DependencyProperty.Register("Opacity", typeof(double), typeof(CbListViewItem),
					new FrameworkPropertyMetadata(defaultValue: 1.0, propertyChangedCallback: Opacity_PropertyChanged)
				);

		private static void Opacity_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			CbListViewItem c = (CbListViewItem)o;

			var oldValue = (double)e.OldValue;
			var newValue = (double)e.NewValue;

			//Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Width is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");
			Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Opacity for {c.ColorBandIndex} is changing from {oldValue.ToString("F2")} to {newValue.ToString("F2")}.");

			c.CbSectionLine.Opacity = newValue;
			c.CbColorBlock.Opacity = newValue;
			c.CbRectangle.Opacity = newValue;
		}

		#endregion

		#endregion
	}
}
