#pragma once

struct PointInt
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



