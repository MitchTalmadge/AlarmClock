using System;
using System.Collections.Generic;
using Microsoft.Kinect;

namespace AlarmClock.Utilities
{
    public class WakeUpDetector : IDisposable
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
        /// The number of successes required before an overall success is triggered. (30 successes may be achieved per second.)
        /// </summary>
        private const int SuccessCountRequired = 300; //300 / 30 = 10 seconds

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

        public delegate void WakeUpConfirmed();

        public delegate void WakeUpProgress(short progress, short max);

        /// <summary>
        /// Starts Wake Up Detection.
        /// </summary>
        /// <returns>True if detection started successfully, False if it could not be started (Could not connect to sensor)</returns>
        public bool StartDetection()
        {
            return LoadKinectSensor();
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

        /// <summary>
        /// Shuts down the Kinect Sensor and releases all streams.
        /// </summary>
        private void ShutdownKinectSensor()
        {
            _kinectSensor?.Stop();
            _kinectSensor?.Dispose();
            _kinectSensor = null;
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
                    _lastPixels = (DepthImagePixel[]) _currentPixels.Clone();

                var pixelDistanceMatchCount = 0;
                for (var i = 0; i < _currentPixels.Length; i++)
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
                    WakeUpProgressEvent?.Invoke(_successCount, SuccessCountRequired);

                    if (_successCount >= SuccessCountRequired)
                        //Enough successes;  Start main wakeup routine and shutdown Kinect.
                    {
                        WakeUpConfirmedEvent?.Invoke();
                        return;
                    }
                }
                else //Lost sight of user; must have laid back down. Reset volume and success counter.
                {
                    _successCount = 0;
                    WakeUpProgressEvent?.Invoke(_successCount, SuccessCountRequired);
                }

                _lastPixels = (DepthImagePixel[]) _currentPixels.Clone();
            }
        }

        public event WakeUpConfirmed WakeUpConfirmedEvent;

        public event WakeUpProgress WakeUpProgressEvent;

        public void Dispose()
        {
            ShutdownKinectSensor();
        }
    }
}