using MSetExplorer.ScreenHelpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for SysColors.xaml
	/// </summary>
	public partial class SysColorsWindow : Window, IHaveAppNavRequestResponse
	{
		public SysColorsWindow(AppNavRequestResponse appNavRequestResponse)
		{
			AppNavRequestResponse = appNavRequestResponse;

			InitializeComponent();

            var l = new List<ColorAndName>();

            foreach (var i in typeof(SystemColors).GetProperties())
            {
                if (i.PropertyType == typeof(Color))
                {
					var x = i.GetValue(new Color(), BindingFlags.GetProperty, null, null, null);

					if (x is Color c)
					{
						var cn = new ColorAndName
						(

							color: c,
							name: i.Name
						);
						l.Add(cn);
					}
				}
            }

            SystemColorsList.DataContext = l;
        }

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }
	}

	internal class ColorAndName
    {
		public ColorAndName(Color color, string name)
		{
			Color = color;
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public Color Color { get; set; }
        public string Name { get; set; }
    }
}
