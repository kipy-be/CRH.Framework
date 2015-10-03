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
                DiskReader diskIn = DiskReader.InitSingleTrack(@"D:\Work\Traductions\Suikoden\CD\Suikoden_ORIGINAL.bin", DataTrackSystem.ISO9660, DataTrackMode.MODE2_XA);
                string outPath = @"C:\Users\Kipy\Desktop\TestOut";
                
                DataTrackReader trackIn = (DataTrackReader)diskIn.Track;
                foreach (DataTrackIndexEntry entry in trackIn.FileEntries)
                {
                    Console.WriteLine("Extracting {0}...", entry.FullPath);
                    trackIn.ExtractFile(entry.FullPath, outPath + entry.FullPath);
                }

                diskIn.Close();
            }
            catch (Exception ex)
            {
                Log("Error : {0}", ex.Message);
            }
        }

        static void CreateIso()
        {
            try
            {
                DiskReader diskIn = DiskReader.InitSingleTrack(@"D:\Work\Traductions\Suikoden\CD\Suikoden_ORIGINAL.bin", DataTrackSystem.ISO9660, DataTrackMode.MODE2_XA);
                DiskWriter diskOut = DiskWriter.InitSingleTrack(@"C:\Users\Kipy\Desktop\test.iso", DataTrackSystem.ISO9660, DataTrackMode.MODE2_XA);

                DataTrackReader trackIn  = (DataTrackReader)diskIn.Track;
                DataTrackWriter trackOut = (DataTrackWriter)diskOut.Track;

                trackOut.Prepare
                (
                    "SUIKODEN-TEST",
                    ((int)trackIn.PrimaryVolumeDescriptor.PathTableSize / 2048) + 1,
                    (int)trackIn.PrimaryVolumeDescriptor.RootDirectoryEntry.ExtentSize / 2048
                );

                trackOut.CopySystemZone(trackIn);

                Stream ms;
                trackIn.EntriesOrder = DataTrackEntriesOrder.LBA;
                foreach (DataTrackIndexEntry entry in trackIn.Entries)
                {
                    Console.WriteLine("{0} {1}", entry.FullPath, (entry.IsDirectory ? "D" : "F") + (entry.IsStream ? "M" : ""));

                    if (entry.IsDirectory)
                        trackOut.CreateDirectory(entry.FullPath, (int)entry.Size / 2048);
                    else if (entry.IsStream)
                        trackOut.CopyStream(entry.FullPath, trackIn, entry);
                    else
                    {
                        ms = trackIn.ReadFile(entry.FullPath);
                        trackOut.WriteFile(entry.FullPath, ms);
                    }
                }

                trackOut.Finalize();
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
