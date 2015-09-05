using System;
using System.Collections.Generic;

namespace CRH.Framework.Disk
{
    public class DiskIndex
    {
        private DiskIndexEntry       m_root;
        private List<DiskIndexEntry> m_entries;

    // Constructors

        internal DiskIndex(DiskIndexEntry root)
        {
            root.FullPath = "/";
            m_root    = root;
            m_entries = new List<DiskIndexEntry>();
        }

    // Methods

        /// <summary>
        /// Add entry to index
        /// </summary>
        /// <param name="entry"></param>
        internal void AddToIndex(DiskIndexEntry entry)
        {
            m_entries.Add(entry);
        }

        /// <summary>
        /// Get directory entries only (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DiskIndexEntry> GetDirectories()
        {
            foreach(DiskIndexEntry entry in m_entries)
            {
                if(entry.IsDIrectory)
                    yield return entry;
            }
        }

        /// <summary>
        /// Get file entries only (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DiskIndexEntry> GetFiles()
        {
            foreach (DiskIndexEntry entry in m_entries)
            {
                if (!entry.IsDIrectory)
                    yield return entry;
            }
        }

        /// <summary>
        /// Get specific entry based on path
        /// </summary>
        /// <param name="fullPath">The full path of the entry (eg : /FOLDER/SUB/FILE.EXT)</param>
        /// <returns></returns>
        internal DiskIndexEntry GetEntry(string fullPath)
        {
            foreach (DiskIndexEntry entry in m_entries)
            {
                if (entry.FullPath == fullPath)
                    return entry;
            }

            return null;
        }
        
    // Accessors

        /// <summary>
        /// The index entry that represents the root of the files tree
        /// </summary>
        internal DiskIndexEntry Root
        {
            get { return m_root; }
        }

        /// <summary>
        /// Entries
        /// </summary>
        internal IEnumerable<DiskIndexEntry> Entries
        {
            get { return m_entries; }
        }
       
    }

    public class DiskIndexEntry
    {
        private DirectoryEntry       m_directoryEntry;
        private DiskIndexEntry       m_parentEntry;
        private List<DiskIndexEntry> m_subEntries = null;

        private string m_fullPath;
        
    // Constructors

        /// <summary>
        /// TreeEntry
        /// </summary>
        /// <param name="directoryEntry">The directory entry that this tree entry refers to</param>
        internal DiskIndexEntry(DiskIndexEntry parent, DirectoryEntry directoryEntry)
        {
            m_parentEntry    = parent;
            m_directoryEntry = directoryEntry;

            if (m_parentEntry != null)
                m_fullPath = parent.FullPath
                                + (parent.FullPath != "/" ? "/" : "")
                                + directoryEntry.Name;

            if (IsDIrectory)
                m_subEntries = new List<DiskIndexEntry>();
        }

    // Methods

        internal void Add(DiskIndexEntry child)
        {
            m_subEntries.Add(child);
        }

        internal void AddRange(IEnumerable<DiskIndexEntry> children)
        {
            m_subEntries.AddRange(children);
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
        /// The parent tree entry (null if root)
        /// </summary>
        public DiskIndexEntry ParentEntry
        {
            get { return m_parentEntry; }
            set { m_parentEntry = value; }
        }

        /// <summary>
        /// The entries contained in this entry
        /// Value : null if not directory
        /// </summary>
        internal List<DiskIndexEntry> SubEntries
        {
            get { return m_subEntries; }
        }

        /// <summary>
        /// The number of files contained in the directory
        /// Value : -1 if not directory
        /// </summary>
        public int ChildrenCount
        {
            get { return m_subEntries == null ? -1 : m_subEntries.Count; }
        }

        /// <summary>
        /// is Directory
        /// </summary>
        public bool IsDIrectory
        {
            get { return m_directoryEntry.IsDirectory; }
        }

        /// <summary>
        /// Size of the entry
        /// </summary>
        public long Size
        {
            get { return m_directoryEntry.ExtentSize; }
        }

        /// <summary>
        /// Lba of the entry
        /// </summary>
        public long Lba
        {
            get { return m_directoryEntry.ExtentLba;  }
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
