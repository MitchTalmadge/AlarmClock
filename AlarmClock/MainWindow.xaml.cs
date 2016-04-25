using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using NAudio.Wave;

namespace AlarmClock
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
        private Boolean _initialSet;

        private DepthImagePixel[] _lastPixels;
        private short[] _fluctuationCount;
        private DepthImagePixel[] _currentPixels;

        //------------------------ Audio ------------------------//

        private Mp3FileReader _mp3FileReader;
        private WaveOut _waveOut;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (LoadKinectSensor())
            {
                Background = new SolidColorBrush(Colors.Black); //Black

                _mp3FileReader = new Mp3Looper("audio/AlarmClock.mp3");

                _waveOut = new WaveOut();
                _waveOut.Init(_mp3FileReader);
                _waveOut.Play();
            }
            else
            {
                Background = new SolidColorBrush(Colors.Red); //Red (Error)
            }

        }

        /// <summary>
        /// Loads the first connected kinect sensor if one is connected.
        /// </summary>
        /// <returns>true if connection was established and the Kinect has started; false if otherwise.</returns>
        private bool LoadKinectSensor()
        {
            foreach (KinectSensor sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    _kinectSensor = sensor;
                    break;
                }
            }

            if (_kinectSensor != null)
            {
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
            }
            return false;
        }

        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
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

                        if (Math.Abs(_lastPixels[i].Depth - _currentPixels[i].Depth) >= RequiredFluctuationChange || !_currentPixels[i].IsKnownDepth)
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
                        _waveOut.Volume = (float)(1.0 - ((float)_successCount / SuccessCountRequired) * 1.0);
                        if (_successCount >= SuccessCountRequired)
                        {
                            _waveOut?.Stop();
                            Application.Current.Shutdown();
                        }
                    }
                    else
                    {
                        _successCount = 0;
                        _waveOut.Volume = 1.0f;
                    }

                    _lastPixels = (DepthImagePixel[])_currentPixels.Clone();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _kinectSensor?.Stop();
        }
    }
}
