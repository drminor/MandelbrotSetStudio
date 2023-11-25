﻿using MSS.Types;
using System;
using System.Windows;

namespace ImageBuilderWPF
{
	public interface IImageWriter : IDisposable
	{
		public Action<MapSectionVectors> ReturnMapSectionVectors { set; }

		void Save();
		void SaveAndClose();
		void Close();

		void WriteBlock(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, byte[] imageBuffer, int destX, int destY);
	}
}