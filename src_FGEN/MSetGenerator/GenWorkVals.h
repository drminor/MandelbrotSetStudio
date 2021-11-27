#pragma once
#include "PointInt.h"

class GenWorkVals
{
	int _width;
	int _height;
	int _len;
	int _targetIterationCnt;
	int* _counts;
	double* _zValues;
	bool* _doneFlags;

	PointInt* _curPos;
	bool _completed;

public:

	GenWorkVals(int width, int height, int targetIterationCnt, int* counts, bool* doneFlags, double* zValues);

	~GenWorkVals();

	bool IsCompleted();

	bool GetNextWorkValues(PointInt& index, int& count, double* zValsBuf);
	void SaveWorkValues(PointInt index, int count, double* zValsBuf, bool doneFlag);
	void UpdateCntWithEV(PointInt index, double escapeVel);

private:
	bool AdvanceCurPos();
	PointInt GetCurPos();

};

