using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeAnalysis
{
    class Program
    {
        const ulong msTimeBucketSize = 15 * 60 * 1000; // 15 minutes
        static readonly DateTime dtZero = new DateTime(1970, 1, 1);

        static void Main(string[] args)
        {
            var sDir = (args.Length == 1) ? args[0] : ".";
            ulong msStart = 0;
            ulong msPrev = 0;
            ulong msDeltaMin = ulong.MaxValue;
            ulong msDeltaMax = 0;
            ulong msDeltaSum = 0;
            int nDeltas = 0;
            Console.WriteLine("time\tnSamples\tdTmin,ms\tdTavg,ms\tdTmax,ms");
            foreach (var sFile in Directory.EnumerateFiles(sDir).Where(s => s.EndsWith(".acclog.tsv")).OrderBy(s => s))
            {
                using (var fs = new FileStream(sFile, FileMode.Open))
                using (var rdr = new StreamReader(fs, Encoding.Default, false, 1024 * 1024))
                {
                    rdr.ReadLine(); // skip header
                    while (true)
                    {
                        var line = rdr.ReadLine();
                        if (line == null)
                            break;
                        var parts = line.Split('\t');
                        if (parts.Length < 1)
                            break;
                        var msTime = ulong.Parse(parts[0]);
                        if (msStart == 0)
                        {
                            msStart = msTime - (msTime % msTimeBucketSize);
                            msPrev = msTime;
                            continue;
                        }
                        var msDelta = msTime - msPrev;
                        if (msDelta < msTimeBucketSize)
                        {   // do not count big intervals between samples
                            if (msDelta < msDeltaMin)
                                msDeltaMin = msDelta;
                            if (msDelta > msDeltaMax)
                                msDeltaMax = msDelta;
                            msDeltaSum += msDelta;
                            nDeltas++;
                        }
                        if (msTime >= msStart + msTimeBucketSize)
                        {   // flush data
                            var dtStart = dtZero.AddMilliseconds(msStart);
                            var sStart = dtStart.ToString("yyyy-MM-dd HH:mm:ss");
                            Console.WriteLine($"{sStart}\t{nDeltas}\t{msDeltaMin}\t{(double)msDeltaSum / nDeltas:f1}\t{msDeltaMax}");
                            msDeltaMin = ulong.MaxValue;
                            msDeltaSum = 0;
                            msDeltaMax = 0;
                            nDeltas = 0;
                            msStart = msTime - (msTime % msTimeBucketSize);
                        }
                        msPrev = msTime;
                    }
                }
            }
        }
    }
}
