using System;
using System.IO;
using System.Windows;
using NAudio.Wave;

namespace AlarmClock.Utilities
{
    public class MusicPlayer : IDisposable
    {
        private WaveOut _waveOut;
        private Mp3FileReader _mp3FileReader;
        private WaveFileReader _waveFileReader;

        /// <summary>
        /// Initializes an mp3 file, with optional looping capabilities.
        /// </summary>
        /// <param name="audioAssetName">The name of the mp3 asset.</param>
        /// <param name="loop">True if the mp3 should loop continuously.</param>
        /// <returns>Duration of the mp3 file.</returns>
        public TimeSpan InitMp3Asset(string audioAssetName, bool loop)
        {
            Dispose();
            _waveOut = new WaveOut();

            _mp3FileReader = loop
                ? new Mp3Looper(Application.GetContentStream(AssetFinder.PathToUri("assets/audio/" + audioAssetName))?.Stream)
                : new Mp3FileReader(
                    Application.GetContentStream(AssetFinder.PathToUri("assets/audio/" + audioAssetName))?.Stream);
            var totalTime = _mp3FileReader.TotalTime;

            _waveOut.Volume = 1.0f;
            _waveOut.Init(_mp3FileReader);

            return totalTime;
        }

        /// <summary>
        /// Initializes a wav file, with optional looping capabilities.
        /// </summary>
        /// <param name="audioAssetName">The name of the wav asset.</param>
        /// <param name="loop">True if the wav should loop continuously.</param>
        /// <returns>Duration of the wav file.</returns>
        public TimeSpan InitWavAsset(string audioAssetName, bool loop)
        {
            Dispose();
            _waveOut = new WaveOut();

            _waveFileReader = loop
                 ? new WaveLooper(Application.GetContentStream(AssetFinder.PathToUri("assets/audio/" + audioAssetName))?.Stream)
                 : new WaveFileReader(
                     Application.GetContentStream(AssetFinder.PathToUri("assets/audio/" + audioAssetName))?.Stream);

            var totalTime = _waveFileReader.TotalTime;

            _waveOut.Volume = 1.0f;
            _waveOut.Init(_waveFileReader);

            return totalTime;
        }

        public void PlayMusic()
        {
            StopMusic();
            _waveFileReader?.Seek(0, SeekOrigin.Begin);
            _mp3FileReader?.Seek(0, SeekOrigin.Begin);
            _waveOut?.Play();
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
            _mp3FileReader?.Dispose();
            _waveFileReader?.Dispose();
        }
    }
}