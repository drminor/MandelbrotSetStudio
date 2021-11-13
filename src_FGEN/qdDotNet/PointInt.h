#pragma once

namespace qdDotNet
{
	public value struct PointInt
	{

	public:
		PointInt(int x, int y);

		inline int X()
		{
			return x;
		};

		inline int Y()
		{
			return y;
		};

		inline PointInt Translate(PointInt amount)
		{
			return PointInt(x + amount.x, y + amount.y);
		}

	private:
		int x;
		int y;
	};
}

