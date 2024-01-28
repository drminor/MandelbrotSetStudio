using MSS.Types;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	internal class CbListViewItem : DependencyObject
	{
		private int _colorBandIndex;

		private readonly ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private readonly CbListViewElevations _elevations;
		private CbListViewElevations? _elevationsLocal;

		private ColorBandSelectionType _selectionType;
		private bool _isCutoffSelected;
		private bool _isColorSelected;
		private bool _isBandSelected;

		private readonly bool _useDetailedDebug = true;

		#region Constructor

		public CbListViewItem(int colorBandIndex, ColorBand colorBand, CbListViewElevations elevations, ColorBandLayoutViewModel colorBandLayoutViewModel, string nameSuffix, SectionLineMovedCallback sectionLineMovedCallback)
		{
			_colorBandIndex = colorBandIndex;
			ColorBand = colorBand;

			_elevations = elevations;
			_elevationsLocal = null;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;

			Name = $"CbListViewItem{nameSuffix}";

			// Build the CbRectangle
			var x1Position = colorBand.PreviousCutoff ?? 0;
			var bandWidth = colorBand.BucketWidth;

			var blendArea = new Rect(x1Position, _elevations.BlendRectanglesElevation, bandWidth, _elevations.BlendRectanglesHeight);
			var isCurrentArea = new Rect(x1Position, _elevations.IsCurrentIndicatorsElevation, bandWidth, _elevations.IsCurrentIndicatorsHeight);
			var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

			CbRectangle = new CbRectangle(colorBandIndex, blendArea, isCurrentArea, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel);

			// Build the Selection Line
			var topArrowArea = new Rect(x1Position, _elevations.SectionLinesElevation, bandWidth, _elevations.SectionLinesHeight);
			var selectionLineArea = new Rect(x1Position, _elevations.ColorBlocksElevation, bandWidth, _elevations.ColorBlocksHeight + _elevations.BlendRectanglesHeight);

			CbSectionLine = new CbSectionLine(colorBandIndex, topArrowArea, selectionLineArea, _colorBandLayoutViewModel, sectionLineMovedCallback);

			// Build the Color Block
			var colorBlocksArea = new Rect(x1Position, elevations.ColorBlocksElevation, bandWidth, elevations.ColorBlocksHeight);
			CbColorBlock = new CbColorBlocks(colorBandIndex, colorBlocksArea, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel);

			Area = new Rect(x1Position, _elevations.Elevation, bandWidth, _elevations.ControlHeight);

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
		public CbColorBlocks CbColorBlock { get; init; }
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

		public double SectionLinePositionX => CbSectionLine.SectionLinePositionX;

		public bool IsCurrent
		{
			get => CbRectangle.IsCurrent;
			set => CbRectangle.IsCurrent = value;
		}

		public bool IsFirst => ColorBand.IsFirst;
		public bool IsLast => ColorBand.IsLast;

		public bool ElevationsAreLocal
		{
			get => _elevationsLocal != null;
			set
			{
				if (value != ElevationsAreLocal)
				{
					_elevationsLocal = value ? _elevations.Clone() : null;
				}
			}
		}

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
			bool updateHandled;

			if (!(sender is ColorBand cb))
			{
				throw new System.ArgumentException("The sender is not a ColorBand.");
			}

			if (e.PropertyName == nameof(ColorBand.Cutoff))
			{
				if (Area.IsEmpty)
				{
					Debug.WriteLine($"WARNING: The Area is Empty on update to Cutoff for Index {ColorBandIndex}: . Returning.");
					return;
				}

				// This updates the Cutoff and Width -- to keep the Previous Cutoff the same.
				var newWidth = cb.Cutoff - (cb.PreviousCutoff ?? 0);
				Area = new Rect(Area.X, Area.Y, newWidth, Area.Height);
				updateHandled = true;
			}
			else if (e.PropertyName == nameof(ColorBand.PreviousCutoff))
			{
				if (Area.IsEmpty)
				{
					Debug.WriteLine($"WARNING: The Area is Empty on update to PreviousCutoff for Index {ColorBandIndex}: . Returning.");
					return;
				}

				// This updates the XPosition and Width -- to keep the Cutoff the same.
				var newX1 = cb.PreviousCutoff ?? 0;
				var newWidth = Area.Right - newX1;
				Area = new Rect(newX1, Area.Y, newWidth, Area.Height);
				updateHandled = true;
			}
			else if (e.PropertyName == nameof(ColorBand.StartColor))
			{
				CbColorBlock.StartColor = cb.StartColor;
				CbRectangle.StartColor = cb.StartColor;
				updateHandled = true;
			}
			else if (e.PropertyName == nameof(ColorBand.ActualEndColor))
			{
				CbColorBlock.EndColor = cb.ActualEndColor;
				CbRectangle.EndColor = cb.ActualEndColor;
				updateHandled = true;
			}
			else if (e.PropertyName == nameof(ColorBand.BlendStyle))
			{
				CbColorBlock.Blend = cb.BlendStyle != ColorBandBlendStyle.None;
				CbRectangle.Blend = CbColorBlock.Blend;
				updateHandled = true;
			}
			else
			{
				updateHandled = false;
			}

			if (updateHandled)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");
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

		public Rect ColorBlockArea
		{
			get => (Rect)GetValue(ColorBlockAreaProperty);
			set => SetCurrentValue(ColorBlockAreaProperty, value);
		}

		public Rect BlendedColorArea
		{
			get => (Rect)GetValue(BlendedColorAreaProperty);
			set => SetCurrentValue(BlendedColorAreaProperty, value);
		}

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

			if (c._elevationsLocal != null)
			{
				var elevations = c.GetElevations(c._elevationsLocal, oldValue, newValue);
				c.UpdateDisplay(newValue, elevations);
			}
			else
			{
				c.UpdateDisplay(newValue, c._elevations);
			}
		}

		private CbListViewElevations GetElevations(CbListViewElevations elevationsLocal, Rect oldValue, Rect newValue)
		{
			CbListViewElevations elevations;

			if (ScreenTypeHelper.IsDoubleChanged(oldValue.Height, newValue.Height) || ScreenTypeHelper.IsDoubleChanged(oldValue.Y, newValue.Y))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Setting the Elevation and Height for item at {ColorBandIndex} to {newValue.Y} and {newValue.Height}.");

				elevationsLocal.SetElevationAndHeight(newValue.Y, newValue.Height);
				elevations = elevationsLocal;
			}
			else
			{
				elevations = _elevations;
			}

			return elevations;
		}

		private void UpdateDisplay(Rect newValue, CbListViewElevations elevations)
		{
			if (!ScreenTypeHelper.IsDoubleChanged(newValue.Right, 60, 2))
			{
				Debug.WriteLine($"CbListViewItem is having its Area value set. The X2 position = 60.");
			}

			CbSectionLine.TopArrowRectangleArea = new Rect(newValue.Left, elevations.SectionLinesElevation, newValue.Width, elevations.SectionLinesHeight);
			CbSectionLine.SectionLineRectangleArea = new Rect(newValue.Left, elevations.ColorBlocksElevation, newValue.Width, elevations.ColorBlocksHeight + elevations.BlendRectanglesHeight);

			CbColorBlock.ColorBlocksArea = new Rect(newValue.Left, elevations.ColorBlocksElevation, newValue.Width, elevations.ColorBlocksHeight);

			CbRectangle.BlendRectangleArea = new Rect(newValue.Left, elevations.BlendRectanglesElevation, newValue.Width, elevations.BlendRectanglesHeight);
			CbRectangle.IsCurrentArea = new Rect(newValue.Left, elevations.IsCurrentIndicatorsElevation, newValue.Width, elevations.IsCurrentIndicatorsHeight);
		}

		#endregion

		#region ColorBlockArea Property

		public static readonly DependencyProperty ColorBlockAreaProperty =
		DependencyProperty.Register("ColorBlockArea", typeof(Rect), typeof(CbListViewItem),
			new FrameworkPropertyMetadata(defaultValue: Rect.Empty, propertyChangedCallback: ColorBlockArea_PropertyChanged)
		);

		private static void ColorBlockArea_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			CbListViewItem c = (CbListViewItem)o;

			var oldValue = (Rect)e.OldValue;
			var newValue = (Rect)e.NewValue;

			Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: ColorBlockArea for {c.ColorBandIndex} is changing from {oldValue} to {newValue}.");

			if (newValue.IsEmpty)
			{
				return;
			}

			if (c.CbColorBlock.CbColorPairProxy == null)
			{
				Debug.WriteLine($"WARNING: The CbColorPairProxy is null on SetColorBlockArea for ColorBandIndex: {c.ColorBandIndex}.");
				return;
			}

			c.CbColorBlock.CbColorPairProxy.Container = newValue;
		}

		#endregion

		#region BlendedColorArea Property

		public static readonly DependencyProperty BlendedColorAreaProperty =
		DependencyProperty.Register("BlendedColorArea", typeof(Rect), typeof(CbListViewItem),
			new FrameworkPropertyMetadata(defaultValue: Rect.Empty, propertyChangedCallback: BlendedColorArea_PropertyChanged)
		);

		private static void BlendedColorArea_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			CbListViewItem c = (CbListViewItem)o;

			var oldValue = (Rect)e.OldValue;
			var newValue = (Rect)e.NewValue;

			Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: BlendedColorArea for {c.ColorBandIndex} is changing from {oldValue} to {newValue}.");

			if (newValue.IsEmpty)
			{
				return;
			}

			if (c.CbRectangle.CbBlendedColorPairProxy == null)
			{
				Debug.WriteLine($"WARNING: The CbBlendedColorPairProxy is null on SetBlendedColorArea for ColorBandIndex: {c.ColorBandIndex}.");
				return;
			}

			c.CbRectangle.CbBlendedColorPairProxy.Container = newValue;
		}

		#endregion

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
