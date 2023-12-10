namespace CRH.Framework.Disk.DataTrack
{
    internal enum XaEntryFlag : ushort
    {
        PERM_USER_R   = 1,
        PERM_USER_X   = 1 << 2,
        PERM_GROUP_R  = 1 << 4,
        PERM_GROUP_X  = 1 << 6,
        PERM_OTHERS_R = 1 << 8,
        PERM_OTHERS_X = 1 << 10,
        FORM1         = 1 << 11,
        FORM2         = 1 << 12,
        INTERLEAVED   = 1 << 13,
        CDDA          = 1 << 14,
        DIRECTORY     = 1 << 15
    }

    /// <summary>
    /// XA entry
    /// Size : 14 bytes
    /// Note : stored in BE only
    /// </summary>
    public sealed class XaEntry
    {
        public const int SIZE = 14;
        public const string XA_SIGNATURE = "XA";

        private ushort _groupId;
        private ushort _userId;
        private ushort _attributes;
        private string _signature;
        private byte   _fileNumber;
        private byte[] _unused;

    // Constructors

        internal XaEntry()
        {
            _groupId    = 0;
            _userId     = 0;
            _attributes = (ushort)XaEntryFlag.PERM_USER_R
                            | (ushort)XaEntryFlag.PERM_USER_X
                            | (ushort)XaEntryFlag.PERM_GROUP_R
                            | (ushort)XaEntryFlag.PERM_GROUP_X
                            | (ushort)XaEntryFlag.PERM_OTHERS_R
                            | (ushort)XaEntryFlag.PERM_OTHERS_X;
            _signature  = XA_SIGNATURE;
            _fileNumber = 1;
            _unused     = [0, 0, 0, 0, 0];
        }

    // Methods

        /// <summary>
        /// Get specific attribute state from Attributes field
        /// </summary>
        /// <param name="mask">Attribute's bitmask to read</param>
        private bool GetAttribute(XaEntryFlag mask)
        {
            return (_attributes & (ushort)mask) > 0;
        }

        /// <summary>
        /// Set attribute state into Attributes field
        /// </summary>
        /// <param name="mask">Attribute's bitmask to write</param>
        /// <param name="value">Value</param>
        private void SetAttribute(XaEntryFlag mask, bool value)
        {
            if (value)
            {
                _attributes |= (ushort)mask;
            }
            else
            {
                _attributes &= (ushort)(0xFFFF ^ (ushort)mask);
            }
        }

    // Accessors

        /// <summary>
        /// Owning group
        /// </summary>
        internal ushort GroupId
        {
            get => _groupId;
            set => _groupId = value;
        }

        /// <summary>
        /// Owning user
        /// </summary>
        internal ushort UserId
        {
            get => _userId;
            set => _userId = value;
        }

        /// <summary>
        /// Attributes of the file/folder
        /// </summary>
        internal ushort Attributes
        {
            get => _attributes;
            set => _attributes = value;
        }

        /// <summary>
        /// User's read permission (ur)
        /// </summary>
        internal bool UserReadPermission
        {
            get => GetAttribute(XaEntryFlag.PERM_USER_R);
            set => SetAttribute(XaEntryFlag.PERM_USER_R, value);
        }

        /// <summary>
        /// User's execution permssion (ux)
        /// </summary>
        internal bool UserExecPermission
        {
            get => GetAttribute(XaEntryFlag.PERM_USER_X);
            set => SetAttribute(XaEntryFlag.PERM_USER_X, value);
        }

        /// <summary>
        /// Group's read permission (gr)
        /// </summary>
        internal bool GroupReadPermission
        {
            get => GetAttribute(XaEntryFlag.PERM_GROUP_R);
            set => SetAttribute(XaEntryFlag.PERM_GROUP_R, value);
        }

        /// <summary>
        /// Group's execution permission (gx)
        /// </summary>
        internal bool GroupExecPermission
        {
            get => GetAttribute(XaEntryFlag.PERM_GROUP_X);
            set => SetAttribute(XaEntryFlag.PERM_GROUP_X, value);
        }

        /// <summary>
        /// Everyone else's read permission (or)
        /// </summary>
        internal bool OthersReadPermission
        {
            get => GetAttribute(XaEntryFlag.PERM_OTHERS_R);
            set => SetAttribute(XaEntryFlag.PERM_OTHERS_R, value);
        }

        /// <summary>
        /// Everyone else's execution permission (ox)
        /// </summary>
        internal bool OthersExecPermission
        {
            get => GetAttribute(XaEntryFlag.PERM_OTHERS_X);
            set => SetAttribute(XaEntryFlag.PERM_OTHERS_X, value);
        }

        /// <summary>
        /// Is MODE2_FORM1 sector
        /// When set, opposed flag 'IsMode2Form2' is unset
        /// </summary>
        internal bool IsForm1
        {
            get => GetAttribute(XaEntryFlag.FORM1);
            set
            { 
                SetAttribute(XaEntryFlag.FORM1, value);
                if (value)
                {
                    SetAttribute(XaEntryFlag.FORM2, false);
                }
            }
        }

        /// <summary>
        /// Is MODE2_FORM2 sector
        /// When set, opposed flag 'IsMode2Form1' is unset
        /// </summary>
        internal bool IsForm2
        {
            get => GetAttribute(XaEntryFlag.FORM2);
            set
            { 
                SetAttribute(XaEntryFlag.FORM2, value);
                if (value)
                {
                    SetAttribute(XaEntryFlag.FORM1, false);
                }
            }
        }

        /// <summary>
        /// Is CDDA (contains audio)
        /// </summary>
        internal bool IsCdda
        {
            get => GetAttribute(XaEntryFlag.CDDA);
            set => SetAttribute(XaEntryFlag.CDDA, value);
        }

        /// <summary>
        /// Is interleaved
        /// </summary>
        internal bool IsInterleaved
        {
            get => GetAttribute(XaEntryFlag.INTERLEAVED);
            set => SetAttribute(XaEntryFlag.INTERLEAVED, value);
        }

        /// <summary>
        /// Is a directory
        /// </summary>
        internal bool IsDirectory
        {
            get => GetAttribute(XaEntryFlag.DIRECTORY);
            set => SetAttribute(XaEntryFlag.DIRECTORY, value);
        }

        /// <summary>
        /// Entry signature
        /// Size : 2 bytes
        /// Value : always "XA"
        /// </summary>
        internal string Signature
        {
            get => _signature;
            set => _signature = value;
        }

        /// <summary>
        /// File number
        /// </summary>
        internal byte FileNumber
        {
            get => _fileNumber;
            set => _fileNumber = value;
        }

        /// <summary>
        /// unused
        /// Size : 5 bytes
        /// </summary>
        internal byte[] Unused
        {
            get => _unused;
            set => _unused = value;
        }
    }
}