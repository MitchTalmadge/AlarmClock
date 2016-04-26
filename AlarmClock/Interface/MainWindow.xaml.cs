using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AlarmClock.Utilities;
using Microsoft.Kinect;
using NAudio.Utils;
using NAudio.Wave;

namespace AlarmClock.Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        /// <summary>
        /// The wake up detector, which determines when the user has woken up.
        /// </summary>
        private WakeUpDetector _wakeUpDetector;

        /// <summary>
        /// The audio output handler.
        /// </summary>
        private WaveOut _waveOut;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Background = new SolidColorBrush(Colors.Black);
            BackgroundImage.Visibility = Visibility.Hidden;

            _wakeUpDetector = new WakeUpDetector();
            _wakeUpDetector.WakeUpConfirmedEvent += WakeUpDetectorOnWakeUpConfirmedEvent;
            _wakeUpDetector.WakeUpProgressEvent += WakeUpDetectorOnWakeUpProgressEvent;

            if (_wakeUpDetector.StartDetection())
            {
                Background = new SolidColorBrush(Colors.Black);

                //Begin playing alarm clock sound on loop
                PlayAudio(AssetFinder.AssetToUri("audio/AlarmClock.mp3"), true);
            }
            else //Could not load Kinect Sensor. Just skip it.
            {
                _wakeUpDetector.Dispose();
                DoWakeupRoutine();
            }
        }

        private void WakeUpDetectorOnWakeUpConfirmedEvent()
        {
            StopAudio();
            _wakeUpDetector.Dispose();
            DoWakeupRoutine();
        }

        private void WakeUpDetectorOnWakeUpProgressEvent(short progress, short max)
        {
            if (_waveOut != null)
                _waveOut.Volume = (float)(1.0 - ((float)progress / max) * 1.0); //Adjust volume for fade
        }

        /// <summary>
        /// Plays an mp3 file, with optional looping capabilities.
        /// </summary>
        /// <param name="fileUri">The path to the mp3 file.</param>
        /// <param name="loop">True if the mp3 should loop continuously.</param>
        /// <returns>Duration of the mp3 file.</returns>
        private TimeSpan PlayAudio(Uri fileUri, bool loop)
        {
            _waveOut?.Dispose();
            _waveOut = new WaveOut();

            var reader = loop ? new Mp3Looper(Application.GetContentStream(fileUri)?.Stream) : new Mp3FileReader(Application.GetContentStream(fileUri)?.Stream);
            var totalTime = reader.TotalTime;

            _waveOut.Volume = 1.0f;
            _waveOut.Init(reader);
            _waveOut.Play();

            return totalTime;
        }

        private void StopAudio()
        {
            _waveOut?.Stop();
        }

        private void DoWakeupRoutine()
        {
            TimeSpan totalTime = PlayAudio(AssetFinder.AssetToUri("audio/Intro.mp3"), false); //Start playing intro music.
            Background = new SolidColorBrush(Colors.White);
            BackgroundImage.Visibility = Visibility.Visible;
            BackgroundImage.Source = new BitmapImage(AssetFinder.AssetToUri("images/bg1.jpg"));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _wakeUpDetector?.Dispose();
        }
    }
}