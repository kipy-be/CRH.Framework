using System;
using System.Collections.Generic;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public enum DataTrackEntriesOrder
    {
        DEFAULT = 0,
        NAME    = 2,
        LBA     = 1
    }

    public class DataTrackIndex
    {
        private DataTrackIndexEntry       m_root;
        private List<DataTrackIndexEntry> m_entries;
        private Dictionary<string, DataTrackIndexEntry> m_mappedEntries;

    // Constructors

        internal DataTrackIndex(DirectoryEntry root)
        {
            m_root          = new DataTrackIndexEntry(null, root);
            m_entries       = new List<DataTrackIndexEntry>();
            m_mappedEntries = new Dictionary<string, DataTrackIndexEntry>();
        }

    // Methods

        /// <summary>
        /// Add entry to index
        /// </summary>
        /// <param name="entry"></param>
        internal void AddToIndex(DataTrackIndexEntry entry)
        {
            if (m_mappedEntries.ContainsKey(entry.FullPath))
                throw new FrameworkException("Error while adding entry to index : entry \"{0}\" already exists", entry.FullPath);

            m_entries.Add(entry);
            m_mappedEntries.Add(entry.FullPath, entry);
        }

        /// <summary>
        /// Sort the index by entries's LBA
        /// </summary>
        private List<DataTrackIndexEntry> EntriesByLba()
        {
            List<DataTrackIndexEntry> sortedEntries = new List<DataTrackIndexEntry>(m_entries);
            sortedEntries.Sort((DataTrackIndexEntry e1, DataTrackIndexEntry e2) =>
            {
                return e1.Lba.CompareTo(e2.Lba);
            });
            return sortedEntries;
        }

        /// <summary>
        /// Sort the index by entries's Name
        /// </summary>
        private List<DataTrackIndexEntry> EntriesByName()
        {
            List<DataTrackIndexEntry> sortedEntries = new List<DataTrackIndexEntry>(m_entries);
            sortedEntries.Sort((DataTrackIndexEntry e1, DataTrackIndexEntry e2) =>
            {
                return e1.DirectoryEntry.Name.CompareTo(e2.DirectoryEntry.Name);
            });
            return sortedEntries;
        }

        /// <summary>
        /// Get entries
        /// </summary>
        /// <param name="sorting">sorting mode</param>
        /// <returns></returns>
        private List<DataTrackIndexEntry> GetEntriesList(DataTrackEntriesOrder order)
        {
            switch (order)
            {
                case DataTrackEntriesOrder.LBA:
                    return EntriesByLba();
                case DataTrackEntriesOrder.NAME:
                    return EntriesByName();
                default:
                    return m_entries;
            }
        }

        /// <summary>
        /// Get entries (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DataTrackIndexEntry> GetEntries(DataTrackEntriesOrder order = DataTrackEntriesOrder.DEFAULT)
        {
            foreach (DataTrackIndexEntry entry in GetEntriesList(order))
                yield return entry;
        }

        /// <summary>
        /// Get directory entries only (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DataTrackIndexEntry> GetDirectories(DataTrackEntriesOrder order = DataTrackEntriesOrder.DEFAULT)
        {
            foreach (DataTrackIndexEntry entry in GetEntriesList(order))
            {
                if(entry.IsDirectory)
                    yield return entry;
            }
        }

        /// <summary>
        /// Get file entries only (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DataTrackIndexEntry> GetFiles(DataTrackEntriesOrder order = DataTrackEntriesOrder.DEFAULT)
        {
            foreach (DataTrackIndexEntry entry in GetEntriesList(order))
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
        internal DataTrackIndexEntry GetEntry(string fullPath)
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
        internal DataTrackIndexEntry GetParent(string fullPath)
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
        internal DataTrackIndexEntry FindAParent(string fullPath)
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
                    return m_mappedEntries[fullPath];
                else
                    return null;
            }
        }
        
    // Accessors

        /// <summary>
        /// The index entry that represents the root of the files tree
        /// </summary>
        internal DataTrackIndexEntry Root
        {
            get { return m_root; }
        }
    }
}
