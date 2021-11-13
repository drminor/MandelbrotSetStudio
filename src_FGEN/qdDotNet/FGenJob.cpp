
#include "stdafx.h"
#include "FGenJob.h"
#include "../FGen/FGen.h"

namespace qdDotNet
{
	//FGenJob::FGenJob(String^ name, RectangleInt area, PointDd start, PointDd end, SizeInt samplePoints, int maxIterations)
	//	: ManagedObject(
	//		new FGen::Job(string_to_char_array(name),
	//			FGen::RectangleInt(area.X(), area.Y(), area.W(), area.H()),
	//			FGen::PointDd(start.X().ToDdReal(), start.Y().ToDdReal()), 
	//			FGen::PointDd(end.X().ToDdReal(), end.Y().ToDdReal()),
	//			FGen::SizeInt(samplePoints.W(), samplePoints.H()),
	//			maxIterations)
	//	)
	//{
	//}

	FGenJob::FGenJob(int jobId, PointDd start, PointDd end, SizeInt samplePoints, unsigned int maxIterations, RectangleInt area)
		: m_JobId(jobId), m_Start(start), m_End(end), m_SamplePoints(samplePoints), m_MaxIterations(maxIterations), m_Area(area)
	{
	}
}
