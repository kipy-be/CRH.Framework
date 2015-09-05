using System;
using CRH.Framework.Common;
using CRH.Framework.IO;

namespace CRH.Framework.Disk
{
    internal enum PathTableType
    {
        L_PATH_TABLE = 1,
        M_PATH_TABLE = 2
    }

    internal sealed class PathTableEntry
    {
        private PathTableType m_type;
        private byte   m_directoryIdLength;
        private byte   m_extendedAttributeRecordlength;
        private uint   m_extentLBA;
        private ushort m_parentDirectoryNumber;
        private string m_directoryId;

    // Constructors

        /// <summary>
        /// Path table entry
        /// </summary>
        /// <param name="type">Type of path table</param>
        public PathTableEntry(PathTableType type)
        {
            Type = type;
        }

    // Accessors

        /// <summary>
        /// Type of path table (LPathTable or MPathTable)
        /// LPathTable = values are stored in little endian
        /// MPatchTable = values are stored in big endian
        /// </summary>
        public PathTableType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        /// <summary>
        /// Size of directory's name
        /// </summary>
        public byte DirectoryIdLength
        {
            get { return m_directoryIdLength; }
            set { m_directoryIdLength = value; }
        }

        /// <summary>
        /// Size of the extended attribute area
        /// </summary>
        public byte ExtendedAttributeRecordlength
        {
            get { return m_extendedAttributeRecordlength; }
            set { m_extendedAttributeRecordlength = value; }
        }

        /// <summary>
        /// ExtentLBA
        /// </summary>
        public uint ExtentLBA
        {
            get { return m_extentLBA; }
            set { m_extentLBA = value; }
        }

        /// <summary>
        /// Number of parent directory
        /// </summary>
        public ushort ParentDirectoryNumber
        {
            get { return m_parentDirectoryNumber; }
            set { m_parentDirectoryNumber = value; }
        }

        /// <summary>
        /// Name of the directory
        /// </summary>
        public string DirectoryId
        {
            get { return m_directoryId; }
            set { m_directoryId = value; }
        }
    }
}