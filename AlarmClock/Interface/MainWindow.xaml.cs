using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using AlarmClock.Utilities;
using ForecastIO;

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

        private ForecastIOResponse _forecast;

        //-------- Background Images --------//

        /// <summary>
        /// An array containing the two background images that will overlay and fade between each other over time.
        /// </summary>
        private Image[] _backgroundImages;

        private int _currentBackgroundStage;
        private string _lastUsedBackgroundSource;

        private const short BackgroundSwitchInterval = 15;
        private const short BackgroundMovementDuration = 40;
        private const short BackgroundTransitionDuration = 10;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Background = new SolidColorBrush(Colors.Black);

            _wakeUpDetector = new WakeUpDetector();
            _wakeUpDetector.WakeUpConfirmedEvent += WakeUpDetectorOnWakeUpConfirmedEvent;
            _wakeUpDetector.WakeUpProgressEvent += WakeUpDetectorOnWakeUpProgressEvent;

            _musicPlayer = new MusicPlayer();

            var request = new ForecastIORequest("1151936ce22897f529d08c57a1e8bb71", 40.579219f, -111.999608f, Unit.us);
            _forecast = request.Get();

            StartClock();

            if (_wakeUpDetector.StartDetection())
            {
                Background = new SolidColorBrush(Colors.Black);

                //Begin playing alarm clock sound on loop
                _musicPlayer.InitAudioAsset("AlarmClock.wav", true);
                _musicPlayer.PlayMusic();
            }
            else //Could not load Kinect Sensor. Just skip it.
            {
                _wakeUpDetector.Dispose();
                DoWakeupRoutine();
            }
        }

        private void StartClock()
        {
            UpdateClock(DateTime.Now);

            ClockLabel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1.0, TimeSpan.FromSeconds(2)));

            var timer = new DispatcherTimer(DispatcherPriority.Background) {Interval = TimeSpan.FromSeconds(1)};
            timer.Tick += (sender, args) => { UpdateClock(DateTime.Now); };
            timer.Start();
        }

        private void UpdateClock(DateTime time)
        {
            ClockLabel.Text = time.ToString("hh:mm:ss tt");
        }

        private void DoWakeupRoutine()
        {
            ProgressBar.BeginAnimation(OpacityProperty, new DoubleAnimation(1.0, 0, TimeSpan.FromSeconds(2)));
            //Fade out progress bar

            _musicPlayer.InitAudioAsset("Intro.wav", false); //Init intro music.
            _musicPlayer.PlayMusic(); //Play

            LoadBackgroundImages();

            LoadWeatherTiles();
        }

        private void LoadBackgroundImages()
        {
            _backgroundImages = new[] {new Image(), new Image()};

            foreach (var image in _backgroundImages)
            {
                MainCanvas.Children.Insert(0, image);

                image.Width = ActualWidth*1.2;
                image.Height = ActualHeight*1.2;
                image.Stretch = Stretch.UniformToFill;
                image.Opacity = 0;
            }

            var timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromSeconds(BackgroundSwitchInterval)
            };
            timer.Tick += SwitchImageTick;
            timer.Start();
            SwitchImageTick(null, null);
        }

        private static string GetRandomBackgroundImageAssetPath(string previousImagePath)
        {
            var files = Directory.GetFiles("assets/images/backgrounds/");
            var numImages = files.Length;
            if (numImages == 1)
                return files[0];

            var random = new Random();
            var randomNum = 0;
            do
            {
                randomNum = random.Next(numImages);
            } while (previousImagePath != null && files[randomNum] == previousImagePath);

            return files[randomNum];
        }

        public void SwitchImageTick(object source, EventArgs args)
        {
            var moveTop = 0;
            var moveLeft = 0;

            var usingImageIndex = _currentBackgroundStage%2;
            var otherImageIndex = usingImageIndex ^ 1;

            switch (_currentBackgroundStage)
            {
                case 1:
                    moveLeft = 1;
                    break;
                case 2:
                    moveTop = 1;
                    moveLeft = 1;
                    break;
                case 3:
                    moveTop = 1;
                    break;
                default:
                    break;
            }

            var newTop = -(moveTop*(_backgroundImages[usingImageIndex].Height - ActualHeight));
            var oppositeTop = -((moveTop ^ 1)*(_backgroundImages[usingImageIndex].Height - ActualHeight));
            var newLeft = -(moveLeft*(_backgroundImages[usingImageIndex].Width - ActualWidth));
            var oppositeLeft = -((moveLeft ^ 1)*(_backgroundImages[usingImageIndex].Width - ActualWidth));

            _backgroundImages[usingImageIndex].Source =
                new BitmapImage(
                    AssetFinder.PathToUri(
                        _lastUsedBackgroundSource = GetRandomBackgroundImageAssetPath(_lastUsedBackgroundSource)));

            _backgroundImages[usingImageIndex].BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1.0, TimeSpan.FromSeconds(BackgroundTransitionDuration)));
            _backgroundImages[otherImageIndex].BeginAnimation(OpacityProperty,
                new DoubleAnimation(1.0, 0, TimeSpan.FromSeconds(BackgroundTransitionDuration)));

            StartImageMovement(_backgroundImages[usingImageIndex], newTop, oppositeTop, newLeft, oppositeLeft);

            _currentBackgroundStage++;
            if (_currentBackgroundStage == 4)
                _currentBackgroundStage = 0;
        }

        private static void StartImageMovement(Image image, double fromTop, double toTop, double fromLeft, double toLeft)
        {
            Canvas.SetTop(image, fromTop);
            Canvas.SetLeft(image, fromLeft);

            image.BeginAnimation(Canvas.TopProperty,
                new DoubleAnimation(fromTop, toTop, TimeSpan.FromSeconds(BackgroundMovementDuration)));
            image.BeginAnimation(Canvas.LeftProperty,
                new DoubleAnimation(fromLeft, toLeft, TimeSpan.FromSeconds(BackgroundMovementDuration)));
        }

        private async void LoadWeatherTiles()
        {
            const int numTiles = 7;
            var division = Math.Floor(this.ActualWidth/numTiles);

            var tileWidth = Math.Floor(division*0.8);
            var margin = Math.Floor(division*0.1);

            var beepPlayer = new MusicPlayer();
            beepPlayer.InitAudioAsset("Beep.wav", false);

            await Task.Delay(TimeSpan.FromMilliseconds(2000));

            var counter = 0;

            var timer = new DispatcherTimer(DispatcherPriority.Render) {Interval = TimeSpan.FromMilliseconds(300)};
            timer.Tick += (sender, args) =>
            {
                if (counter >= numTiles)
                {
                    beepPlayer.Dispose();
                    timer.Stop();
                    Speak();
                }
                else
                {
                    beepPlayer.PlayMusic();

                    var rect = new Rectangle
                    {
                        Width = tileWidth,
                        Height = 200,
                        Fill = new SolidColorBrush(Colors.White),
                        Opacity = 0.0
                    };

                    rect.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 0.6, TimeSpan.FromSeconds(0.8)));

                    MainCanvas.Children.Add(rect);
                    Canvas.SetBottom(rect, margin);
                    Canvas.SetLeft(rect, ((margin + tileWidth + margin)*counter) + margin);
                }

                counter++;
            };
            timer.Start();
        }

        private async void Speak()
        {
            var counter = 0;
            var timer = new DispatcherTimer(DispatcherPriority.Render) {Interval = TimeSpan.FromMilliseconds(30)};
            timer.Tick += (sender, args) =>
            {
                _musicPlayer.SetVolume(1.0f - (counter*0.008f));
                counter++;
                if (counter == 100) //(100 * 0.008 = 0.8), resulting in 0.2 volume in (30 * 100 = 3) seconds
                    timer.Stop();
            };
            timer.Start();

            await Task.Factory.StartNew(() =>
            {
                var greeting = "Good";
                var hour = DateTime.Now.Hour;
                if (hour > 18)
                    greeting += "Evening";
                else if (hour > 12)
                    greeting += "Afternoon";
                else
                    greeting += "Morning";

                var summary = greeting + ". It is " + DateHelper.DateToText(DateTime.Now, true) +
                                 ". Today's weather will be " +
                                 _forecast.daily.data[0].summary +
                                 ". A summary of the weather this week is: " + _forecast.daily.summary;
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://www.acapela-group.com");
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("MyLanguages", "sonid9"),
                        new KeyValuePair<string, string>("MySelectedVoice", "Graham"),
                        new KeyValuePair<string, string>("MyTextForTTS", summary),
                        new KeyValuePair<string, string>("SendToVaaS", "")
                    });
                    var result = client.PostAsync("/demo-tts/DemoHTML5Form_V2.php", content).Result;
                    var resultContent = result.Content.ReadAsStringAsync().Result;
                    const string pattern = "myPhpVar = '(.+)';";
                    var match = Regex.Match(resultContent, pattern);
                    if (match.Groups.Count > 1)
                        MusicPlayer.PlayMp3FromUrl(match.Groups[1].ToString());
                }
            });
        }

        private void WakeUpDetectorOnWakeUpConfirmedEvent()
        {
            _musicPlayer.StopMusic();
            _wakeUpDetector.Dispose();
            DoWakeupRoutine();
        }

        private void WakeUpDetectorOnWakeUpProgressEvent(short progress, short max)
        {
            var relativeProgress = (float) (1.0 - ((float) progress/max)*1.0);
            _musicPlayer.SetVolume(relativeProgress); //Adjust volume for fade
            ProgressBar.Width = ActualWidth*(1 - relativeProgress); //Adjust progress bar width
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _wakeUpDetector?.Dispose();
        }
    }
}