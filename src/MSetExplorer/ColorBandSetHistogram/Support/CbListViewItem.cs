using MSS.Types;
using System.ComponentModel;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class CbListViewItem
	{
		private ColorBandSelectionType _selectionType;

		private bool _isCutoffSelected;
		private bool _isColorSelected;
		private bool _isBandSelected;

		private readonly bool _useDetailedDebug = false;

		#region Constructor

		public CbListViewItem(ColorBand colorBand, CbRectangle cbRectangle, CbSectionLine cbSectionLine, CbColorBlock cbColorBlock)
		{
			ColorBand = colorBand;
			CbSectionLine = cbSectionLine;
			CbColorBlock = cbColorBlock;
			CbRectangle = cbRectangle;

			_selectionType = 0;
			_isCutoffSelected = false;
			_isColorSelected = false;
			_isBandSelected = false;
			ColorBand.IsSelected = false;

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
					Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");

					// This ColorBand had its Cutoff updated.

					// This also updates the cutoff
					CbRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);
					CbColorBlock.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);
				}
				else if (e.PropertyName == nameof(ColorBand.PreviousCutoff))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");

					// The ColorBand preceeding this one had its Cutoff updated.
					// This ColorBand had its PreviousCutoff (aka XPosition) updated.
					// This ColorBand's Starting Position (aka XPosition) and Width should be updated to accomodate.

					//CbRectangle.XPosition = cb.PreviousCutoff ?? 0;
					//CbRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);

					// This also updates the width
					CbRectangle.XPosition = cb.PreviousCutoff ?? 0;
					CbColorBlock.XPosition = cb.PreviousCutoff ?? 0;
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
	}
}
