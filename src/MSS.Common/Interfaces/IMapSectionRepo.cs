using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapSectionRepo
	{
		MapSection GetMapSection(string subdivisionId, SizeInt blockPosition);
	}
}
