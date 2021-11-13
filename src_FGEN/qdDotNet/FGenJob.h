#pragma once

#include "../FGen/FGen.h"
#include "ManagedObject.h"
#include "Dd.h"
#include "RectangleInt.h"
#include "PointDd.h"
#include "SizeInt.h"
#include "PointInt.h"

using namespace System;
namespace qdDotNet
{
	public ref class FGenJob
	{

	public:
		FGenJob(int jobId, PointDd start, PointDd end, SizeInt samplePoints, unsigned int maxIterations, RectangleInt area);

		property int JobId
		{
		public:
			int get()
			{
				return m_JobId;
			}
		}
		property PointDd Start
		{
		public:
			PointDd get()
			{
				return m_Start;
			}
		}

		property PointDd End
		{
		public:
			PointDd get()
			{
				return m_End;
			}
		}

		property SizeInt SamplePoints
		{
		public:
			SizeInt get()
			{
				return m_SamplePoints;
			}
		}

		property unsigned int MaxIterations
		{
		public:
			unsigned int get()
			{
				return m_MaxIterations;
			}
		}

		property RectangleInt Area
		{
		public:
			RectangleInt get()
			{
				return m_Area;
			}
		}

	private:
		int m_JobId;
		RectangleInt m_Area;
		PointDd m_Start;
		PointDd m_End;
		SizeInt m_SamplePoints;
		unsigned int m_MaxIterations;


	};
}


