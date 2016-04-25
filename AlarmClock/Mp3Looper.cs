using NAudio.Wave;

namespace AlarmClock
{
    public class Mp3Looper : Mp3FileReader
    {
        public Mp3Looper(string mp3FileName) : base(mp3FileName)
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