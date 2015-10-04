using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRH.Framework.Disk.DataTrack
{
    internal enum XaSubModeFlag : byte
    {
        EOR           = 1,
        VIDEO         = 1 << 1,
        AUDIO         = 1 << 2,
        DATA          = 1 << 3,
        TRIGGER_ON    = 1 << 4,
        FORM2         = 1 << 5,
        REAL_TIME     = 1 << 6,
        EOF           = 1 << 7
    }

    public sealed class XaSubHeader
    {
        private byte m_file;
        private byte m_channel;
        private byte m_subMode;
        private byte m_dataType;

        private static XaSubHeader m_basicSubHeader;
        private static XaSubHeader m_endOfRecordSubHeader;
        private static XaSubHeader m_endOfFileSubHeader;

    // Constructors

        /// <summary>
        /// XA Subheader
        /// </summary>
        public XaSubHeader()
        {}

        /// <summary>
        /// XA Subheader
        /// </summary>
        public XaSubHeader(byte file, byte channel, byte subMode, byte dataType)
        {
            m_file     = file;
            m_channel  = channel;
            m_subMode  = subMode;
            m_dataType = dataType;
        }

    // Methods

        /// <summary>
        /// Get specific flag state from SubMode field
        /// </summary>
        /// <param name="mask">Le mask de l'attribut à lire</param>
        /// <returns></returns>
        private bool GetSubModeFlag(XaSubModeFlag mask)
        {
            return (m_subMode & (byte)mask) > 0;
        }

        /// <summary>
        /// Set attribute state into Attributes field
        /// </summary>
        /// <param name="mask">Le mask du l'attribut à écrire</param>
        /// <param name="value">La valeur</param>
        private void SetSubModeFlag(XaSubModeFlag mask, bool value)
        {
            if (value)
                m_subMode |= (byte)mask;
            else
                m_subMode &= (byte)(0xFF ^ (byte)mask);
        }

    // Accessors

        /// <summary>
        /// Basic subHeader (data)
        /// </summary>
        public static XaSubHeader Basic
        {
            get
            {
                if (m_basicSubHeader == null)
                {
                    m_basicSubHeader = new XaSubHeader();
                    m_basicSubHeader.IsData = true;
                }
                return m_basicSubHeader;
            }
        }

        /// <summary>
        /// Basic end of record subheader (data, end of record)
        /// </summary>
        public static XaSubHeader EndOfRecord
        {
            get
            {
                if (m_endOfRecordSubHeader == null)
                {
                    m_endOfRecordSubHeader = new XaSubHeader();
                    m_endOfRecordSubHeader.IsData = true;
                    m_endOfRecordSubHeader.IsEOR = true;
                }
                return m_endOfRecordSubHeader;
            }
        }

        /// <summary>
        /// Basic end of file subheader (data, end of record, end of file)
        /// </summary>
        public static XaSubHeader EndOfFile
        {
            get
            {
                if (m_endOfFileSubHeader == null)
                {
                    m_endOfFileSubHeader = new XaSubHeader();
                    m_endOfFileSubHeader.IsData = true;
                    m_endOfFileSubHeader.IsEOF = true;
                    m_endOfFileSubHeader.IsEOR = true;
                }
                return m_endOfFileSubHeader;
            }
        }

        /// <summary>
        /// File
        /// </summary>
        public byte File
        {
            get { return m_file; }
            internal set { m_file = value; }
        }

        /// <summary>
        /// Channel
        /// </summary>
        public byte Channel
        {
            get { return m_channel; }
            internal set { m_channel = value; }
        }

        /// <summary>
        /// Sub-mode
        /// </summary>
        public byte SubMode
        {
            get { return m_subMode; }
            internal set { m_subMode = value; }
        }

        /// <summary>
        /// Is End of record
        /// </summary>
        public bool IsEOR
        {
            get { return GetSubModeFlag(XaSubModeFlag.EOR); }
            set { SetSubModeFlag(XaSubModeFlag.EOR, value); }
        }

        /// <summary>
        /// Contains audio
        /// </summary>
        public bool IsAudio
        {
            get { return GetSubModeFlag(XaSubModeFlag.AUDIO); }
            set { SetSubModeFlag(XaSubModeFlag.AUDIO, value); }
        }

        /// <summary>
        /// Contains video
        /// </summary>
        public bool IsVideo
        {
            get { return GetSubModeFlag(XaSubModeFlag.VIDEO); }
            set { SetSubModeFlag(XaSubModeFlag.VIDEO, value); }
        }

        /// <summary>
        /// Contains data
        /// </summary>
        public bool IsData
        {
            get { return GetSubModeFlag(XaSubModeFlag.DATA); }
            set { SetSubModeFlag(XaSubModeFlag.DATA, value); }
        }

        /// <summary>
        /// Trigger on (OS dependant)
        /// </summary>
        public bool TriggerOn
        {
            get { return GetSubModeFlag(XaSubModeFlag.TRIGGER_ON); }
            set { SetSubModeFlag(XaSubModeFlag.TRIGGER_ON, value); }
        }

        /// <summary>
        /// Is Form 2
        /// </summary>
        public bool IsForm2
        {
            get { return GetSubModeFlag(XaSubModeFlag.FORM2); }
            set { SetSubModeFlag(XaSubModeFlag.FORM2, value); }
        }

        /// <summary>
        /// Is real time data
        /// </summary>
        public bool IsRealTime
        {
            get { return GetSubModeFlag(XaSubModeFlag.REAL_TIME); }
            set { SetSubModeFlag(XaSubModeFlag.REAL_TIME, value); }
        }

        /// <summary>
        /// Is End of file
        /// </summary>
        public bool IsEOF
        {
            get { return GetSubModeFlag(XaSubModeFlag.EOF); }
            set { SetSubModeFlag(XaSubModeFlag.EOF, value); }
        }

        /// <summary>
        /// Data type
        /// </summary>
        public byte DataType
        {
            get { return m_dataType; }
            internal set { m_dataType = value; }
        }
    }
}
