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

            //ExtractFiles();
            CreateIso();

            // --------------

            m_watch.Stop();
            Console.WriteLine("Terminated. Execution time : {0}", m_watch.Elapsed);
            Console.ReadLine();
            m_logs.Close();
            m_logs.Dispose();
        }

        /// <summary>
        /// Extract all files from ISO
        /// </summary>
        static void ExtractFiles()
        {
            try
            {
                // Hardcoded paths, yes, it's a test.

                DiskReader diskIn = new DiskReader(@"D:\Work\Traductions\Suikoden\CD\Suikoden_ORIGINAL.bin", IsoType.ISO9660, TrackMode.MODE2_XA);
                //DiskReader diskIn = new DiskReader(@"D:\Work\Traductions\Suikoden PSP\CD\Suikoden_PSP_JAP.iso", IsoType.ISO9660, DiskMode.RAW);

                string outPath = @"C:\Users\Kipy\Desktop\TestOut";

                diskIn.EntriesOrder = DiskEntriesOrder.LBA;
                foreach (DiskIndexEntry entry in diskIn.FileEntries)
                {
                    Console.WriteLine("Extracting {0}...", entry.FullPath);
                    diskIn.ExtractFile(entry.FullPath, outPath + entry.FullPath);
                }

                diskIn.Close();
            }
            catch(Exception ex)
            {
                Log("Error : {0}", ex.Message);
            }
        }

        static void CreateIso()
        {
            try
            {
                DiskReader diskIn = new DiskReader(@"D:\Work\Traductions\Suikoden\CD\Suikoden_ORIGINAL.bin", IsoType.ISO9660, TrackMode.MODE2_XA);
                DiskWriter diskOut = new DiskWriter(@"C:\Users\Kipy\Desktop\test.iso", IsoType.ISO9660, TrackMode.MODE2_XA);
         
                diskOut.Prepare
                (
                    "SUIKODEN-TEST",
                    ((int)diskIn.PrimaryVolumeDescriptor.PathTableSize / 2048) + 1,
                    (int)diskIn.PrimaryVolumeDescriptor.RootDirectoryEntry.ExtentSize / 2048
                );

                diskOut.CopySystemZone(diskIn);
                
                Stream ms;
                diskIn.EntriesOrder = DiskEntriesOrder.LBA;
                foreach (DiskIndexEntry entry in diskIn.Entries)
                {
                    Console.WriteLine("{0} {1}", entry.FullPath, (entry.IsDirectory ? "D" : "F") + (entry.IsStream ? "M" : ""));

                    if (entry.IsDirectory)
                        diskOut.CreateDirectory(entry.FullPath, (int)entry.Size / 2048);
                    else if (entry.IsStream)
                        diskOut.CopyStream(entry.FullPath, diskIn, entry);
                    else
                    {
                        ms = diskIn.ReadFile(entry.FullPath);
                        diskOut.WriteFile(entry.FullPath, ms);
                    }
                }

                diskOut.Finalize();
                diskOut.Close();

                diskIn.Close();
            }
            catch (Exception ex)
            {
                Log("Error : {0}", ex.Message);
            }
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
