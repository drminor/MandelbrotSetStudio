#pragma once

struct SizeInt
{

public:
	SizeInt();
	SizeInt(int width, int height);

	inline int Width() const
	{
		return width;
	};

	inline int Height() const
	{
		return height;
	};

	~SizeInt();

private:
	int width;
	int height;
};

