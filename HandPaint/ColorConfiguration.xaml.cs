using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using HandPaint.SkinDetector;

namespace HandPaint
{
    /// <summary>
    /// Interaction logic for ColorConfiguration.xaml
    /// </summary>
    public partial class ColorConfiguration : Window
    {

        private Hsv _hsvMin;
        private Hsv _hsvMax;

        private VideoCapture _capture;
        private DispatcherTimer _videoTimer;
        private MainWindow _mainWindow;

        public ColorConfiguration(Hsv hsvMin, Hsv hsvMax, MainWindow window)
        {
            InitializeComponent();
            _hsvMin = hsvMin;
            _hsvMax = hsvMax;
            MinimumHLabel.Content = "H: " + _hsvMin.Hue * 2;
            MinimumSLabel.Content = "S: " + _hsvMin.Satuation;
            MinimumVLabel.Content = "V: " + _hsvMin.Value;
            MinimumHSlider.Value = _hsvMin.Hue * 2;
            MinimumSSlider.Value = _hsvMin.Satuation;
            MinimumVSlider.Value = _hsvMin.Value;
            
            MaximumHLabel.Content = "H: " + _hsvMax.Hue * 2;
            MaximumSLabel.Content = "S: " + _hsvMax.Satuation;
            MaximumVLabel.Content = "V: " + _hsvMax.Value;
            MaximumHSlider.Value = _hsvMax.Hue * 2;
            MaximumSSlider.Value = _hsvMax.Satuation;
            MaximumVSlider.Value = _hsvMax.Value;

            _capture = new VideoCapture();
            _videoTimer = new DispatcherTimer();
            _videoTimer.Tick += VideoTimerTick;
            _videoTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            _videoTimer.Start();
            _mainWindow = window;
        }

        private void VideoTimerTick(object sender, EventArgs e)
        {
            HsvSkinDetector detector = new HsvSkinDetector();
            var currentFrame = _capture.QueryFrame();
            var skin = detector.DetectSkin(currentFrame.ToImage<Bgr, Byte>(), _hsvMin, _hsvMax);
            Image.Source = ToBitmapSource(skin);
        }

        private void MinimumHSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _hsvMin.Hue = e.NewValue / 2;
            MinimumHLabel.Content = "H: " + _hsvMin.Hue;
        }

        private void MinimumSSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _hsvMin.Satuation = e.NewValue;
            MinimumSLabel.Content = "S: " + _hsvMin.Satuation;
        }

        private void MinimumVSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _hsvMin.Value = e.NewValue;
            MinimumVLabel.Content = "V: " + _hsvMin.Value;
        }

        private void MaximumHSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _hsvMax.Hue = e.NewValue / 2;
            MaximumHLabel.Content = "H: " + _hsvMax.Hue;
        }

        private void MaximumSSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _hsvMax.Satuation = e.NewValue;
            MaximumSLabel.Content = "S: " + _hsvMax.Satuation;
        }
        private void MaximumVSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _hsvMax.Value = e.NewValue;
            MaximumVLabel.Content = "V: " + _hsvMax.Value;
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public BitmapSource ToBitmapSource(IImage image)
        {
            var imageFrame = new Image<Bgr, byte>(image.Bitmap);

            using (var source = imageFrame.Bitmap)
            {

                var ptr = source.GetHbitmap();
                var bs = System.Windows.Interop
                    .Imaging.CreateBitmapSourceFromHBitmap(
                        ptr,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr);
                return bs;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mainWindow.SetHsvValues(_hsvMin, _hsvMax);
        }
    }
}
