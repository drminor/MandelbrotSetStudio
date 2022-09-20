/* Generated file, do not edit */

#ifndef CXXTEST_RUNNING
#define CXXTEST_RUNNING
#endif

#include <cxxtest/TestListener.h>
#include <cxxtest/TestTracker.h>
#include <cxxtest/TestRunner.h>
#include <cxxtest/RealDescriptions.h>
#include <cxxtest/TestMain.h>
#include <cxxtest/ParenPrinter.h>

int main( int argc, char *argv[] ) {
 int status;
    CxxTest::ParenPrinter tmp;
    CxxTest::RealWorldDescription::_worldName = "cxxtest";
    status = CxxTest::Main< CxxTest::ParenPrinter >( tmp, argc, argv );
    return status;
}
bool suite_IntrinsicTest_init = false;
#include "IntrinsicTests.h"

static IntrinsicTest suite_IntrinsicTest;

static CxxTest::List Tests_IntrinsicTest = { 0, 0 };
CxxTest::StaticSuiteDescription suiteDescription_IntrinsicTest( "IntrinsicTests.h", 5, "IntrinsicTest", suite_IntrinsicTest, Tests_IntrinsicTest );

static class TestDescription_suite_IntrinsicTest_testMultiplication : public CxxTest::RealTestDescription {
public:
 TestDescription_suite_IntrinsicTest_testMultiplication() : CxxTest::RealTestDescription( Tests_IntrinsicTest, suiteDescription_IntrinsicTest, 8, "testMultiplication" ) {}
 void runTest() { suite_IntrinsicTest.testMultiplication(); }
} testDescription_suite_IntrinsicTest_testMultiplication;

static class TestDescription_suite_IntrinsicTest_testAddition : public CxxTest::RealTestDescription {
public:
 TestDescription_suite_IntrinsicTest_testAddition() : CxxTest::RealTestDescription( Tests_IntrinsicTest, suiteDescription_IntrinsicTest, 13, "testAddition" ) {}
 void runTest() { suite_IntrinsicTest.testAddition(); }
} testDescription_suite_IntrinsicTest_testAddition;

#include <cxxtest/Root.cpp>
const char* CxxTest::RealWorldDescription::_worldName = "cxxtest";
