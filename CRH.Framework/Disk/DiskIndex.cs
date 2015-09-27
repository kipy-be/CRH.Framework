using System;
using System.Collections.Generic;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public enum DiskEntriesOrder
    {
        DEFAULT = 0,
        NAME    = 2,
        LBA     = 1
    }

    public class DiskIndex
    {
        private DiskIndexEntry       m_root;
        private List<DiskIndexEntry> m_entries;
        private Dictionary<string, DiskIndexEntry> m_mappedEntries;

    // Constructors

        internal DiskIndex(DirectoryEntry root, bool isXa)
        {
            root.Length    += (byte)(isXa ? XaEntry.SIZE : 0);
            m_root          = new DiskIndexEntry(null, root);
            m_root.FullPath = "/";
            m_entries       = new List<DiskIndexEntry>();
            m_mappedEntries = new Dictionary<string, DiskIndexEntry>();
        }

    // Methods

        /// <summary>
        /// Add entry to index
        /// </summary>
        /// <param name="entry"></param>
        internal void AddToIndex(DiskIndexEntry entry)
        {
            if (m_mappedEntries.ContainsKey(entry.FullPath))
                throw new FrameworkException("Error while adding entry to index : entry \"{0}\" already exists", entry.FullPath);

            m_entries.Add(entry);
            m_mappedEntries.Add(entry.FullPath, entry);
        }

        /// <summary>
        /// Sort the index by entries's LBA
        /// </summary>
        private List<DiskIndexEntry> EntriesByLba()
        {
            List<DiskIndexEntry> sortedEntries = new List<DiskIndexEntry>(m_entries);
            sortedEntries.Sort((DiskIndexEntry e1, DiskIndexEntry e2) =>
            {
                return e1.Lba.CompareTo(e2.Lba);
            });
            return sortedEntries;
        }

        /// <summary>
        /// Sort the index by entries's Name
        /// </summary>
        private List<DiskIndexEntry> EntriesByName()
        {
            List<DiskIndexEntry> sortedEntries = new List<DiskIndexEntry>(m_entries);
            sortedEntries.Sort((DiskIndexEntry e1, DiskIndexEntry e2) =>
            {
                return e1.FullPath.CompareTo(e2.FullPath);
            });
            return sortedEntries;
        }

        /// <summary>
        /// Get entries
        /// </summary>
        /// <param name="sorting">sorting mode</param>
        /// <returns></returns>
        private List<DiskIndexEntry> GetEntriesList(DiskEntriesOrder order)
        {
            switch (order)
            {
                case DiskEntriesOrder.LBA:
                    return EntriesByLba();
                case DiskEntriesOrder.NAME:
                    return EntriesByName();
                default:
                    return m_entries;
            }
        }

        /// <summary>
        /// Get entries (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DiskIndexEntry> GetEntries(DiskEntriesOrder order = DiskEntriesOrder.DEFAULT)
        {
            foreach (DiskIndexEntry entry in GetEntriesList(order))
                yield return entry;
        }

        /// <summary>
        /// Get directory entries only (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DiskIndexEntry> GetDirectories(DiskEntriesOrder order = DiskEntriesOrder.DEFAULT)
        {
            foreach (DiskIndexEntry entry in GetEntriesList(order))
            {
                if(entry.IsDirectory)
                    yield return entry;
            }
        }

        /// <summary>
        /// Get file entries only (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DiskIndexEntry> GetFiles(DiskEntriesOrder order = DiskEntriesOrder.DEFAULT)
        {
            foreach (DiskIndexEntry entry in GetEntriesList(order))
            {
                if (!entry.IsDirectory)
                    yield return entry;
            }
        }

        /// <summary>
        /// Get specific entry based on its path
        /// </summary>
        /// <param name="fullPath">The full path of the entry (eg : /FOLDER/SUB/FILE.EXT)</param>
        /// <returns></returns>
        internal DiskIndexEntry GetEntry(string fullPath)
        {
            if (m_mappedEntries.ContainsKey(fullPath))
                return m_mappedEntries[fullPath];
            else
                return null;
        }

        /// <summary>
        /// Get parent of specific entry based on its path
        /// </summary>
        /// <param name="fullPath">The full path of the entry (eg : /FOLDER/SUB/FILE.EXT)</param>
        /// <returns></returns>
        internal DiskIndexEntry GetParent(string fullPath)
        {
            if (m_mappedEntries.ContainsKey(fullPath))
                return m_mappedEntries[fullPath].ParentEntry;
            else
                return null;
        }

        /// <summary>
        /// Get the potential parent of a an entry based on its potential path
        /// </summary>
        /// <param name="fullPath">The full path of the potential entry (eg : /FOLDER/SUB/FILE.EXT)</param>
        /// <returns></returns>
        internal DiskIndexEntry FindAParent(string fullPath)
        {
            int lIndex = fullPath.LastIndexOf('/');

            if (lIndex == fullPath.Length - 1)
                lIndex = fullPath.LastIndexOf('/', lIndex - 1);

            if (lIndex == 0)
                return m_root;
            else
            {
                fullPath = fullPath.Substring(0, lIndex);
                if (m_mappedEntries.ContainsKey(fullPath))
                    return m_mappedEntries[fullPath].ParentEntry;
                else
                    return null;
            }
        }
        
    // Accessors

        /// <summary>
        /// The index entry that represents the root of the files tree
        /// </summary>
        internal DiskIndexEntry Root
        {
            get { return m_root; }
        }
    }

    public class DiskIndexEntry
    {
        private DirectoryEntry       m_directoryEntry;
        private DiskIndexEntry       m_parentEntry;
        private List<DiskIndexEntry> m_subEntries = null;

        private string m_fullPath;
        private bool   m_isRoot;
        private uint   m_directoryAvailableSpace = 0;
        
    // Constructors

        /// <summary>
        /// TreeEntry
        /// </summary>
        /// <param name="directoryEntry">The directory entry that this tree entry refers to</param>
        internal DiskIndexEntry(DiskIndexEntry parent, DirectoryEntry directoryEntry)
        {
            m_parentEntry    = parent;
            m_isRoot         = (parent == null);
            m_directoryEntry = directoryEntry;

            if (!m_isRoot)
                m_fullPath = parent.FullPath
                                + (parent.FullPath != "/" ? "/" : "")
                                + directoryEntry.Name;

            if (IsDirectory)
            {
                m_subEntries = new List<DiskIndexEntry>();

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
        }

    // Methods

        /// <summary>
        /// Add sub entry to this entry (add file / folder to folder)
        /// </summary>
        /// <param name="child">The entry to add</param>
        internal void Add(DiskIndexEntry child)
        {
            if(!IsDirectory)
                throw new FrameworkException("Error while adding entry to directory : entry \"{0}\" is not a directory", m_fullPath);

            if (child.DirectoryEntry.Length > m_directoryAvailableSpace)
                throw new FrameworkException("Error while adding entry to directory : directory \"{0}\" is too small", m_fullPath);

            m_subEntries.Add(child);
            m_directoryAvailableSpace -= child.DirectoryEntry.Length;
        }

        /// <summary>
        /// Add sub entries to this entry (add files / folders to folder)
        /// </summary>
        /// <param name="children">The entries to add</param>
        internal void AddRange(IEnumerable<DiskIndexEntry> children)
        {
            if (!IsDirectory)
                throw new FrameworkException("Error while adding entries to directory : entry \"{0}\" is not a directory", m_fullPath);

            uint totalSize = 0;
            foreach (DiskIndexEntry child in children)
                totalSize += child.DirectoryEntry.Length;

            if (totalSize > m_directoryAvailableSpace)
                throw new FrameworkException("Error while adding entries to directory : directory \"{0}\" is too small", m_fullPath);

            m_subEntries.AddRange(children);
            m_directoryAvailableSpace -= totalSize;
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
        /// The directory available space (used only in writing mode)
        /// </summary>
        internal uint DirectoryAvailableSpace
        {
            get { return m_directoryAvailableSpace; }
            set { m_directoryAvailableSpace = value; }
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
        public bool IsDirectory
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
