using System;
using System.Collections.Generic;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public class DataTrackIndexEntry
    {
        private DirectoryEntry m_directoryEntry;
        private DataTrackIndexEntry m_parentEntry;
        private List<DataTrackIndexEntry> m_subEntries = null;

        private string m_fullPath;
        private bool m_isRoot;
        private uint m_directoryAvailableSpace = 0;

        // Constructors

        /// <summary>
        /// TreeEntry
        /// </summary>
        /// <param name="directoryEntry">The directory entry that this tree entry refers to</param>
        internal DataTrackIndexEntry(DataTrackIndexEntry parent, DirectoryEntry directoryEntry)
        {
            m_parentEntry = parent;
            m_isRoot = (parent == null);
            m_directoryEntry = directoryEntry;

            if (!m_isRoot)
                m_fullPath = parent.FullPath
                                + (parent.FullPath != "/" ? "/" : "")
                                + directoryEntry.Name;
            else
                m_fullPath = "/";

            if (IsDirectory)
            {
                m_subEntries = new List<DataTrackIndexEntry>();

                if (!m_isRoot)
                    m_directoryAvailableSpace = directoryEntry.ExtentSize
                                                - parent.DirectoryEntry.Length
                                                - parent.DirectoryEntry.ExtendedAttributeRecordlength
                                                - directoryEntry.Length;
                else
                    m_directoryAvailableSpace = directoryEntry.ExtentSize
                                                - directoryEntry.Length
                                                - directoryEntry.ExtendedAttributeRecordlength
                                                - directoryEntry.Length;

            }

            if (parent != null)
                parent.Add(this);
        }

        // Methods

        /// <summary>
        /// Add sub entry to this entry (add file / folder to folder)
        /// </summary>
        /// <param name="subEntry">The entry to add</param>
        private void Add(DataTrackIndexEntry subEntry)
        {
            if (!IsDirectory)
                throw new FrameworkException("Error while adding entry to directory : entry \"{0}\" is not a directory", m_fullPath);

            if (subEntry.DirectoryEntry.Length > m_directoryAvailableSpace)
                throw new FrameworkException("Error while adding entry to directory : directory \"{0}\" is too small", m_fullPath);

            m_subEntries.Add(subEntry);
            m_directoryAvailableSpace -= subEntry.DirectoryEntry.Length;
        }

        // Accessors

        /// <summary>
        /// The directory entry that this tree entry refers to
        /// </summary>
        internal DirectoryEntry DirectoryEntry
        {
            get { return m_directoryEntry; }
            set { m_directoryEntry = value; }
        }

        /// <summary>
        /// Is this entry root
        /// </summary>
        internal bool IsRoot
        {
            get { return m_isRoot; }
        }

        /// <summary>
        /// The parent tree entry (null if root)
        /// </summary>
        public DataTrackIndexEntry ParentEntry
        {
            get { return m_parentEntry; }
            set { m_parentEntry = value; }
        }

        /// <summary>
        /// The entries contained in this entry
        /// Value : null if not directory
        /// </summary>
        internal List<DataTrackIndexEntry> SubEntries
        {
            get { return m_subEntries; }
        }

        /// <summary>
        /// The directory available space (used only in writing mode)
        /// </summary>
        internal uint DirectoryAvailableSpace
        {
            get { return m_directoryAvailableSpace; }
            set { m_directoryAvailableSpace = value; }
        }

        /// <summary>
        /// The number of files/directories contained in the directory
        /// Value : -1 if not directory
        /// </summary>
        public int SubEntriesCount
        {
            get { return m_subEntries == null ? -1 : m_subEntries.Count; }
        }

        /// <summary>
        /// is Directory
        /// </summary>
        public bool IsDirectory
        {
            get { return m_directoryEntry.IsDirectory; }
        }

        public bool IsStream
        {
            get { return m_directoryEntry.HasXa ? m_directoryEntry.XaEntry.IsForm2 : false; }
        }

        /// <summary>
        /// Size of the entry
        /// </summary>
        public uint Size
        {
            get { return m_directoryEntry.ExtentSize; }
        }

        /// <summary>
        /// Length of the DirectoryEntry
        /// </summary>
        internal uint Length
        {
            get { return m_directoryEntry.Length; }
        }

        /// <summary>
        /// Lba of the entry
        /// </summary>
        public uint Lba
        {
            get { return m_directoryEntry.ExtentLba; }
        }

        /// <summary>
        /// The full path of the file/directory that this entry refers to
        /// eg : /FOLDER/SUB/FILE.EXT
        /// </summary>
        public string FullPath
        {
            get { return m_fullPath; }
            set { m_fullPath = value; }
        }
    }
}
