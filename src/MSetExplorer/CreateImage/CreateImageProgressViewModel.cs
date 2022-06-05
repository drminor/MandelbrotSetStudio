using ImageBuilder;
using MSS.Types;
using System;

namespace MSetExplorer
{
	public class CreateImageProgressViewModel
	{
		private readonly PngBuilder _pngBuilder;

		#region Constructor

		public CreateImageProgressViewModel(PngBuilder pngBuilder)
		{
			_pngBuilder = pngBuilder;

			Progress = new Progress<int>();
		}

		#endregion

		#region Public Properties

		public Progress<int> Progress { get; init; }

		public string? ImageFilePath { get; private set; }
		public Poster? Poster { get; private set; }
		public bool UseEscapeVelocities { get; private set; }

		#endregion

		#region Public Methods

		public void CreateImage(string imageFilePath, Poster poster, bool useEscapeVelocities)
		{
			ImageFilePath = imageFilePath;
			Poster = poster;
			UseEscapeVelocities = useEscapeVelocities;

			_pngBuilder.Build(imageFilePath, poster, useEscapeVelocities);
		}

		#endregion

	}
}
