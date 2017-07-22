using System;
using System.IO;
using System.Net;
using NAudio.Wave;

namespace AlarmClock.Utilities
{
    public class MusicPlayer : IDisposable
    {
        private WaveOut _waveOut;
        private AudioFileReader _audioFileReader;

        /// <summary>
        /// Initializes an audio file, with optional looping capabilities.
        /// </summary>
        /// <param name="audioAssetName">The name of the audio asset.</param>
        /// <param name="loop">True if the audio should loop continuously.</param>
        /// <returns>Duration of the audio file.</returns>
        public TimeSpan InitAudioAsset(string audioAssetName, bool loop)
        {
            Dispose();
            _waveOut = new WaveOut();

            _audioFileReader = new AudioFileLooper("assets/audio/" + audioAssetName, loop);

            var totalTime = _audioFileReader.TotalTime;

            _waveOut.Init(_audioFileReader);

            return totalTime;
        }

        public void PlayMusic()
        {
            StopMusic();
            _audioFileReader.Position = 0; //Restart

            _waveOut?.Play();
        }

        public static void PlayMp3FromUrl(string url)
        {
            using (Stream ms = new MemoryStream())
            {
                using (var stream = WebRequest.Create(url)
                    .GetResponse().GetResponseStream())
                {
                    var buffer = new byte[32768];
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                }

                ms.Position = 0;
                using (WaveStream blockAlignedStream =
                    new BlockAlignReductionStream(
                        WaveFormatConversionStream.CreatePcmStream(
                            new Mp3FileReader(ms))))
                {
                    using (var waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                    {
                        waveOut.Init(blockAlignedStream);
                        waveOut.Play();
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                }
            }
        }

        public void StopMusic()
        {
            _waveOut?.Stop();
        }

        public void SetVolume(float volume)
        {
            if (volume < 0) volume = 0;
            if (volume > 1.0) volume = 1.0f;

            if (_audioFileReader != null)
                _audioFileReader.Volume = volume;
        }

        public void Dispose()
        {
            StopMusic();
            _waveOut?.Dispose();
            _audioFileReader?.Dispose();
        }
    }

    public class AudioFileLooper : AudioFileReader
    {
        private readonly bool _loop;

        public AudioFileLooper(string fileName, bool loop) : base(fileName)
        {
            _loop = loop;
        }

        public override int Read(byte[] buffer, int offset, int numBytes)
        {
            /*var totalBytesRead = 0;

            while (totalBytesRead < numBytes)
            {
                var bytesRead = base.Read(buffer, offset + totalBytesRead, numBytes - totalBytesRead);
                if (bytesRead == 0 && _loop) //End of File
                {
                    Position = 0; //Back to beginning
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;*/
            var output = base.Read(buffer, offset, numBytes);
            if (output == 0 && _loop)
            {
                Position = 0;
                output = base.Read(buffer, offset, numBytes);
            }
            return output;
        }
    }
}