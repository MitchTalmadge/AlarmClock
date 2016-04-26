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

        private MusicPlayer _musicPlayer;

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

            _musicPlayer = new MusicPlayer();

            if (_wakeUpDetector.StartDetection())
            {
                Background = new SolidColorBrush(Colors.Black);

                //Begin playing alarm clock sound on loop
                _musicPlayer.PlayMp3Asset("AlarmClock.mp3", true);
            }
            else //Could not load Kinect Sensor. Just skip it.
            {
                _wakeUpDetector.Dispose();
                DoWakeupRoutine();
            }
        }

        private void DoWakeupRoutine()
        {
            TimeSpan totalTime = _musicPlayer.PlayMp3Asset("Intro.mp3", false); //Start playing intro music.
            Background = new SolidColorBrush(Colors.White);
            BackgroundImage.Visibility = Visibility.Visible;
            BackgroundImage.Source = new BitmapImage(AssetFinder.AssetToUri("images/bg1.jpg"));
        }

        private void WakeUpDetectorOnWakeUpConfirmedEvent()
        {
            _musicPlayer.StopMusic();
            _wakeUpDetector.Dispose();
            DoWakeupRoutine();
        }

        private void WakeUpDetectorOnWakeUpProgressEvent(short progress, short max)
        {
            _musicPlayer.SetVolume((float) (1.0 - ((float) progress/max)*1.0)); //Adjust volume for fade
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _wakeUpDetector?.Dispose();
        }
    }
}