#include "stdafx.h"
#include "Job.h"

namespace FGen
{
	Job::Job(int jobId, PointDd start, PointDd end, SizeInt samplePoints, unsigned int maxIterations, RectangleInt area)
		: m_JobId(jobId), m_Start(start), m_End(end), m_SamplePoints(samplePoints), m_MaxIterations(maxIterations), m_Area(area)
	{
	}

	Job::~Job()
	{
	}
}



