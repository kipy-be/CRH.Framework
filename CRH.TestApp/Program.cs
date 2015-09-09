using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using CRH.Framework.Common;
using CRH.Framework.Disk;
using CRH.Framework.Utils;

namespace CRH.TestApp
{
    /// <summary>
    /// Little console application for framework testing purposes
    /// </summary>
    class Program
    {
        static StreamWriter m_logs;
        static Stopwatch    m_watch;

        static void Main(string[] args)
        {
            m_logs  = new StreamWriter(new FileStream("out.log", FileMode.Create, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8);
            m_watch = new Stopwatch();
            m_watch.Start();

            Console.WriteLine(Infos.About());
            Console.WriteLine("Framework's test application");

            // Testing stuffs

            ExtractFiles();
            //TestBcd();

            // --------------

            m_watch.Stop();
            Console.WriteLine("Terminated. Execution time : {0}", m_watch.Elapsed);
            Console.ReadLine();
            m_logs.Close();
            m_logs.Dispose();
        }

        static void ExtractFiles()
        {
            try
            {
                // Hardcoded paths, yes, it's a test.

                DiskReader diskIn = new DiskReader(@"D:\Work\Traductions\Suikoden\CD\Suikoden_ORIGINAL.bin", IsoType.ISO9660, DiskMode.MODE2_XA);
                //DiskReader diskIn = new DiskReader(@"D:\Work\Traductions\Suikoden PSP\CD\Suikoden_PSP_JAP.iso", IsoType.ISO9660, DiskMode.RAW);

                string outPath = @"C:\Users\Kipy\Desktop\TestOut";

                foreach(DiskIndexEntry entry in diskIn.FileEntries)
                    diskIn.ExtractFile(entry.FullPath, outPath + entry.FullPath);

                diskIn.Close();
            }
            catch(Exception ex)
            {
                Log("Error : {0}", ex.Message);
            }
        }

        static void TestBcd()
        {
            byte dec = 0;
            do
            {
                string hex = Converter.DecToHex(dec, 2);
                byte bcd = Converter.DecToBcd(dec);
                string bcdHex = Converter.DecToHex(bcd, 2);
                byte decFromBcd = Converter.BcdToDec(bcd);
                string decFromBcdHex = Converter.DecToHex(bcd, 2);
                Console.WriteLine("dec = {0} ({1})\tbcd = {2} ({3})\tback = {4} ({5})", dec, hex, bcd, bcdHex, decFromBcd, decFromBcdHex);
                dec++;
            }
            while (dec < 128);
        }

    // Logs

        static void Log(string str)
        {
            Console.WriteLine(str);
            m_logs.WriteLine(str);
            m_logs.Flush();
        }

        static void Log(string str, params object[] prms)
        {
            Console.WriteLine(str, prms);
            m_logs.WriteLine(str, prms);
            m_logs.Flush();
        }
    }
}
