using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Test
{
  static class Program
  {
    [STAThread]
    static void Main()
    {
      //generate some random numbers for the test
      var ff = new float[100000]; var rnd = new Random();
      for (int i = 0; i < ff.Length; i++) ff[i] = (float)(rnd.NextDouble() - 0.5f) * 1000;

      var oldrationals = new BigRational[100000];
      var newrationals = new NewRational[100000];

      //meaningless calculations just to test
      //at first some calls for JIT compilation
      for (int i = 0; i < 10; i += 2)
      {
        BigRational a = ff[i + 0], b = ff[i + 1];
        oldrationals[i + 0] = (a * b - a + b * b + b / 1234.5678M).Sign;
        oldrationals[i + 1] = a * b - b * b + a / Math.PI + a * 3 - b / 0.0001M;
      }
      for (int i = 0; i < 10; i += 2)
      {
        NewRational a = ff[i + 0], b = ff[i + 1];
        newrationals[i + 0] = (a * b - a + b * b + b / 1234.5678M).Sign;
        newrationals[i + 1] = a * b - b * b + a / Math.PI + a * 3 - b / 0.0001M;
      }
 
      //now the speed test 
      var sw = new Stopwatch();
      sw.Start();
      for (int i = 0; i < 100000; i += 2)
      {
        BigRational a = ff[i + 0], b = ff[i + 1];
        oldrationals[i + 0] = (a * b - a + b * b + b / 1234.5678M).Sign;
        oldrationals[i + 1] = a * b - b * b + a / Math.PI + a * 3 - b / 0.0001M;
      }
      sw.Stop(); var t1 = sw.ElapsedMilliseconds; sw.Reset();

      sw.Start();
      for (int i = 0; i < 100000; i += 2)
      {
        NewRational a = ff[i + 0], b = ff[i + 1];
        newrationals[i + 0] = (a * b - a + b * b + b / 1234.5678M).Sign;
        newrationals[i + 1] = a * b - b * b + a / Math.PI + a * 3 - b / 0.0001M;
      }
      sw.Stop(); var t2 = sw.ElapsedMilliseconds; sw.Reset();

      //just to make sure that we have same results:
      for (int i = 0; i < 100000; i++)
      {
        var f1 = (float)oldrationals[i];
        var f2 = (float)newrationals[i];
        if (f1 != f2) { MessageBox.Show("Error"); break; }
      }

      MessageBox.Show("BigRational: " + t1 + "ms\n" + "NewRational: " + t2 + "ms\n" + (t2 * 100 / t1) + "%\n\nNewRational is " + Math.Round(((double)t1 / t2), 1) + " times faster");
 
      //same test parallel
      sw.Start();
      Parallel.For(0, 50000, i => 
      {
        BigRational a = ff[i * 2 + 0], b = ff[i * 2 + 1];
        oldrationals[i * 2 + 0] = (a * b - a + b * b + b / 1234.5678M).Sign;
        oldrationals[i * 2 + 1] = a * b - b * b + a / Math.PI + a * 3 - b / 0.0001M;
      });
      sw.Stop(); t1 = sw.ElapsedMilliseconds; sw.Reset();
      sw.Start();
      Parallel.For(0, 50000, i =>
      { 
        NewRational a = ff[i * 2 + 0], b = ff[i * 2 + 1];
        newrationals[i * 2 + 0] = (a * b - a + b * b + b / 1234.5678M).Sign;
        newrationals[i * 2 + 1] = a * b - b * b + a / Math.PI + a * 3 - b / 0.0001M;
      });
      sw.Stop(); t2 = sw.ElapsedMilliseconds; sw.Reset();

      MessageBox.Show("Parallel:\nBigRational: " + t1 + "ms\n" + "NewRational: " + t2 + "ms\n" + (t2 * 100 / t1) + "%\n\nNewRational is " + Math.Round(((double)t1 / t2), 1) + " times faster");
 
    }

  }
}
