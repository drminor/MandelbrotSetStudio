#pragma once

#include "../FGen/FGen.h"
#include "Dd.h"
#include "FGenJob.h"

using namespace System;
namespace qdDotNet
{
	public ref class FGenerator
	{

	public:

		static int BLOCK_WIDTH = 100;
		static int BLOCK_HEIGHT = 100;

		FGenerator(FGenJob^ job);

		void FillCounts(PointInt position, array<UInt32>^% xCounts, array<bool>^% doneFlags, array<double>^% zValues);
		void FillXCountsTest(PointInt position, array<UInt32>^% xCounts, array<bool>^% doneFlags, array<double>^% zValues, int yPtr);

		property FGenJob^ Job
		{
		public:
			FGenJob^ get()
			{
				return m_Job;
			}
		}

		virtual ~FGenerator()
		{
			CleanUp();
		}
		!FGenerator()
		{
			CleanUp();
		}

	private:

		FGenJob^ m_Job;
		FGen::Generator* m_Generator;

		inline void CleanUp() {
			if (m_Generator != nullptr)
			{
				delete m_Generator;
			}
		}

	};
}
