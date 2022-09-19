#include "pch.h"
#include "Generator.h"

#include <iostream>

#include "qpMath.h"
#include "qpMathVec.h"

#include "GenPt.h"
#include "FGenMath.h"

#include "GenWorkVals.h"

Generator::Generator()
{
	// TODO: Make m_log2 be a static property
	m_Log2 = std::log10(2);
}

Generator::~Generator()
{
}

void Generator::FillCountsVec(PointDd pos, SizeInt blockSize, SizeDd sampleSize, int targetCount, int* counts, bool* doneFlags, double* zValues)
{
	int blockWidth = blockSize.Width();
	int blockHeight = blockSize.Height();

	qp* xPoints = new qp[blockWidth];
	GetPoints(pos.X(), sampleSize.Width(), blockWidth, xPoints);

	qp* yPoints = new qp[blockHeight];
	GetPoints(pos.Y(), sampleSize.Height(), blockHeight, yPoints);

	FGenMath* fgenCalc = new FGenMath(blockWidth);

	GenWorkVals* workVals = new GenWorkVals(blockWidth, blockHeight, targetCount, counts, doneFlags, zValues);
	GenPt* genPt = new GenPt(blockWidth);
	int genPtLen = 0;
	int nullPtIterationOps = 0;

	PointInt curCoordIndex = PointInt(0, 0);
	int count;
	double* zValsBuf = new double[4];
	bool morePts = true;

	for (int i = 0; i < blockWidth && morePts; i++) {
		morePts = workVals->GetNextWorkValues(curCoordIndex, count, zValsBuf);
		if (morePts) {
			qp cX = xPoints[curCoordIndex.X()];
			qp cY = yPoints[curCoordIndex.Y()];

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
		nullPtIterationOps += (blockWidth - genPtLen);

		complete = true;
		for (int i = 0; i < blockWidth; i++)
		{
			if (genPt->IsEmpty(i)) continue;

			if (genPt->_evIterationsRemaining[i] > 0) {
				if (QpGreaterThan(genPt->_sumSqsHis[i], genPt->_sumSqsLos[i], 256)) {
					qp sumSqs = qp(genPt->_sumSqsHis[i], genPt->_sumSqsLos[i]);
					double escapVel = GetEscapeVelocity(sumSqs);
					workVals->UpdateCntWithEV(genPt->_resultIndexes[i], escapVel);

					morePts = workVals->GetNextWorkValues(curCoordIndex, count, zValsBuf);
					if (morePts) {
						qp cX = xPoints[curCoordIndex.X()];
						qp cY = yPoints[curCoordIndex.Y()];

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
					qp cX = xPoints[curCoordIndex.X()];
					qp cY = yPoints[curCoordIndex.Y()];

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
			if (genPt->_cnt[i] == targetCount) {

				zValsBuf[0] = genPt->_zxCordHis[i];
				zValsBuf[1] = genPt->_zxCordLos[i];
				zValsBuf[2] = genPt->_zyCordHis[i];
				zValsBuf[3] = genPt->_zyCordLos[i];

				workVals->SaveWorkValues(genPt->_resultIndexes[i], genPt->_cnt[i], zValsBuf, false);

				morePts = workVals->GetNextWorkValues(curCoordIndex, count, zValsBuf);
				if (morePts) {
					qp cX = xPoints[curCoordIndex.X()];
					qp cY = yPoints[curCoordIndex.Y()];

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
		std::cout << "  -+- Executed " << nullPtIterationOps << " null pt iterations.";
	}

	delete fgenCalc;
	delete workVals;
	delete genPt;
	delete[] zValsBuf;

	delete[] xPoints;
	delete[] yPoints;
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

void Generator::FillXCountsTest(PointDd pos, SizeInt blockSize, SizeDd sampleSize, int targetCount, unsigned int* counts, bool* doneFlags, double* zValues, int yPtr)
{
	int blockWidth = blockSize.Width();
	int blockHeight = blockSize.Height();

	qp* xPoints = new qp[blockWidth];
	GetPoints(pos.X(), sampleSize.Width(), blockWidth, xPoints);

	qp* yPoints = new qp[blockHeight];
	GetPoints(pos.Y(), sampleSize.Height(), blockHeight, yPoints);

	int resultPtr = yPtr * blockWidth;

	qp yCord = yPoints[0];

	for (int i = 0; i < blockWidth; i++) {
		qp xCord = xPoints[i];
		PointDd c = PointDd(xCord, yCord);

		PointDd z = GetPointDd(&zValues[resultPtr * 4]);
		counts[resultPtr] = 3000 + i;
		doneFlags[resultPtr] = true;
		PointDdToDoubleArray(z, &zValues[resultPtr * 4]);
		resultPtr++;
	}

	delete[] xPoints;
	delete[] yPoints;
}

PointDd Generator::GetPointDd(double* zValues)
{
	double xHi = zValues[0];
	double xLo = zValues[1];
	double yHi = zValues[2];
	double yLo = zValues[3];

	return PointDd(qp(xHi, xLo), qp(yHi, yLo));
}

void Generator::PointDdToDoubleArray(PointDd z, double* zValues) {
	zValues[0] = z.X()._hi();
	zValues[1] = z.X()._lo();
	zValues[2] = z.Y()._hi();
	zValues[3] = z.Y()._lo();
}

double Generator::GetEscapeVelocity(qp sumSqs)
{
	// TODO: Consider providing this method a qpMath object.
	qpMath* qpCalc = new qpMath();

	qp sumSqsHi = qp(sumSqs._hi());
	qp sumSqsTotal = qpCalc->addD(sumSqsHi, sumSqs._lo());

	double evd = sumSqsTotal.toDouble();

	double modulus = std::log10(evd) / 2;
	double nu = std::log10(modulus / m_Log2) / m_Log2;
	nu /= 4;

	if (nu > 1) {
		std::cout << "Nu has a value of " << nu << ", using 1 instead.";
		nu = 1;
	}

	double result = 1 - nu;
	//double result = 0.0;

	delete qpCalc;

	return result;
}

void Generator::GetPoints(qp startC, qp delta, int extent, qp* result)
{
	qpMath* qpCalc = new qpMath();

	double* factors = new double[extent];

	for (int i = 0; i < extent; i++)
	{
		factors[i] = i;
	}

	qpMathVec* qpVecCalc = new qpMathVec(extent);

	double* diff_his = new double[extent];
	double* diff_los = new double[extent];
	qpVecCalc->extendSingleQp(delta, diff_his, diff_los);

	double* temp_his = new double[extent];
	double* temp_los = new double[extent];
	qpVecCalc->mulQpByD(diff_his, diff_los, factors, temp_his, temp_los);

	double* startC_his = new double[extent];
	double* startC_los = new double[extent];
	qpVecCalc->extendSingleQp(startC, startC_his, startC_los);

	qpVecCalc->addQps(temp_his, temp_los, startC_his, startC_los, diff_his, diff_los);

	qpVecCalc->fillQpVector(diff_his, diff_los, result);

	delete qpCalc;
	delete[] factors;
	delete qpVecCalc;
	delete[] diff_his, diff_los;
	delete[] temp_his, temp_los;
	delete[] startC_his, startC_los;
}







