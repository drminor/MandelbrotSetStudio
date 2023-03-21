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
bool suite_SampleTest_init = false;
#include "SampleTests.h"

static SampleTest suite_SampleTest;

static CxxTest::List Tests_SampleTest = { 0, 0 };
CxxTest::StaticSuiteDescription suiteDescription_SampleTest( "SampleTests.h", 5, "SampleTest", suite_SampleTest, Tests_SampleTest );

static class TestDescription_suite_SampleTest_testMultiplication2 : public CxxTest::RealTestDescription {
public:
 TestDescription_suite_SampleTest_testMultiplication2() : CxxTest::RealTestDescription( Tests_SampleTest, suiteDescription_SampleTest, 10, "testMultiplication2" ) {}
 void runTest() { suite_SampleTest.testMultiplication2(); }
} testDescription_suite_SampleTest_testMultiplication2;

static class TestDescription_suite_SampleTest_testAddition2 : public CxxTest::RealTestDescription {
public:
 TestDescription_suite_SampleTest_testAddition2() : CxxTest::RealTestDescription( Tests_SampleTest, suiteDescription_SampleTest, 15, "testAddition2" ) {}
 void runTest() { suite_SampleTest.testAddition2(); }
} testDescription_suite_SampleTest_testAddition2;

#include <cxxtest/Root.cpp>
const char* CxxTest::RealWorldDescription::_worldName = "cxxtest";
