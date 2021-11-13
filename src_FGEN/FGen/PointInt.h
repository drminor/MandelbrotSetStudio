#pragma once

#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

namespace FGen
{
	struct FGEN_API PointInt
	{

	public:
		PointInt();
		PointInt(int x, int y);

		void SetXY(int nx, int ny);

		inline int X() const
		{
			return x;
		};

		inline int Y() const
		{
			return y;
		};

		~PointInt();

	private:
		int x;
		int y;
	}; 

}

