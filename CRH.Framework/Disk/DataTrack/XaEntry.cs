using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        private ushort m_groupId;
        private ushort m_userId;
        private ushort m_attributes;
        private string m_signature;
        private byte m_fileNumber;
        private byte[] m_unused;

    // Constructors

        internal XaEntry()
        {
            m_groupId    = 0;
            m_userId     = 0;
            m_attributes = (ushort)XaEntryFlag.PERM_USER_R
                            | (ushort)XaEntryFlag.PERM_USER_X
                            | (ushort)XaEntryFlag.PERM_GROUP_R
                            | (ushort)XaEntryFlag.PERM_GROUP_X
                            | (ushort)XaEntryFlag.PERM_OTHERS_R
                            | (ushort)XaEntryFlag.PERM_OTHERS_X;
            m_signature  = XA_SIGNATURE;
            m_fileNumber = 1;
            m_unused     = new byte[5] { 0, 0, 0, 0, 0 };
        }

    // Methods

        /// <summary>
        /// Get specific attribute state from Attributes field
        /// </summary>
        /// <param name="mask">Attribute's bitmask to read</param>
        private bool GetAttribute(XaEntryFlag mask)
        {
            return (m_attributes & (ushort)mask) > 0;
        }

        /// <summary>
        /// Set attribute state into Attributes field
        /// </summary>
        /// <param name="mask">Attribute's bitmask to write</param>
        /// <param name="value">Value</param>
        private void SetAttribute(XaEntryFlag mask, bool value)
        {
            if (value)
                m_attributes |= (ushort)mask;
            else
                m_attributes &= (ushort)(0xFFFF ^ (ushort)mask);
        }

    // Accessors

        /// <summary>
        /// Owning group
        /// </summary>
        internal ushort GroupId
        {
            get { return m_groupId; }
            set { m_groupId = value; }
        }

        /// <summary>
        /// Owning user
        /// </summary>
        internal ushort UserId
        {
            get { return m_userId; }
            set { m_userId = value; }
        }

        /// <summary>
        /// Attributes of the file/folder
        /// </summary>
        internal ushort Attributes
        {
            get { return m_attributes; }
            set { m_attributes = value; }
        }

        /// <summary>
        /// User's read permission (ur)
        /// </summary>
        internal bool UserReadPermission
        {
            get { return GetAttribute(XaEntryFlag.PERM_USER_R); }
            set { SetAttribute(XaEntryFlag.PERM_USER_R, value); }
        }

        /// <summary>
        /// User's execution permssion (ux)
        /// </summary>
        internal bool UserExecPermission
        {
            get { return GetAttribute(XaEntryFlag.PERM_USER_X); }
            set { SetAttribute(XaEntryFlag.PERM_USER_X, value); }
        }

        /// <summary>
        /// Group's read permission (gr)
        /// </summary>
        internal bool GroupReadPermission
        {
            get { return GetAttribute(XaEntryFlag.PERM_GROUP_R); }
            set { SetAttribute(XaEntryFlag.PERM_GROUP_R, value); }
        }

        /// <summary>
        /// Group's execution permission (gx)
        /// </summary>
        internal bool GroupExecPermission
        {
            get { return GetAttribute(XaEntryFlag.PERM_GROUP_X); }
            set { SetAttribute(XaEntryFlag.PERM_GROUP_X, value); }
        }

        /// <summary>
        /// Everyone else's read permission (or)
        /// </summary>
        internal bool OthersReadPermission
        {
            get { return GetAttribute(XaEntryFlag.PERM_OTHERS_R); }
            set { SetAttribute(XaEntryFlag.PERM_OTHERS_R, value); }
        }

        /// <summary>
        /// Everyone else's execution permission (ox)
        /// </summary>
        internal bool OthersExecPermission
        {
            get { return GetAttribute(XaEntryFlag.PERM_OTHERS_X); }
            set { SetAttribute(XaEntryFlag.PERM_OTHERS_X, value); }
        }

        /// <summary>
        /// Is MODE2_FORM1 sector
        /// When set, opposed flag 'IsMode2Form2' is unset
        /// </summary>
        internal bool IsForm1
        {
            get { return GetAttribute(XaEntryFlag.FORM1); }
            set
            { 
                SetAttribute(XaEntryFlag.FORM1, value);
                if(value)
                    SetAttribute(XaEntryFlag.FORM2, false);
            }
        }

        /// <summary>
        /// Is MODE2_FORM2 sector
        /// When set, opposed flag 'IsMode2Form1' is unset
        /// </summary>
        internal bool IsForm2
        {
            get { return GetAttribute(XaEntryFlag.FORM2); }
            set
            { 
                SetAttribute(XaEntryFlag.FORM2, value);
                if(value)
                    SetAttribute(XaEntryFlag.FORM1, false);
            }
        }

        /// <summary>
        /// Is CDDA (contains audio)
        /// </summary>
        internal bool IsCdda
        {
            get { return GetAttribute(XaEntryFlag.CDDA); }
            set { SetAttribute(XaEntryFlag.CDDA, value); }
        }

        /// <summary>
        /// Is interleaved
        /// </summary>
        internal bool IsInterleaved
        {
            get { return GetAttribute(XaEntryFlag.INTERLEAVED); }
            set { SetAttribute(XaEntryFlag.INTERLEAVED, value); }
        }

        /// <summary>
        /// Is a directory
        /// </summary>
        internal bool IsDirectory
        {
            get { return GetAttribute(XaEntryFlag.DIRECTORY); }
            set { SetAttribute(XaEntryFlag.DIRECTORY, value); }
        }

        /// <summary>
        /// Entry signature
        /// Size : 2 bytes
        /// Value : always "XA"
        /// </summary>
        internal string Signature
        {
            get { return m_signature; }
            set { m_signature = value; }
        }

        /// <summary>
        /// File number
        /// </summary>
        internal byte FileNumber
        {
            get { return m_fileNumber; }
            set { m_fileNumber = value; }
        }

        /// <summary>
        /// unused
        /// Size : 5 bytes
        /// </summary>
        internal byte[] Unused
        {
            get { return m_unused; }
            set { m_unused = value; }
        }
    }
}