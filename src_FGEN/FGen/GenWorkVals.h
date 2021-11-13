#pragma once

#include "PointInt.h"

namespace FGen
{
	class GenWorkVals
	{
		int _width;
		int _height;
		int _len;
		unsigned int _targetIterationCnt;
		unsigned int * _counts;
		double * _zValues;
		bool * _doneFlags;

		PointInt * _curPos;
		bool _completed;

	public:
		
		GenWorkVals(int width, int height, unsigned int targetIterationCnt, unsigned int * counts, bool * doneFlags, double * zValues);

		~GenWorkVals();

		bool IsCompleted();

		bool GetNextWorkValues(PointInt &index, unsigned int &count, double * zValsBuf);
		void SaveWorkValues(PointInt index, unsigned int count, double * zValsBuf, bool doneFlag);
		void UpdateCntWithEV(PointInt index, double escapeVel);

	private:
		bool AdvanceCurPos();
		PointInt GetCurPos();

	};
}

