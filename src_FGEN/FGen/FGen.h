#pragma once

#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

#include "qp.h"
#include "CoordsDd.h"
#include "CoordsMath.h"
#include "Generator.h"
#include "Job.h"
#include "PointDd.h"
#include "SizeInt.h"
#include "PointInt.h"
#include "RectangleInt.h"

//#include "qpMath.h"