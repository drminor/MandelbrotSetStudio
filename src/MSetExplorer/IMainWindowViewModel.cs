using MSS.Types;
using MSS.Types.MSet;

namespace MSetExplorer
{
	internal interface IMainWindowViewModel
	{
		Project Project { get; }

		IMapLoaderJobStack MapLoaderJobStack { get;}

		IMapDisplayViewModel MapDisplayViewModel { get; }
	}
}