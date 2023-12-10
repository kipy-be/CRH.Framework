using CRH.Framework.IO;
using System;
using System.Text;

namespace CRH.Framework.Disk.DataTrack
{
    /// <summary>
    /// Volume descriptor type
    /// </summary>
    public enum VolumeDescriptorType : byte
    {
        BOOT = 0x00,
        PRIMARY = 0x01,
        SUPPLEMENTARY = 0x02,
        PARTITION = 0x03,
        SET_TERMINATOR = 0xFF
    }

    /// <summary>
    /// VolumeDescriptor (Base)
    /// </summary>
    public abstract class VolumeDescriptor
    {
        public const string VOLUME_ID = "CD001";
        public const string VOLUME_XA = "CD-XA001";

        protected VolumeDescriptorType _type;
        protected string _id;
        protected byte _version;

        internal VolumeDescriptor(VolumeDescriptorType type, byte version)
        {
            _type = type;
            _version = version;
            _id = VOLUME_ID;
        }

        /// <summary>
        /// Convert the specific datetime format of descriptor to DateTime
        /// </summary>
        /// <param name="value">The buffer to read</param>
        internal static DateTime ToDateTime(byte[] buffer)
        {
            if (buffer.Length == 17)
            {
                string value = Encoding.ASCII.GetString(buffer, 0, 16);
                byte timeZone = buffer[16];
                if (value.Equals("0000000000000000"))
                {
                    return DateTime.MinValue;
                }
                else
                {
                    var date = new DateTime
                    (
                        int.Parse(value.Substring(0, 4)),       // Year (1 to 9999)
                        int.Parse(value.Substring(4, 2)),       // Month (1 to 12)
                        int.Parse(value.Substring(6, 2)),       // Day (1 to 31)
                        int.Parse(value.Substring(8, 2)),       // Hours (0 to 23)
                        int.Parse(value.Substring(10, 2)),      // Minutes (0 to 59)
                        int.Parse(value.Substring(12, 2)),      // Seconds (0 to 59)
                        int.Parse(value.Substring(14, 2)) * 10  // Hundredth of seconds (0 to 99)
                    );

                    // There's also a timezone, but realy... who cares ?
                    // Just for info, format is :
                    // int8 with a value range of 0 to 100 (0 = -48 to 100 = 52, the value is then multiplied by 15 to obtain the timezone in minutes)
                    return date;
                }
            }

            return DateTime.MinValue;
        }

        private static string DatePartToString(int value, int size)
        {
            string strValue = value.ToString();

            while (strValue.Length < size)
            {
                strValue = "0" + strValue;
            }

            return strValue;
        }

        /// <summary>
        /// Convert the DateTime to specific datetime format of descriptor
        /// </summary>
        /// <param name="date">The date to convert</param>
        internal static byte[] FromDateTime(DateTime date)
        {
            string value = "";
            byte[] buffer = new byte[17];

            value += DatePartToString(date.Year, 4);
            value += DatePartToString(date.Month, 2);
            value += DatePartToString(date.Day, 2);
            value += DatePartToString(date.Hour, 2);
            value += DatePartToString(date.Minute, 2);
            value += DatePartToString(date.Second, 2);
            value += DatePartToString(date.Millisecond / 10, 2);

            CBuffer.Copy(Encoding.ASCII.GetBytes(value), buffer);

            return buffer;
        }

        /// <summary>
        /// Type of Volume Descriptor
        /// </summary>
        public VolumeDescriptorType Type
        {
            get => _type;
            set => _type = value;
        }

        /// <summary>
        /// Id
        /// Size : 5 bytes
        /// Value : always "CD001"
        /// </summary>
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        /// Version
        /// Value : always 0x01
        /// </summary>
        public byte Version
        {
            get => _version;
            set => _version = value;
        }
    }
}