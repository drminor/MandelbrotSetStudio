#pragma once

#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

#include "stdafx.h"
#include <vector>
#include "Job.h"
#include "GenPt.h"
#include "qp.h"
#include "FGenMath.h"


namespace FGen
{
	const int BLOCK_WIDTH = 100;
	const int BLOCK_HEIGHT = 100;

	class FGEN_API Generator
	{
	public:

		Generator(Job job);

		int GetJobId();

		Job GetJob();

		//std::vector<unsigned int> GetCounts();
		//std::vector<unsigned int> GetXCounts(int yPtr);

		//void FillCounts(PointInt pos, unsigned int* counts, bool * doneFlags, double * zValues);
		void FillCountsVec(PointInt pos, unsigned int* counts, bool * doneFlags, double * zValues);

		//void FillXCounts(PointInt pos, unsigned int* counts, bool * doneFlags, double * zValues, int yPtr);
		//void FillXCounts2(PointInt pos, unsigned int* counts, bool * doneFlags, double * zValues, int yPtr);

		void FillXCountsTest(PointInt pos, unsigned int* counts, bool * doneFlags, double * zValues, int yPtr);

		~Generator();

	private:
		const char* m_Name;
		Job m_Job;
		unsigned int m_targetIterationCount;
		qp* m_XPoints;
		qp* m_YPoints;
		double m_Log2;

		qp * GetXPoints();
		qp * GetYPoints();

		qp * GetPoints(int sampleCnt, int width, int areaStart, int areaEnd, qp startC, qp diff);

		//unsigned int GetCount(PointDd c, unsigned int maxIterations, unsigned int cntr, bool * done, PointDd * curVal);
		//unsigned int GetCount2(qp cX, qp cY, double * curZ, unsigned int cntr, bool * done, qp xSquared, qp ySquared);

		bool QpGreaterThan(double hi, double lo, double comp);
		//void SaveCnt(int index, GenPt genPt, bool done, unsigned int * counts, bool * doneFlags, double * zValues);

		//bool FillDoneSlot(int index, GenPt genPt, PointInt &curCoordIndex, bool &morePts, int startX, int startY,
		//	unsigned int * counts, bool * doneFlags, double * zValues);

		////bool GetNextWorkIndex(PointInt &cur, bool &morePts, unsigned int * counts, bool * doneFlags);
		//PointInt GetNextCoordIndex(PointInt cur, bool &morePts);

		double GetEscapeVelocity(qp sumSqs);
		PointDd GetPointDd(double * zValues);
		void PointDdToDoubleArray(PointDd z, double * zValues);

	};

}

