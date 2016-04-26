using System;
using System.Windows;
using NAudio.Wave;

namespace AlarmClock.Utilities
{
    public class MusicPlayer : IDisposable
    {
        private WaveOut _waveOut;

        /// <summary>
        /// Plays an mp3 file, with optional looping capabilities.
        /// </summary>
        /// <param name="audioAssetName">The name of the mp3 asset.</param>
        /// <param name="loop">True if the mp3 should loop continuously.</param>
        /// <returns>Duration of the mp3 file.</returns>
        public TimeSpan PlayMp3Asset(string audioAssetName, bool loop)
        {
            _waveOut?.Dispose();
            _waveOut = new WaveOut();

            var reader = loop
                ? new Mp3Looper(Application.GetContentStream(AssetFinder.AssetToUri("audio/" + audioAssetName))?.Stream)
                : new Mp3FileReader(
                    Application.GetContentStream(AssetFinder.AssetToUri("audio/" + audioAssetName))?.Stream);
            var totalTime = reader.TotalTime;

            _waveOut.Volume = 1.0f;
            _waveOut.Init(reader);
            _waveOut.Play();

            return totalTime;
        }

        public void StopMusic()
        {
            _waveOut?.Stop();
        }

        public void SetVolume(float volume)
        {
            if (volume < 0) volume = 0;
            if (volume > 1.0) volume = 1.0f;

            if (_waveOut != null)
                _waveOut.Volume = volume;
        }

        public void Dispose()
        {
            StopMusic();
            _waveOut?.Dispose();
        }
    }
}