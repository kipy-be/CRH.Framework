using System;
using System.Collections.Generic;
using System.IO;
using CRH.Framework.Common;
using CRH.Framework.IO;
using CRH.Framework.Utils;

namespace CRH.Framework.Disk
{
    public sealed class DiskWriter : DiskBase
    {
        private CBinaryWriter m_stream;
        private DiskIndex m_index;

        private bool m_finalized;
        private bool m_indexBuilt;

    // Constructors

        /// <summary>
        /// DiskWriter
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file to create</param>
        /// <param name="type">Type of ISO (ISO9660 / ISO9660_UDF)</param>
        /// <param name="mode">Disk mode</param>
        public DiskWriter(string fileUrl, IsoType type, TrackMode mode, bool overwriteIfExists = true)
            : base(fileUrl, type, mode)
        {
            m_finalized  = false;
            m_indexBuilt = false;

            try
            {
                m_file       = new FileInfo(m_fileUrl);
                m_fileStream = new FileStream(m_file.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);
                m_stream     = new CBinaryWriter(m_fileStream);
                m_fileOpen   = true;
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while while writing ISO : Unable to create the ISO File");
            }
        }

    // Methods

        /// <summary>
        /// Close the file and dispose it
        /// </summary>
        public override void Close()
        {
            if (!m_fileOpen)
                return;

            if (!m_finalized)
                throw new FrameworkException("Error while closing ISO : ISO is not finalized, it will be unreadable");

            m_stream.CloseAndDispose();
            m_fileOpen = false;
        }


        /// <summary>
        /// Finalise the disk (write descriptors, path table, etc.)
        /// </summary>
        public void Finalize()
        {
            if (m_finalized)
                return;

            // tmp

            m_finalized = true;
        }

        /// <summary>
        /// Write a sector
        /// </summary>
        /// <param name="data">The sector's data to write</param>
        /// <param name="mode">Sector's mode</param>
        public void WriteSector(byte[] data, SectorMode mode)
        {
            try
            {
                byte[] buffer = new byte[m_sectorSize];
                using (CBinaryWriter bufferStream = new CBinaryWriter(buffer))
                {
                    if (mode != SectorMode.RAW)
                    {
                        bufferStream.Write(SYNC);
                        long position = SectorPosition + 150;
                        byte[] header = new byte[4];
                        header[3] = (byte)(mode == SectorMode.MODE0 ? 0 : mode == SectorMode.MODE1 ? 1 : 2);
                        header[2] = Converter.DecToBcd((byte)(position % 75)); position /= 75;
                        header[1] = Converter.DecToBcd((byte)(position % 60)); position /= 60;
                        header[0] = Converter.DecToBcd((byte)(position % 60));
                        bufferStream.Write(header); 
                    }

                    bufferStream.Write(data, 0, data.Length);

                    if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                        EccEdc.EccEdcCompute(buffer, mode);

                    m_stream.Write(buffer);
                }
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while writing sector : unable to write sector");
            }
        }

        /// <summary>
        /// Write a sector at the specified lba
        /// </summary>
        /// <param name="lba">Sector's LBA</param>
        /// <param name="sector">The sector to write</param>
        /// <param name="mode">Sector's mode</param>
        public void WriteSector(long lba, byte[] sector, SectorMode mode)
        {
            SeekSector(lba);
            WriteSector(sector, mode);
        }

        /// <summary>
        /// Write an empty sector
        /// </summary>
        /// <param name="mode">Sector's mode</param>
        public void WriteEmptySector()
        {
            if(m_mode == TrackMode.RAW)
                WriteSector(new byte[2048], SectorMode.RAW);
            else
                WriteSector(new byte[2048], SectorMode.MODE0);
        }

        /// <summary>
        /// Write empty sectors
        /// </summary>
        /// <param name="count">Number of sectors to write</param>
        public void WriteEmptySectors(int count)
        {
            byte[] data = new byte[2048];
            if (m_mode == TrackMode.RAW)
            {
                for (int i = 0; i < count; i++)
                    WriteSector(data, SectorMode.RAW);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    WriteSector(data, SectorMode.MODE0);
            }
        }
    }
}
