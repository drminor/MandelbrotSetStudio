#include "stdafx.h"
#include "GenPt.h"

namespace FGen
{
	GenPt::GenPt(int blockWidth)
	{
		_blockWidth = blockWidth;

		_cnt = new int[blockWidth];

		_cxCordHis = new double[blockWidth];
		_cxCordLos = new double[blockWidth];
		_cyCordHis = new double[blockWidth];
		_cyCordLos = new double[blockWidth];

		_resultIndexes = new PointInt[blockWidth];
		_evIterationsRemaining = new int[blockWidth];

		_zxCordHis = new double[blockWidth];
		_zxCordLos = new double[blockWidth];
		_zyCordHis = new double[blockWidth];
		_zyCordLos = new double[blockWidth];

		_xsCordHis = new double[blockWidth];
		_xsCordLos = new double[blockWidth];
		_ysCordHis = new double[blockWidth];
		_ysCordLos = new double[blockWidth];

		_sumSqsHis = new double[blockWidth];
		_sumSqsLos = new double[blockWidth];

		_rCordHis = new double[blockWidth];
		_rCordLos = new double[blockWidth];

		for (int i = 0; i < blockWidth; i++) {
			_cnt[i] = 0;

			_cxCordHis[i] = 0;
			_cxCordLos[i] = 0;
			_cyCordHis[i] = 0;
			_cyCordLos[i] = 0;

			_zxCordHis[i] = 0;
			_zxCordLos[i] = 0;
			_zyCordHis[i] = 0;
			_zyCordLos[i] = 0;

			_xsCordHis[i] = 0;
			_xsCordLos[i] = 0;
			_ysCordHis[i] = 0;
			_ysCordLos[i] = 0;

			_sumSqsHis[i] = 0;
			_sumSqsLos[i] = 0;

			_resultIndexes[i] = PointInt();
			_evIterationsRemaining[i] = -1;

			_rCordHis[i] = 0;
			_rCordLos[i] = 0;
		}
	}

	void GenPt::SetC(int index, PointInt resultIndex, qp cx, qp cy, qp zx, qp zy, unsigned int cnt)
	{
		_cnt[index] = cnt;
		_cxCordHis[index] = cx._hi();
		_cxCordLos[index] = cx._lo();
		_cyCordHis[index] = cy._hi();
		_cyCordLos[index] = cy._lo();

		_zxCordHis[index] = zx._hi();
		_zxCordLos[index] = zx._lo();
		_zyCordHis[index] = zy._hi();
		_zyCordLos[index] = zy._lo();

		_resultIndexes[index].SetXY(resultIndex.X(), resultIndex.Y());
		Clear(index);
	}

	void GenPt::Clear(int index)
	{
		_evIterationsRemaining[index] = -1;

		_xsCordHis[index] = 0;
		_xsCordLos[index] = 0;
		_ysCordHis[index] = 0;
		_ysCordLos[index] = 0;

		_sumSqsHis[index] = -1;
		_sumSqsLos[index] = 0;

		_rCordHis[index] = 0;
		_rCordLos[index] = 0;
	}

	void GenPt::SetEmpty(int index)
	{
		_resultIndexes[index].SetXY(-1, -1);
	}

	bool GenPt::IsEmpty(int index)
	{
		return _resultIndexes[index].X() == -1;
	}

	bool GenPt::IsEvIterationsRemainingZero(int index)
	{
		return _evIterationsRemaining[index] == 0;
	}

	void GenPt::SetEvIterationsRemaining(int index, int val)
	{
		_evIterationsRemaining[index] = val;
	}

	int GenPt::DecrementEvIterationsRemaining(int index)
	{
		int nv = _evIterationsRemaining[index] - 1;
		_evIterationsRemaining[index] = nv;
		return nv;
	}

	GenPt::~GenPt()
	{
		delete[] _cxCordHis, _cxCordLos, _cyCordHis, _cyCordLos;
		delete[] _zxCordHis, _zxCordLos, _zyCordHis, _zyCordLos;
		delete[] _xsCordHis, _xsCordLos, _ysCordHis, _ysCordLos;
		delete[] _cnt, _sumSqsHis, _sumSqsLos;
		delete[] _resultIndexes, _evIterationsRemaining;

		delete[] _rCordHis, _rCordLos;

	}
}
