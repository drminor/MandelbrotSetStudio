#pragma once

#ifdef MSETGEN_EXPORTS
#define MSETGEN_API __declspec(dllexport)
#else
#define MSETGEN_API __declspec(dllimport)
#endif

namespace MSetGenerator
{
	class MSETGEN_API ddBridge
	{
	public:
		const char* test(double a, double b);

	};

}
