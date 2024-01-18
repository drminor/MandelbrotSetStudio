﻿using MSS.Types;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	internal class CbListViewItem : DependencyObject
	{
		private ColorBandSelectionType _selectionType;

		private bool _isCutoffSelected;
		private bool _isColorSelected;
		private bool _isBandSelected;

		private readonly bool _useDetailedDebug = true;

		#region Constructor

		public CbListViewItem(int colorBandIndex, ColorBand colorBand, ColorBandLayoutViewModel colorBandLayoutViewModel, string nameSuffix)
		{
			Name = $"CbListViewItem{nameSuffix}";
			ColorBand = colorBand;

			// Build the CbRectangle
			var xPosition = colorBand.PreviousCutoff ?? 0;
			var bandWidth = colorBand.BucketWidth; // colorBand.Cutoff - xPosition;
			var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

			CbRectangle = new CbRectangle(colorBandIndex, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, colorBandLayoutViewModel, nameSuffix);

			// Build the Selection Line
			var selectionLinePosition = colorBand.Cutoff;
			CbSectionLine = new CbSectionLine(colorBandIndex, selectionLinePosition, colorBandLayoutViewModel);

			// Build the Color Block
			CbColorBlock = new CbColorBlock(colorBandIndex, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, colorBandLayoutViewModel);

			_selectionType = 0;
			_isCutoffSelected = false;
			_isColorSelected = false;
			_isBandSelected = false;
			ColorBand.IsSelected = false;

			PreviousCutoff = xPosition;
			Width = bandWidth;

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
			get => CbRectangle.ColorBandIndex;
			set
			{
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

		#region Dependency Property Getters / Setters

		public string Name
		{
			get => (string)GetValue(NameProperty);
			set => SetCurrentValue(NameProperty, value);
		}

		public double Cutoff
		{
			get => (double)GetValue(CutoffProperty);
			set => SetCurrentValue(CutoffProperty, value);
		}

		public double PreviousCutoff
		{
			get => (double)GetValue(PreviousCutoffProperty);
			set => SetCurrentValue(PreviousCutoffProperty, value);
		}

		public double Width
		{
			get => (double)GetValue(WidthProperty);
			set => SetCurrentValue(WidthProperty, value);
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
					Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);
				}
				else if (e.PropertyName == nameof(ColorBand.PreviousCutoff))
				{
					// This updates the XPosition and Width -- to keep the Cutoff the same.
					Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");
					PreviousCutoff = cb.PreviousCutoff ?? 0;
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

		#region Dependency Properties

		#region Name Dependency Property

		public static readonly DependencyProperty NameProperty =
				FrameworkElement.NameProperty.AddOwner(typeof(CbListViewItem));

		#endregion

		#region Cutoff Dependency Property

		public static readonly DependencyProperty CutoffProperty =
				DependencyProperty.Register("Cutoff", typeof(double), typeof(CbListViewItem),
					new FrameworkPropertyMetadata(defaultValue: 0.0, propertyChangedCallback: Cutoff_PropertyChanged)
				);

		private static void Cutoff_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			CbListViewItem c = (CbListViewItem)o;

			var oldValue = (double)e.OldValue;
			var newValue = (double)e.NewValue;

			//Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Cutoff is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");
			Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Cutoff is changing from {oldValue.ToString("F2")} to {newValue.ToString("F2")}.");

			c.CbSectionLine.XPosition = newValue;
		}

		#endregion

		#region PreviousCutoff Dependency Property

		public static readonly DependencyProperty PreviousCutoffProperty =
				DependencyProperty.Register("PreviousCutoff", typeof(double), typeof(CbListViewItem),
					new FrameworkPropertyMetadata(defaultValue: 0.0, propertyChangedCallback: PreviousCutoff_PropertyChanged)
				);

		private static void PreviousCutoff_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			CbListViewItem c = (CbListViewItem)o;

			var oldValue = (double)e.OldValue;
			var newValue = (double)e.NewValue;

			//Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: PreviousCutOff is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");
			Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: PreviousCutoff is changing from {oldValue.ToString("F2")} to {newValue.ToString("F2")}.");

			// The ColorBand preceeding this one had its Cutoff updated.
			// This ColorBand had its PreviousCutoff (aka XPosition) updated.
			// This ColorBand's Starting Position (aka XPosition) and Width should be updated to accomodate.

			//CbRectangle.XPosition = cb.PreviousCutoff ?? 0;
			//CbRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);

			// This also updates the width
			c.CbRectangle.XPosition = newValue;
			c.CbColorBlock.XPosition = newValue;
		}

		#endregion

		#region Width Dependency Property

		public static readonly DependencyProperty WidthProperty =
				DependencyProperty.Register("Width", typeof(double), typeof(CbListViewItem),
					new FrameworkPropertyMetadata(defaultValue: 0.0, propertyChangedCallback: Width_PropertyChanged)
				);

		private static void Width_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			CbListViewItem c = (CbListViewItem)o;

			var oldValue = (double)e.OldValue;
			var newValue = (double)e.NewValue;

			//Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Width is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");
			Debug.WriteLineIf(c._useDetailedDebug, $"CbListViewItem: Width is changing from {oldValue.ToString("F2")} to {newValue.ToString("F2")}.");

			// This ColorBand had its Cutoff updated.

			// This also updates the cutoff
			c.CbRectangle.Width = newValue;
			c.CbColorBlock.Width = newValue;
		}

		#endregion

		#endregion
	}
}
