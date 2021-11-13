#include "stdafx.h"
#include "FGenerator.h"
#include "../FGen/FGen.h"
#include "FGenJob.h"

namespace qdDotNet
{
	FGenerator::FGenerator(FGenJob^ job)
	{
		FGen::Job* nJob = (new FGen::Job(
			job->JobId,
			FGen::PointDd(job->Start.X().ToQp(), job->Start.Y().ToQp()),
			FGen::PointDd(job->End.X().ToQp(), job->End.Y().ToQp()),
			FGen::SizeInt(job->SamplePoints.W(), job->SamplePoints.H()),
			job->MaxIterations,
			FGen::RectangleInt(job->Area.X(), job->Area.Y(), job->Area.W(), job->Area.H())
		));

		m_Generator = new FGen::Generator(*nJob);
		m_Job = job;
	}

	void FGenerator::FillCounts(PointInt position, array<UInt32>^% xCounts, array<bool>^% doneFlags, array<double>^% zValues)
	{
		FGen::PointInt pos = FGen::PointInt(position.X(), position.Y());
		pin_ptr<unsigned int> pCnts = &xCounts[0];
		pin_ptr<bool> pDfs = &doneFlags[0];
		pin_ptr<double> pZVals = &zValues[0];

		m_Generator->FillCountsVec(pos, pCnts, pDfs, pZVals);
	}

	void FGenerator::FillXCountsTest(PointInt position, array<UInt32>^% xCounts, array<bool>^% doneFlags, array<double>^% zValues, int yPtr)
	{
		FGen::PointInt pos = FGen::PointInt(position.X(), position.Y());
		pin_ptr<unsigned int> pCnts = &xCounts[0];
		pin_ptr<bool> pDfs = &doneFlags[0];
		pin_ptr<double> pZVals = &zValues[0];

		m_Generator->FillXCountsTest(pos, pCnts, pDfs, pZVals, yPtr);
	}
	   	 	
}

