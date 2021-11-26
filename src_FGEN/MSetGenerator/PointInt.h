#pragma once

#ifdef MSETGEN_EXPORTS
#define MSETGEN_API __declspec(dllexport)
#else
#define MSETGEN_API __declspec(dllimport)
#endif

namespace FGen
{
	struct MSETGEN_API PointInt
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


