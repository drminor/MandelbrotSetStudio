#include "stdafx.h"
#include "GenWorkVals.h"

namespace FGen
{
	GenWorkVals::GenWorkVals(int width, int height, unsigned int targetIterationCnt, unsigned int * counts, bool * doneFlags, double * zValues)
	{
		_width = width;
		_height = height;
		_len = width * height;

		_targetIterationCnt = targetIterationCnt;
		_counts = counts;
		_doneFlags = doneFlags;
		_zValues = zValues;

		_curPos = new PointInt(0, 0);
		_completed = false;
	}

	GenWorkVals::~GenWorkVals()
	{
		delete _curPos;
	}

	bool GenWorkVals::GetNextWorkValues(PointInt &index, unsigned int & count, double * zValsBuf) 
	{
		if (_completed) return false;

		int vPtr = _curPos->Y() * _width + _curPos->X();
		unsigned int cntVal = _counts[vPtr] / 10000;
		bool needsWork = !_doneFlags[vPtr] && cntVal < _targetIterationCnt;

		while (!needsWork && !_completed) {
			if (!AdvanceCurPos()) return false;

			vPtr = _curPos->Y() * _width + _curPos->X();
			cntVal = _counts[vPtr] / 10000;
			needsWork = !_doneFlags[vPtr] && cntVal < _targetIterationCnt;
		}

		if (needsWork) {
			index = GetCurPos();
			count = cntVal;

			vPtr *= 4;
			for (int i = 0; i < 4; i++) {
				zValsBuf[i] = _zValues[vPtr + i];
			}

			AdvanceCurPos();
			return true;
		}
		else {
			return false;
		}
	}

	void GenWorkVals::SaveWorkValues(PointInt index, unsigned int count, double * zValsBuf, bool doneFlag)
	{
		int vPtr = index.Y() * _width + index.X();
		_counts[vPtr] = 10000 * count;
		_doneFlags[vPtr] = doneFlag;

		vPtr *= 4;
		for (int i = 0; i < 4; i++) {
			_zValues[vPtr + i] = zValsBuf[i];
		}
	}

	void GenWorkVals::UpdateCntWithEV(PointInt index, double escapeVel)
	{
		int vPtr = index.Y() * _width + index.X();
		_counts[vPtr] = _counts[vPtr] + static_cast<int>(escapeVel * 10000);
	}

	bool GenWorkVals::IsCompleted()
	{
		return _completed;
	}

	// PRIVATE METHODS START HERE

	bool GenWorkVals::AdvanceCurPos()
	{
		if (_completed) return false;

		if (_curPos->X() == _width - 1) {
			if (_curPos->Y() == _height - 1) {
				_completed = true;
				return false;
			}
			else {
				_curPos->SetXY(0, _curPos->Y() + 1);
			}
		}
		else {
			_curPos->SetXY(_curPos->X() + 1, _curPos->Y());
		}

		return true;
	}

	PointInt GenWorkVals::GetCurPos()
	{
		return *_curPos;
	}

}
