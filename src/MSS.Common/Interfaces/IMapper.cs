
namespace MSS.Common
{
	public interface IMapper<S,T>
	{
		T MapTo(S source);
		S MapFrom(T target);
	}
}
