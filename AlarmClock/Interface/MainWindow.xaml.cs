using System;
using System.Windows;
using System.Windows.Media;
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
        /// The required decrease in distance to trigger a success for a pixel (In millimeters).
        /// </summary>
        private const int RequiredDistanceDecrease = 700;

        /// <summary>
        /// The number of pixels which match REQUIRED_DISTANCE_DECREASE needed in order to trigger a success.
        /// </summary>
        private const int RequiredPixelDistanceMatches = 500;

        /// <summary>
        /// The change in distance required to trigger a fluctuation for a pixel (In millimeters).
        /// </summary>
        private const int RequiredFluctuationChange = 1000;

        /// <summary>
        /// The maximum number of fluctuations allowed for a pixel before disregarding it entirely.
        /// </summary>
        private const int MaxFluctuationTriggers = 30;

        /// <summary>
        /// The number of successes required before an overall success is triggered. (1 success may be achieved per second.)
        /// </summary>
        private const int SuccessCountRequired = 150;

        /// <summary>
        /// The number of successes
        /// </summary>
        private short _successCount;

        //------------------------ Kinect ------------------------//

        /// <summary>
        /// The Kinect Sensor that is currently being used.
        /// </summary>
        private KinectSensor _kinectSensor;

        private DepthImagePixel[] _initialPixels;
        private bool _initialSet;

        private DepthImagePixel[] _lastPixels;
        private short[] _fluctuationCount;
        private DepthImagePixel[] _currentPixels;

        //------------------------ Audio ------------------------//

        private WaveOut _waveOut;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (LoadKinectSensor())
            {
                Background = new SolidColorBrush(Colors.Black);

                //Begin playing alarm clock sound on loop
                PlayAudio("assets/audio/AlarmClock.mp3", true);
            }
            else //Could not load Kinect Sensor. Just skip it.
            {
                DoWakeupRoutine();
            }
        }

        /// <summary>
        /// Loads the first connected kinect sensor if one is connected.
        /// </summary>
        /// <returns>true if connection was established and the Kinect has started; false if otherwise.</returns>
        private bool LoadKinectSensor()
        {
            foreach (var sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status != KinectStatus.Connected) continue;

                _kinectSensor = sensor;
                break;
            }

            if (_kinectSensor == null) return false;

            _kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            _initialPixels = new DepthImagePixel[_kinectSensor.DepthStream.FramePixelDataLength];
            _currentPixels = new DepthImagePixel[_initialPixels.Length];
            _fluctuationCount = new short[_initialPixels.Length];

            _kinectSensor.DepthFrameReady += SensorDepthFrameReady;

            try
            {
                _kinectSensor.Start();
                return true;
            }
            catch (System.IO.IOException)
            {
                _kinectSensor = null;
            }
            return false;
        }

        private void ShutdownKinectSensor()
        {
            _kinectSensor?.Stop();
            _kinectSensor?.Dispose();
            _kinectSensor = null;
        }

        /// <summary>
        /// Plays an mp3 file, with optional looping capabilities.
        /// </summary>
        /// <param name="file">The path to the mp3 file.</param>
        /// <param name="loop">True if the mp3 should loop continuously.</param>
        /// <returns>Duration of the mp3 file.</returns>
        private TimeSpan PlayAudio(string file, bool loop)
        {
            _waveOut?.Dispose();
            _waveOut = new WaveOut();

            var reader = loop ? new Mp3Looper(file) : new Mp3FileReader(file);
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
            StopAudio(); //Stop playing alarm
            ShutdownKinectSensor(); //Shutdown Kinect sensor; we're done with it.

            TimeSpan totalTime = PlayAudio("assets/audio/Intro.mp3", false); //Start playing intro music.
            Background = new SolidColorBrush(Colors.White);
        }

        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (var depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null) return;

                if (!_initialSet)
                {
                    depthFrame.CopyDepthImagePixelDataTo(_initialPixels);
                    _initialSet = true;
                }

                depthFrame.CopyDepthImagePixelDataTo(_currentPixels);
                if (_lastPixels == null)
                    _lastPixels = (DepthImagePixel[])_currentPixels.Clone();

                int pixelDistanceMatchCount = 0;
                for (int i = 0; i < _currentPixels.Length; i++)
                {
                    if (_fluctuationCount[i] >= MaxFluctuationTriggers)
                        continue; //This pixel fluctuates too much. Skip it.

                    if (Math.Abs(_lastPixels[i].Depth - _currentPixels[i].Depth) >= RequiredFluctuationChange ||
                        !_currentPixels[i].IsKnownDepth)
                    {
                        _fluctuationCount[i]++; //This pixel fluctuated too much. Add one to the counter.
                        continue; //Skip it.
                    }

                    if (_initialPixels[i].Depth - _currentPixels[i].Depth >= RequiredDistanceDecrease)
                        pixelDistanceMatchCount++;
                }

                if (pixelDistanceMatchCount >= RequiredPixelDistanceMatches)
                {
                    _successCount++;
                    _waveOut.Volume = (float)(1.0 - ((float)_successCount / SuccessCountRequired) * 1.0); //Adjust volume for fade

                    if (_successCount >= SuccessCountRequired) //Enough successes;  Start main wakeup routine and shutdown Kinect.
                    {
                        DoWakeupRoutine();
                        return;
                    }
                }
                else //Lost sight of user; must have laid back down. Reset volume and success counter.
                {
                    _successCount = 0;
                    _waveOut.Volume = 1.0f;
                }

                _lastPixels = (DepthImagePixel[])_currentPixels.Clone();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ShutdownKinectSensor();
        }
    }
}