#pragma once

#ifdef QPVEC_EXPORTS
#define QPVEC_API __declspec(dllexport)
#else
#define QPVEC_API __declspec(dllimport)
#endif

#include "stdafx.h"
#include "mkl.h"
#include "vHelper.h"
#include "twoSum.h"
#include "twoProd.h"

