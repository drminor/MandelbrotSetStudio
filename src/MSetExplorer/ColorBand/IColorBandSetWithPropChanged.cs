using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IColorBandSetWithPropChanged : ICollection<IColorBandWPC>, INotifyCollectionChanged, INotifyPropertyChanged
	{

	}
}
