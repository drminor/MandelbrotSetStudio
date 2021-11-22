#include "stdafx.h"
#include <iostream>

#include "FGen.h"
#include "qpMath.h"
#include "qpMathVec.h"
#include "GenWorkVals.h"

namespace FGen
{
	Generator::Generator(Job job) : m_Job(job)
	{
		m_targetIterationCount = m_Job.MaxIterations();
		m_XPoints = GetXPoints();
		m_YPoints = GetYPoints();
		m_Log2 = std::log10(2);
	}

	Generator::~Generator()
	{
		delete[] m_XPoints;
		delete[] m_YPoints;
	}

	int Generator::GetJobId()
	{
		return m_Job.JobId();
	};

	FGen::Job Generator::GetJob()
	{
		return m_Job;
	};

	//RectangleInt m_Area;
	//PointDd m_Start;
	//PointDd m_End;
	//SizeInt m_SamplePoints;
	//unsigned int m_MaxIterations;

	void Generator::FillCountsVec(PointInt pos, unsigned int * counts, bool * doneFlags, double * zValues)
	{
		int startX = pos.X() * BLOCK_WIDTH;
		int startY = pos.Y() * BLOCK_HEIGHT;

		FGenMath * fgenCalc = new FGenMath(BLOCK_WIDTH);

		GenWorkVals * workVals = new GenWorkVals(BLOCK_WIDTH, BLOCK_HEIGHT, m_targetIterationCount, counts, doneFlags, zValues);
		GenPt * genPt = new GenPt(BLOCK_WIDTH);
		int genPtLen = 0;
		int nullPtIterationOps = 0;

		PointInt curCoordIndex = PointInt(0, 0); 
		unsigned int count;
		double * zValsBuf = new double[4];
		bool morePts = true;

		for (int i = 0; i < FGen::BLOCK_WIDTH && morePts; i++) {
			morePts = workVals->GetNextWorkValues(curCoordIndex, count, zValsBuf);
			if (morePts) {
				qp cY = m_YPoints[startY + curCoordIndex.Y()];
				qp cX = m_XPoints[startX + curCoordIndex.X()];

				qp zX = qp(zValsBuf[0], zValsBuf[1]);
				qp zY = qp(zValsBuf[2], zValsBuf[3]);

				genPt->SetC(genPtLen++, curCoordIndex, cX, cY, zX, zY, count);
			}
		}

		bool complete = genPtLen < 1;
		bool haveNewEntries = true;

		while (!complete)
		{
			if (genPtLen < 1) std::cout << "Not complete, but genPtLen is < 1.";
			if (haveNewEntries) {
				fgenCalc->InitialzeNewEntries(*genPt);
				haveNewEntries = false;
			}

			fgenCalc->Iterate(*genPt);
			nullPtIterationOps += (BLOCK_WIDTH - genPtLen);

			complete = true;
			for (int i = 0; i < BLOCK_WIDTH; i++)
			{
				if (genPt->IsEmpty(i)) continue;

				if (genPt->_evIterationsRemaining[i] > 0) {
					if (QpGreaterThan(genPt->_sumSqsHis[i], genPt->_sumSqsLos[i], 256)) {
						qp sumSqs = qp(genPt->_sumSqsHis[i], genPt->_sumSqsLos[i]);
						double escapVel = GetEscapeVelocity(sumSqs);
						workVals->UpdateCntWithEV(genPt->_resultIndexes[i], escapVel);

						morePts = workVals->GetNextWorkValues(curCoordIndex, count, zValsBuf);
						if (morePts) {
							qp cY = m_YPoints[startY + curCoordIndex.Y()];
							qp cX = m_XPoints[startX + curCoordIndex.X()];

							qp zX = qp(zValsBuf[0], zValsBuf[1]);
							qp zY = qp(zValsBuf[2], zValsBuf[3]);

							genPt->SetC(i, curCoordIndex, cX, cY, zX, zY, count);

							haveNewEntries = true;
							complete = false;
						}
						else {
							genPt->SetEmpty(i);
							genPtLen--;
						}
					}
					else {
						genPt->DecrementEvIterationsRemaining(i);
						complete = false;
					}
					continue;
				}

				if (genPt->IsEvIterationsRemainingZero(i)) {

					// The size of Z is still less than 256 after an additional 25 interations.
					// Add 1 to the count because this point is growing very slowy.
					workVals->UpdateCntWithEV(genPt->_resultIndexes[i], 1);
					
					morePts = workVals->GetNextWorkValues(curCoordIndex, count, zValsBuf);
					if (morePts) {
						qp cY = m_YPoints[startY + curCoordIndex.Y()];
						qp cX = m_XPoints[startX + curCoordIndex.X()];

						qp zX = qp(zValsBuf[0], zValsBuf[1]);
						qp zY = qp(zValsBuf[2], zValsBuf[3]);

						genPt->SetC(i, curCoordIndex, cX, cY, zX, zY, count);

						complete = false;
					}
					else {
						genPt->SetEmpty(i);
						genPtLen--;
					}
					continue;
				}

				if (QpGreaterThan(genPt->_sumSqsHis[i], genPt->_sumSqsLos[i], 4)) {

					zValsBuf[0] = genPt->_zxCordHis[i];
					zValsBuf[1] = genPt->_zxCordLos[i];
					zValsBuf[2] = genPt->_zyCordHis[i];
					zValsBuf[3] = genPt->_zyCordLos[i];

					workVals->SaveWorkValues(genPt->_resultIndexes[i], genPt->_cnt[i], zValsBuf, true);
					genPt->SetEvIterationsRemaining(i, 25);
					complete = false;

					//qp sumSqs = qp(genPt->_sumSqsHis[i], genPt->_sumSqsLos[i]);
					//double escapVel = GetEscapeVelocity(sumSqs);
					//workVals->UpdateCntWithEV(genPt->_resultIndexes[i], escapVel);

					//morePts = workVals->GetNextWorkValues(curCoordIndex, count, zValsBuf);
					//if (morePts) {
					//	qp cY = m_YPoints[startY + curCoordIndex.Y()];
					//	qp cX = m_XPoints[startX + curCoordIndex.X()];

					//	qp zX = qp(zValsBuf[0], zValsBuf[1]);
					//	qp zY = qp(zValsBuf[2], zValsBuf[3]);

					//	genPt->SetC(i, curCoordIndex, cX, cY, zX, zY, count);

					//	haveNewEntries = true;
					//	complete = false;
					//}
					//else {
					//	genPt->SetEmpty(i);
					//}
					continue;
				}

				genPt->_cnt[i]++;
				if (genPt->_cnt[i] == m_targetIterationCount) {

					zValsBuf[0] = genPt->_zxCordHis[i];
					zValsBuf[1] = genPt->_zxCordLos[i];
					zValsBuf[2] = genPt->_zyCordHis[i];
					zValsBuf[3] = genPt->_zyCordLos[i];

					workVals->SaveWorkValues(genPt->_resultIndexes[i], genPt->_cnt[i], zValsBuf, false);

					morePts = workVals->GetNextWorkValues(curCoordIndex, count, zValsBuf);
					if (morePts) {
						qp cY = m_YPoints[startY + curCoordIndex.Y()];
						qp cX = m_XPoints[startX + curCoordIndex.X()];

						qp zX = qp(zValsBuf[0], zValsBuf[1]);
						qp zY = qp(zValsBuf[2], zValsBuf[3]);

						genPt->SetC(i, curCoordIndex, cX, cY, zX, zY, count);
						haveNewEntries = true;
						complete = false;
					}
					else {
						genPt->SetEmpty(i);
						genPtLen--;
					}
				}
				else {
					complete = false;
				}
			}
		}
		
		if (nullPtIterationOps > 0) {
			std::cout << "Executed " << nullPtIterationOps << " null pt iterations.";
		}

		delete fgenCalc;
		delete workVals;
		delete genPt;
		delete[] zValsBuf;
	}

	bool Generator::QpGreaterThan(double hi, double lo, double comp)
	{
		return (hi > comp) || ((hi == comp) && (lo > 0.0));

		//bool le(double b) const
		//{
		//	if (isnan() || QD_ISNAN(b)) return false;
		//	return (_hi() < b) || ((_hi() == b) && (_lo() <= 0.0));
		//}
	}

	void Generator::FillXCountsTest(PointInt pos, unsigned int * counts, bool * doneFlags, double * zValues, int yPtr)
	{
		int startX = pos.X() * 3;
		int startY = pos.Y() * BLOCK_HEIGHT;

		int resultPtr = yPtr * BLOCK_WIDTH;

		qp yCord = m_YPoints[startY + yPtr];

		for (int i = 0; i < FGen::BLOCK_WIDTH; i++) {
			qp xCord = m_XPoints[startX + i];
			PointDd c = PointDd(xCord, yCord);

			PointDd z = GetPointDd(&zValues[resultPtr * 4]);
			counts[resultPtr] = 3000 + i;
			doneFlags[resultPtr] = true;
			PointDdToDoubleArray(z, &zValues[resultPtr * 4]);
			resultPtr++;
		}
	}

	PointDd Generator::GetPointDd(double * zValues)
	{
		double xHi = zValues[0];
		double xLo = zValues[1];
		double yHi = zValues[2];
		double yLo = zValues[3];

		return PointDd(qp(xHi, xLo), qp(yHi, yLo));
	}

	void Generator::PointDdToDoubleArray(PointDd z, double * zValues) {
		zValues[0] = z.X()._hi();
		zValues[1] = z.X()._lo();
		zValues[2] = z.Y()._hi();
		zValues[3] = z.Y()._lo();
	}

	//unsigned int Generator::GetCount(PointDd c, unsigned int maxIterations, unsigned int cntr, bool * done, PointDd * curVal) {
	//
	//	qp cX = c.X();
	//	qp cY = c.Y();
	//
	//	qp zX = curVal->X();
	//	qp zY = curVal->Y();
	//
	//	qp xSquared = qp();
	//	qp ySquared = qp();
	//
	//	double escapeVel = 0;
	//	//unsigned int cntr;
	//	//cntr /= 10000;
	//	cntr = 0;
	//	for (; cntr < maxIterations; cntr++) {
	//		zY = 2 * zX * zY + cY;
	//		zX = xSquared - ySquared + cX;
	//		xSquared = zX * zX;
	//		ySquared = zY * zY;
	//
	//		if ((xSquared + ySquared) > 4) {
	//			//escapeVel = GetEscapeVelocity(cX, cY, zX, zY, xSquared, ySquared);
	//			*done = true;
	//			break;
	//		}
	//	}
	//
	//	*curVal = PointDd(zX, zY);
	//
	//	double both = cntr + escapeVel;
	//	both = std::round(10000 * both);
	//
	//	return int(both);
	//}
	//
	//unsigned int Generator::GetCount2(qp cX, qp cY, double * curZ, unsigned int cntr, bool * done, qp xSquared, qp ySquared)
	//{
	//	qp zX = qp(curZ[0], curZ[1]);
	//	qp zY = qp(curZ[2], curZ[3]);
	//
	//	xSquared.resetToZero();
	//	ySquared.resetToZero();
	//
	//	double escapeVel = 0;
	//	//unsigned int cntr;
	//	//cntr /= 10000;
	//	cntr = 0;
	//	for (; cntr < m_targetIterationCount; cntr++) {
	//		zY = 2 * zX * zY + cY;
	//		zX = xSquared - ySquared + cX;
	//		xSquared = zX * zX;
	//		ySquared = zY * zY;
	//
	//		if ((xSquared + ySquared) > 4) {
	//
	//			//escapeVel = GetEscapeVelocity(cX, cY, zX, zY, xSquared, ySquared);
	//			*done = true;
	//			break;
	//		}
	//	}
	//
	//	curZ[0] = zX._hi();
	//	curZ[1] = zX._lo();
	//	curZ[2] = zY._hi();
	//	curZ[3] = zY._lo();
	//
	//	double both = cntr + escapeVel;
	//	both = std::round(10000 * both);
	//
	//	return int(both);
	//}

	//public double Iterate(DPoint c, ref DPoint z, ref int cntr, out bool done)
	//{
	//	done = false;
	//	double escapeVelocity = 0.0;
	//
	//	_xSquared = 0;
	//	_ySquared = 0;
	//
	//	for (; cntr < _maxIterations; cntr++)
	//	{
	//		z.Y = 2 * z.X * z.Y + c.Y;
	//		z.X = _xSquared - _ySquared + c.X;
	//
	//		_xSquared = z.X * z.X;
	//		_ySquared = z.Y * z.Y;
	//
	//		if ((_xSquared + _ySquared) > 4)
	//		{
	//			done = true;
	//			escapeVelocity = GetEscapeVelocity(z, c, _xSquared, _ySquared);
	//			//escapeVelocity = 0.4;
	//			break;
	//		}
	//	}
	//
	//	return escapeVelocity;
	//}

	double Generator::GetEscapeVelocity(qp sumSqs)
	{
		double evd = sumSqs.toDouble();

		double modulus = std::log10(evd) / 2;
	    double nu = std::log10(modulus / m_Log2) / m_Log2;
		nu /= 4;

		if (nu > 1) {
			std::cout << "Nu has a value of " << nu << ", using 1 instead.";
			nu = 1;
		}

		 double result = 1 - nu;
		//double result = 0.0;
	    return result;
	}

	//// sqrt of inner term removed using log simplification rules.
	//log_zn = log(x*x + y * y) / 2
	//	nu = log(log_zn / log(2)) / log(2)
	//	// Rearranging the potential function.
	//	// Dividing log_zn by log(2) instead of log(N = 1<<8)
	//	// because we want the entire palette to range from the
	//	// center to radius 2, NOT our bailout radius.
	//	iteration = iteration + 1 - nu
	//}

	qp* Generator::GetXPoints()
	{
		int xSamples = m_Job.SamplePoints().W();
		int areaXSampleCnt = m_Job.Area().W() * BLOCK_WIDTH;

		qp startC = m_Job.Start().X();
		qp endC = m_Job.End().X();

		int start = m_Job.Area().SX() * BLOCK_WIDTH;
		int end = start + areaXSampleCnt;

		qp* result = GetPoints(xSamples, areaXSampleCnt, start, end, startC, endC);

		return result;
	}

	qp* Generator::GetYPoints()
	{
		int ySamples = m_Job.SamplePoints().H();
		int areaYSampleCnt = m_Job.Area().H() * BLOCK_HEIGHT;

		qp startC = m_Job.End().Y();
		qp endC = m_Job.Start().Y();

		int start = m_Job.Area().SY() * BLOCK_HEIGHT;
		int end = start + areaYSampleCnt;

		qp* result = GetPoints(ySamples, areaYSampleCnt, start, end, startC, endC);

		return result;
	}

	qp* Generator::GetPoints(int sampleCnt, int width, int areaStart, int areaEnd, qp startC, qp endC)
	{
		qp* result = new qp[width];

		qpMath * qpCalc = new qpMath();

		//qp diff = qpCalc->getDiff(endC, startC);
		qp diff = qpCalc->sub(endC, startC);

		double * rats = new double[sampleCnt];

		//int rPtr = 0;
		for (int i = areaStart; i < areaEnd; i++)
		{
			rats[i - areaStart] = i / (double)sampleCnt;
		}

		qpMathVec * qpVecCalc = new qpMathVec(width);

		double * diff_his = new double[width];
		double * diff_los = new double[width];
		qpVecCalc->extendSingleQp(diff, diff_his, diff_los);

		double * temp_his = new double[width];
		double * temp_los = new double[width];
		qpVecCalc->mulQpByD(diff_his, diff_los, rats, temp_his, temp_los);

		double * startC_his = new double[width];
		double * startC_los = new double[width];
		qpVecCalc->extendSingleQp(startC, startC_his, startC_los);

		qpVecCalc->addQps(temp_his, temp_los, startC_his, startC_los, diff_his, diff_los);

		qpVecCalc->fillQpVector(diff_his, diff_los, result);

		delete[] diff_his, diff_los, startC_his, startC_los;
		delete[] temp_his, temp_los;

		delete qpCalc;
		delete qpVecCalc;

		return result;
	}

	//std::vector<unsigned int>Generator::GetCounts()
	//{
	//	int xSamples = m_Job.SamplePoints().W();
	//	int ySamples = m_Job.SamplePoints().H();
	//
	//	int tSamples = xSamples * ySamples;
	//	std::vector<unsigned int> result; 
	//	//std::vector<unsigned short> result(tSamples);
	//
	//	result.reserve(tSamples);
	//
	//	int rPtr = 0;
	//	for (int j = 0; j < ySamples; j++) {
	//		qp yCord = m_YPoints[j];
	//		for (int i = 0; i < xSamples; i++) {
	//			qp xCord = m_XPoints[i];
	//			PointDd c = PointDd(xCord, yCord);
	//			//result[rPtr++] = Generator::GetCount(c, m_Job.MaxIterations());
	//			bool done = false;
	//			PointDd z = PointDd(0, 0);
	//
	//			result.push_back(Generator::GetCount(c, m_targetIterationCount, 0, &done, &z));
	//		}
	//	}
	//
	//	result.resize(tSamples);
	//
	//	return result;
	//}
	//
	//std::vector<unsigned int> Generator::GetXCounts(int yPtr)
	//{
	//	std::vector<unsigned int> result;
	//
	//	int xSamples = m_Job.SamplePoints().W();
	//	result.reserve(xSamples);
	//
	//	qp yCord = m_YPoints[yPtr];
	//	for (int i = 0; i < xSamples; i++) {
	//		qp xCord = m_XPoints[i];
	//		PointDd c = PointDd(xCord, yCord);
	//		bool done = false;
	//		PointDd z = PointDd(0, 0);
	//		result.push_back(Generator::GetCount(c, m_targetIterationCount, 0, &done, &z));
	//	}
	//
	//	result.resize(xSamples);
	//
	//	return result;
	//}

	//void Generator::FillCounts(PointInt pos, unsigned int * counts, bool * doneFlags, double * zValues)
	//{
	//	int startX = pos.X() * BLOCK_WIDTH;
	//	int startY = pos.Y() * BLOCK_HEIGHT;
	//
	//	int resultPtr = 0;
	//	for (int j = 0; j < FGen::BLOCK_HEIGHT; j++) {
	//		
	//		qp yCord = m_YPoints[startY + j];
	//
	//		for (int i = 0; i < FGen::BLOCK_WIDTH; i++) {
	//			qp xCord = m_XPoints[startX + i];
	//			PointDd c = PointDd(xCord, yCord);
	//
	//			PointDd z = GetPointDd(&zValues[resultPtr * 4]);
	//			counts[resultPtr] = Generator::GetCount(c, m_targetIterationCount, counts[resultPtr], &doneFlags[resultPtr], &z);
	//			PointDdToDoubleArray(z, &zValues[resultPtr * 4]);
	//
	//			resultPtr++;
	//		}
	//	}
	//}

	//void Generator::FillXCounts(PointInt pos, unsigned int * counts, bool * doneFlags, double * zValues, int yPtr)
	//{
	//	int startX = pos.X() * BLOCK_WIDTH;
	//	int startY = pos.Y() * BLOCK_HEIGHT;
	//
	//	int resultPtr = yPtr * BLOCK_WIDTH;
	//
	//	qp yCord = m_YPoints[startY + yPtr];
	//
	//	for (int i = 0; i < FGen::BLOCK_WIDTH; i++) {
	//		qp xCord = m_XPoints[startX + i];
	//		PointDd c = PointDd(xCord, yCord);
	//
	//		PointDd z = GetPointDd(&zValues[resultPtr * 4]);
	//		counts[resultPtr] = Generator::GetCount(c, m_targetIterationCount, counts[resultPtr], &doneFlags[resultPtr], &z);
	//		PointDdToDoubleArray(z, &zValues[resultPtr * 4]);
	//
	//		resultPtr++;
	//	}
	//}

	//void Generator::FillXCounts2(PointInt pos, unsigned int * counts, bool * doneFlags, double * zValues, int yPtr)
	//{
	//	qp xSquared = qp(0);
	//	qp ySquared = qp(0);
	//	int startX = pos.X() * BLOCK_WIDTH;
	//	int startY = pos.Y() * BLOCK_HEIGHT;
	//
	//	int resultPtr = yPtr * BLOCK_WIDTH;
	//
	//	qp yCord = m_YPoints[startY + yPtr];
	//
	//	for (int i = 0; i < FGen::BLOCK_WIDTH; i++) {
	//		qp xCord = m_XPoints[startX + i];
	//		counts[resultPtr] = Generator::GetCount2(xCord, yCord, &zValues[resultPtr * 4], counts[resultPtr], &doneFlags[resultPtr], xSquared, ySquared);
	//		resultPtr++;
	//	}
	//
	//	for (int i = 0; i < FGen::BLOCK_WIDTH; i++) {
	//		qp xCord = m_XPoints[startX + i];
	//		counts[resultPtr] = Generator::GetCount2(xCord, yCord, &zValues[resultPtr * 4], counts[resultPtr], &doneFlags[resultPtr], xSquared, ySquared);
	//		resultPtr++;
	//	}
	//}

}

