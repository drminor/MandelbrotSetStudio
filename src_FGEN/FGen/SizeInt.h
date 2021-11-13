#pragma once

#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

namespace FGen
{
	struct FGEN_API SizeInt
	{

	public:
		SizeInt();
		SizeInt(int w, int h);

		inline int W() const
		{
			return w;
		};

		inline int H() const
		{
			return h;
		};

		~SizeInt();

	private:
		int w;
		int h;
	};
}