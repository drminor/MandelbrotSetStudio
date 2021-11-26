#pragma once

struct SizeInt
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

