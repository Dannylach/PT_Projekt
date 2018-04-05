using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Shapes;
using Emgu.CV.CvEnum;

namespace HandPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VideoCapture _capture;
        private DispatcherTimer _timer;

        private Rectangle _myRectangle;
        private Line _myLine;
        private bool _drawing = false;
        public MainWindow()
        {
            InitializeComponent();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _capture = new VideoCapture();
            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var currentFrame = _capture.QueryFrame();
            var mirrorFrame = new Mat();
            CvInvoke.Flip(currentFrame, mirrorFrame, FlipType.Horizontal);
            Canvas.Background = new ImageBrush(ToBitmapSource(mirrorFrame));
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (var source = image.Bitmap)
            {
                var ptr = source.GetHbitmap(); //obtain the Hbitmap

                var bs = System.Windows.Interop
                    .Imaging.CreateBitmapSourceFromHBitmap(
                        ptr,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        private void Canvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _drawing = true;
            _myLine = new Line
            {
                X1 = e.GetPosition(Canvas).X,
                Y1 = e.GetPosition(Canvas).Y,
                X2 = e.GetPosition(Canvas).X,
                Y2 = e.GetPosition(Canvas).Y,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 4
            };
            Canvas.Children.Add(_myLine);
        }

        private void Canvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_drawing)
            {
                _drawing = false;
            }
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_drawing)
            {
                _myLine.X2 = e.GetPosition(Canvas).X;
                _myLine.Y2 = e.GetPosition(Canvas).Y;
            }
        }
    }
}
