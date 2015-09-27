using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRH.Framework.Disk
{
    public sealed class XaSubHeader
    {
        private byte m_file;
        private byte m_channel;
        private byte m_subMode;
        private byte m_dataType;

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

    // Accessors

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
        /// Data type
        /// </summary>
        public byte DataType
        {
            get { return m_dataType; }
            internal set { m_dataType = value; }
        }
    }
}
