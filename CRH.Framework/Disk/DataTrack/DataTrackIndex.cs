using CRH.Framework.Common;
using System.Collections.Generic;

namespace CRH.Framework.Disk.DataTrack
{
    public class DataTrackIndex
    {
        private DataTrackIndexEntry       _root;
        private List<DataTrackIndexEntry> _entries;
        private Dictionary<string, DataTrackIndexEntry> _mappedEntries;
        
        private int _entriesCount;
        private int _directoryEntriesCount;
        private int _fileEntriesCount;

        internal DataTrackIndex(DirectoryEntry root)
        {
            _root          = new DataTrackIndexEntry(null, root);
            _entries       = new List<DataTrackIndexEntry>();
            _mappedEntries = new Dictionary<string, DataTrackIndexEntry>();

            _entriesCount          = 0;
            _directoryEntriesCount = 0;
            _fileEntriesCount      = 0;
        }

        /// <summary>
        /// Add entry to index
        /// </summary>
        /// <param name="entry"></param>
        internal void AddToIndex(DataTrackIndexEntry entry)
        {
            if (_mappedEntries.ContainsKey(entry.FullPath))
            {
                throw new FrameworkException("Error while adding entry to index : entry \"{0}\" already exists", entry.FullPath);
            }

            _entriesCount++;

            if (entry.IsDirectory)
            {
                _directoryEntriesCount++;
            }
            else
            {
                _fileEntriesCount++;
            }

            _entries.Add(entry);
            _mappedEntries.Add(entry.FullPath, entry);
        }

        /// <summary>
        /// Sort the index by entries's LBA
        /// </summary>
        private List<DataTrackIndexEntry> EntriesByLba()
        {
            var sortedEntries = new List<DataTrackIndexEntry>(_entries);

            sortedEntries.Sort(
                (DataTrackIndexEntry e1, DataTrackIndexEntry e2) => e1.Lba.CompareTo(e2.Lba)
            );

            return sortedEntries;
        }

        /// <summary>
        /// Sort the index by entries's Name
        /// </summary>
        private List<DataTrackIndexEntry> EntriesByName()
        {
            var sortedEntries = new List<DataTrackIndexEntry>(_entries);

            sortedEntries.Sort(
                (DataTrackIndexEntry e1, DataTrackIndexEntry e2) => e1.DirectoryEntry.Name.CompareTo(e2.DirectoryEntry.Name)
            );

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
                    return _entries;
            }
        }

        /// <summary>
        /// Get entries (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DataTrackIndexEntry> GetEntries(DataTrackEntriesOrder order = DataTrackEntriesOrder.DEFAULT)
        {
            foreach (DataTrackIndexEntry entry in GetEntriesList(order))
            {
                yield return entry;
            }
        }

        /// <summary>
        /// Get directory entries only (iterator)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<DataTrackIndexEntry> GetDirectories(DataTrackEntriesOrder order = DataTrackEntriesOrder.DEFAULT)
        {
            foreach (DataTrackIndexEntry entry in GetEntriesList(order))
            {
                if (entry.IsDirectory)
                {
                    yield return entry;
                }
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
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        /// Get specific entry based on its path
        /// </summary>
        /// <param name="fullPath">The full path of the entry (eg : /FOLDER/SUB/FILE.EXT)</param>
        /// <returns></returns>
        internal DataTrackIndexEntry GetEntry(string fullPath)
        {
            if (!_mappedEntries.ContainsKey(fullPath))
            {
                return null;
            }

            return _mappedEntries[fullPath];
        }

        /// <summary>
        /// Get parent of specific entry based on its path
        /// </summary>
        /// <param name="fullPath">The full path of the entry (eg : /FOLDER/SUB/FILE.EXT)</param>
        /// <returns></returns>
        internal DataTrackIndexEntry GetParent(string fullPath)
        {
            if (!_mappedEntries.ContainsKey(fullPath))
            {
                return null;
            }

            return _mappedEntries[fullPath].ParentEntry;
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
            {
                lIndex = fullPath.LastIndexOf('/', lIndex - 1);
            }

            if (lIndex == 0)
            {
                return _root;
            }
            else
            {
                fullPath = fullPath.Substring(0, lIndex);
                if (!_mappedEntries.ContainsKey(fullPath))
                {
                    return null;
                }
                    
                return _mappedEntries[fullPath];
            }
        }

        /// <summary>
        /// The index entry that represents the root of the files tree
        /// </summary>
        internal DataTrackIndexEntry Root => _root;

        /// <summary>
        /// Number of entries
        /// </summary>
        internal int EntriesCount
        {
            get => _entriesCount;
            set => _entriesCount = value;
        }

        /// <summary>
        /// Number of directory entries
        /// </summary>
        internal int DirectoryEntriesCount
        {
            get => _directoryEntriesCount;
            set => _directoryEntriesCount = value;
        }

        /// <summary>
        /// Number of file entries
        /// </summary>
        internal int FileEntriesCount
        {
            get => _fileEntriesCount;
            set => _fileEntriesCount = value;
        }
    }
}
