namespace CRH.Framework.Disk.DataTrack
{
    public sealed class XaSubHeader
    {
        private byte _file;
        private byte _channel;
        private byte _subMode;
        private byte _dataType;

        private static XaSubHeader _basicSubHeader;
        private static XaSubHeader _endOfRecordSubHeader;
        private static XaSubHeader _endOfFileSubHeader;

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
            _file     = file;
            _channel  = channel;
            _subMode  = subMode;
            _dataType = dataType;
        }

        /// <summary>
        /// Get specific flag state from SubMode field
        /// </summary>
        /// <param name="mask">Le mask de l'attribut à lire</param>
        /// <returns></returns>
        private bool GetSubModeFlag(XaSubModeFlag mask)
        {
            return (_subMode & (byte)mask) > 0;
        }

        /// <summary>
        /// Set attribute state into Attributes field
        /// </summary>
        /// <param name="mask">Le mask du l'attribut à écrire</param>
        /// <param name="value">La valeur</param>
        private void SetSubModeFlag(XaSubModeFlag mask, bool value)
        {
            if (value)
            {
                _subMode |= (byte)mask;
            }
            else
            {
                _subMode &= (byte)(0xFF ^ (byte)mask);
            }
        }

        /// <summary>
        /// Basic subHeader (data)
        /// </summary>
        public static XaSubHeader Basic
        {
            get
            {
                if (_basicSubHeader == null)
                {
                    _basicSubHeader = new XaSubHeader();
                    _basicSubHeader.IsData = true;
                }

                return _basicSubHeader;
            }
        }

        /// <summary>
        /// Basic end of record subheader (data, end of record)
        /// </summary>
        public static XaSubHeader EndOfRecord
        {
            get
            {
                if (_endOfRecordSubHeader == null)
                {
                    _endOfRecordSubHeader = new XaSubHeader();
                    _endOfRecordSubHeader.IsData = true;
                    _endOfRecordSubHeader.IsEOR = true;
                }

                return _endOfRecordSubHeader;
            }
        }

        /// <summary>
        /// Basic end of file subheader (data, end of record, end of file)
        /// </summary>
        public static XaSubHeader EndOfFile
        {
            get
            {
                if (_endOfFileSubHeader == null)
                {
                    _endOfFileSubHeader = new XaSubHeader();
                    _endOfFileSubHeader.IsData = true;
                    _endOfFileSubHeader.IsEOF = true;
                    _endOfFileSubHeader.IsEOR = true;
                }

                return _endOfFileSubHeader;
            }
        }

        /// <summary>
        /// File
        /// </summary>
        public byte File
        {
            get => _file;
            internal set => _file = value;
        }

        /// <summary>
        /// Channel
        /// </summary>
        public byte Channel
        {
            get => _channel;
            internal set => _channel = value;
        }

        /// <summary>
        /// Sub-mode
        /// </summary>
        public byte SubMode
        {
            get => _subMode;
            internal set => _subMode = value;
        }

        /// <summary>
        /// Is End of record
        /// </summary>
        public bool IsEOR
        {
            get => GetSubModeFlag(XaSubModeFlag.EOR);
            set => SetSubModeFlag(XaSubModeFlag.EOR, value);
        }

        /// <summary>
        /// Contains audio
        /// </summary>
        public bool IsAudio
        {
            get => GetSubModeFlag(XaSubModeFlag.AUDIO);
            set => SetSubModeFlag(XaSubModeFlag.AUDIO, value);
        }

        /// <summary>
        /// Contains video
        /// </summary>
        public bool IsVideo
        {
            get => GetSubModeFlag(XaSubModeFlag.VIDEO);
            set => SetSubModeFlag(XaSubModeFlag.VIDEO, value);
        }

        /// <summary>
        /// Contains data
        /// </summary>
        public bool IsData
        {
            get => GetSubModeFlag(XaSubModeFlag.DATA);
            set => SetSubModeFlag(XaSubModeFlag.DATA, value);
        }

        /// <summary>
        /// Trigger on (OS dependant)
        /// </summary>
        public bool TriggerOn
        {
            get => GetSubModeFlag(XaSubModeFlag.TRIGGER_ON);
            set => SetSubModeFlag(XaSubModeFlag.TRIGGER_ON, value);
        }

        /// <summary>
        /// Is Form 2
        /// </summary>
        public bool IsForm2
        {
            get => GetSubModeFlag(XaSubModeFlag.FORM2);
            set => SetSubModeFlag(XaSubModeFlag.FORM2, value);
        }

        /// <summary>
        /// Is real time data
        /// </summary>
        public bool IsRealTime
        {
            get => GetSubModeFlag(XaSubModeFlag.REAL_TIME);
            set => SetSubModeFlag(XaSubModeFlag.REAL_TIME, value);
        }

        /// <summary>
        /// Is End of file
        /// </summary>
        public bool IsEOF
        {
            get => GetSubModeFlag(XaSubModeFlag.EOF);
            set => SetSubModeFlag(XaSubModeFlag.EOF, value);
        }

        /// <summary>
        /// Data type
        /// </summary>
        public byte DataType
        {
            get => _dataType;
            internal set => _dataType = value;
        }
    }
}
