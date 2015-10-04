using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using CRH.Framework.Common;
using CRH.Framework.Disk;
using CRH.Framework.Disk.AudioTrack;
using CRH.Framework.Disk.DataTrack;
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
            //ExtractMultiTracks();
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
                DiskReader diskIn = DiskReader.InitSingleTrack(@"D:\Work\Traductions\Suikoden\CD\Suikoden_ORIGINAL.bin", DiskFileSystem.ISO9660, DataTrackMode.MODE2_XA, false, false);
                string outPath = @"C:\Users\Kipy\Desktop\TestOut";
                
                DataTrackReader trackIn = (DataTrackReader)diskIn.Track;
                trackIn.ReadVolumeDescriptors();
                trackIn.BuildIndex();
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

        /// <summary>
        /// Extract all files from ISO multi tracks
        /// </summary>
        static void ExtractMultiTracks()
        {
            try
            {
                DiskReader diskIn = DiskReader.InitMultiTracks(@"C:\Users\Kipy\Desktop\Tests\207 GENSO SUIKODEN (J).cue", DiskFileSystem.ISO9660);
                string outPath = @"C:\Users\Kipy\Desktop\TestOut";

                foreach (Track trackIn in diskIn.Tracks)
                {
                    if (trackIn.IsData)
                    {
                        DataTrackReader dataTrackIn = (DataTrackReader)trackIn;
                        dataTrackIn.ReadVolumeDescriptors();
                        dataTrackIn.BuildIndex();
                        foreach (DataTrackIndexEntry entry in dataTrackIn.FileEntries)
                        {
                            Console.WriteLine("Extracting {0}...", entry.FullPath);
                            dataTrackIn.ExtractFile(entry.FullPath, outPath + @"\DATA\" + entry.FullPath);
                        }
                    }
                    else if(trackIn.IsAudio)
                    {
                        AudioTrackReader audioTrackIn = (AudioTrackReader)trackIn;
                        audioTrackIn.Extract(outPath + @"\AUDIO\AUDIO_" + trackIn.TrackNumber + ".WAV", AudioFileContainer.WAVE);
                    }
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
                DiskReader diskIn = DiskReader.InitSingleTrack(@"D:\Work\Traductions\Suikoden\CD\Suikoden_ORIGINAL.bin", DiskFileSystem.ISO9660, DataTrackMode.MODE2_XA);
                DiskWriter diskOut = DiskWriter.InitSingleTrack(@"C:\Users\Kipy\Desktop\test.iso", DiskFileSystem.ISO9660, DataTrackMode.MODE2_XA);

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
