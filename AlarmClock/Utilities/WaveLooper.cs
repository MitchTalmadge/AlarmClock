using System.IO;
using NAudio.Wave;

namespace AlarmClock.Utilities
{
    public class WaveLooper : WaveFileReader
    {
        public WaveLooper(string mp3FileName) : base(mp3FileName)
        {
        }

        public WaveLooper(Stream inputStream) : base(inputStream)
        {
        }

        public override int Read(byte[] buffer, int offset, int numBytes)
        {
            var totalBytesRead = 0;

            while (totalBytesRead < numBytes)
            {
                var bytesRead = base.Read(buffer, offset + totalBytesRead, numBytes - totalBytesRead);
                if (bytesRead == 0) //End of File
                {
                    base.Position = 0; //Back to beginning
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }
    }
}