using System.IO;

namespace CRH.Framework.IO.Hash
{
    public class Checksum
    {
        /// <summary>
        /// Compute a simple checksum of the given stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="start">Start position (default = begining of the stream)</param>
        /// <param name="length">The length to compute (default = to end of stream)</param>
        public static uint ComputeToUint(Stream stream, long start = 0, long length = -1)
        {
            long savePosition = stream.Position;
            long endPosition;

            stream.Position = start;
            endPosition = (length == -1)
                            ? stream.Length - stream.Position
                            : stream.Position + length;

            uint sum = 0;
            int bufferSize = 512;
            byte[] buffer = new byte[bufferSize];
            int dataRead;
            while (stream.Position < endPosition)
            {
                if (endPosition - stream.Position < bufferSize)
                {
                    bufferSize = (int)(endPosition - stream.Position);
                }

                dataRead = stream.Read(buffer, 0, bufferSize);
                for (int i = 0; i < dataRead; i++)
                {
                    sum += buffer[i];
                }
            }

            stream.Position = savePosition;

            return sum;
        }

        /// <summary>
        /// Compute a simple checksum of the given stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="start">Start position (default = begining of the stream)</param>
        /// <param name="length">The length to compute (default = to end of stream)</param>
        public static ushort ComputeToUShort(Stream stream, long start = 0, long length = -1)
        {
            long savePosition = stream.Position;
            long endPosition;

            stream.Position = start;
            endPosition = (length == -1)
                            ? stream.Length - stream.Position
                            : stream.Position + length;

            ushort sum = 0;
            int bufferSize = 512;
            byte[] buffer = new byte[bufferSize];
            int dataRead;
            while (stream.Position < endPosition)
            {
                if (endPosition - stream.Position < bufferSize)
                {
                    bufferSize = (int)(endPosition - stream.Position);
                }

                dataRead = stream.Read(buffer, 0, bufferSize);
                for (int i = 0; i < dataRead; i++)
                {
                    sum += buffer[i];
                }
            }

            stream.Position = savePosition;

            return sum;
        }
    }
}