using System;
using System.Collections.Generic;
using CRH.Framework.Common;

namespace CRH.Framework.Disk.DataTrack
{
    public class DataTrackIndexEntry
    {
        private DirectoryEntry            _directoryEntry;
        private DataTrackIndexEntry       _parentEntry;
        private List<DataTrackIndexEntry> _subEntries = null;

        private string _fullPath;
        private bool   _isRoot;
        private uint   _directoryAvailableSpace = 0;

        // Constructors

        /// <summary>
        /// TreeEntry
        /// </summary>
        /// <param name="directoryEntry">The directory entry that this tree entry refers to</param>
        internal DataTrackIndexEntry(DataTrackIndexEntry parent, DirectoryEntry directoryEntry)
        {
            _parentEntry = parent;
            _isRoot = (parent == null);
            _directoryEntry = directoryEntry;

            if (!_isRoot)
                _fullPath = parent.FullPath
                                + (parent.FullPath != "/" ? "/" : "")
                                + directoryEntry.Name;
            else
                _fullPath = "/";

            if (IsDirectory)
            {
                _subEntries = new List<DataTrackIndexEntry>();

                if (!_isRoot)
                    _directoryAvailableSpace = directoryEntry.ExtentSize
                                                - parent.DirectoryEntry.Length
                                                - parent.DirectoryEntry.ExtendedAttributeRecordlength
                                                - directoryEntry.Length;
                else
                    _directoryAvailableSpace = directoryEntry.ExtentSize
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
                throw new FrameworkException("Error while adding entry to directory : entry \"{0}\" is not a directory", _fullPath);

            if (subEntry.DirectoryEntry.Length > _directoryAvailableSpace)
                throw new FrameworkException("Error while adding entry to directory : directory \"{0}\" is too small", _fullPath);

            _subEntries.Add(subEntry);
            _directoryAvailableSpace -= subEntry.DirectoryEntry.Length;
        }

        // Accessors

        /// <summary>
        /// The directory entry that this tree entry refers to
        /// </summary>
        internal DirectoryEntry DirectoryEntry
        {
            get { return _directoryEntry; }
            set { _directoryEntry = value; }
        }

        /// <summary>
        /// Is this entry root
        /// </summary>
        internal bool IsRoot
        {
            get { return _isRoot; }
        }

        /// <summary>
        /// The parent tree entry (null if root)
        /// </summary>
        public DataTrackIndexEntry ParentEntry
        {
            get { return _parentEntry; }
            set { _parentEntry = value; }
        }

        /// <summary>
        /// The entries contained in this entry
        /// Value : null if not directory
        /// </summary>
        internal List<DataTrackIndexEntry> SubEntries
        {
            get { return _subEntries; }
        }

        /// <summary>
        /// The directory available space (used only in writing mode)
        /// </summary>
        internal uint DirectoryAvailableSpace
        {
            get { return _directoryAvailableSpace; }
            set { _directoryAvailableSpace = value; }
        }

        /// <summary>
        /// The number of files/directories contained in the directory
        /// Value : -1 if not directory
        /// </summary>
        public int SubEntriesCount
        {
            get { return _subEntries == null ? -1 : _subEntries.Count; }
        }

        /// <summary>
        /// is Directory
        /// </summary>
        public bool IsDirectory
        {
            get { return _directoryEntry.IsDirectory; }
        }

        public bool IsStream
        {
            get
            {
                return _directoryEntry.HasXa
                    ? _directoryEntry.XaEntry.IsForm2 || _directoryEntry.XaEntry.IsInterleaved
                    : false;
            }
        }

        /// <summary>
        /// Size of the entry
        /// </summary>
        public uint Size
        {
            get { return _directoryEntry.ExtentSize; }
        }

        /// <summary>
        /// Length of the DirectoryEntry
        /// </summary>
        internal uint Length
        {
            get { return _directoryEntry.Length; }
        }

        /// <summary>
        /// Lba of the entry
        /// </summary>
        public uint Lba
        {
            get { return _directoryEntry.ExtentLba; }
        }

        /// <summary>
        /// The full path of the file/directory that this entry refers to
        /// eg : /FOLDER/SUB/FILE.EXT
        /// </summary>
        public string FullPath
        {
            get { return _fullPath; }
            set { _fullPath = value; }
        }
    }
}
