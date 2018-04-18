using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emgu.CV.CvEnum;
using Ellipse = System.Windows.Shapes.Ellipse;


namespace HandPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MillisecondsToLoad = 500;
        private const int MillisecondsPerTick = 10;
        private bool _programmRunning;

        private VideoCapture _capture;
        private DispatcherTimer _timer;
        private DispatcherTimer _mouseOverTimer;
        private DispatcherTimer _brushTimer;
        private Point _position;

        private Point _startPoint;
        private Rectangle _myRectangle;
        private Line _myLine;
        private Ellipse _myEllipse;
        private bool _drawing = false;
        private Mode _mode = Mode.Brush;
        private Mode _tmpMode;
        private UIElement _tmpMouseOverObject = null;
        private int _millisecondsWhenMouseOver = 0;

        private Ellipse _brush = new Ellipse()
        {
            Stroke = Brushes.Transparent,
            StrokeThickness = 0,
            Height = 5,
            Width = 5,
            Fill = Brushes.Black
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadingProgressBar.Maximum = MillisecondsToLoad;
            _programmRunning = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _capture = new VideoCapture();
            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            _timer.Start();

            _mouseOverTimer = new DispatcherTimer();
            _mouseOverTimer.Tick += new EventHandler(MauseOverTimer_Tick);
            _mouseOverTimer.Interval = new TimeSpan(0, 0, 0, 0, MillisecondsPerTick);

            _brushTimer = new DispatcherTimer();
            _brushTimer.Tick += new EventHandler(BrushTimer_Tick);
            _brushTimer.Interval = TimeSpan.FromTicks(1);

            SelectedModeTextBox.Text = _mode.ToString();
            _brushTimer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var currentFrame = _capture.QueryFrame();
            var mirrorFrame = new Mat();
            CvInvoke.Flip(currentFrame, mirrorFrame, FlipType.Horizontal);
            Canvas.Background = new ImageBrush(ToBitmapSource(mirrorFrame));
        }

        private void MauseOverTimer_Tick(object sender, EventArgs e)
        {
            if (_tmpMouseOverObject.IsMouseOver && _tmpMode != Mode.None)
            {
                if (_millisecondsWhenMouseOver < MillisecondsToLoad)
                {
                    _millisecondsWhenMouseOver += MillisecondsPerTick;
                    LoadingProgressBar.Value = _millisecondsWhenMouseOver;
                }
                else
                {
                    _mode = _tmpMode;
                    _tmpMode = Mode.None;
                    LoadingProgressBar.Value = 0;
                    LoadingProgressBar.Visibility = Visibility.Hidden;
                    SelectedModeTextBox.Text = _mode.ToString();
                }
            }
            else
            {
                _millisecondsWhenMouseOver = 0;
                LoadingProgressBar.Value = 0;
                LoadingProgressBar.Visibility = Visibility.Hidden;
            }
        }

        private void BrushTimer_Tick(object sender, EventArgs e)
        {
            Point position = Mouse.GetPosition(Canvas);
            SelectedModeTextBox.Text = "X: " + position.X + " Y: " + position.Y;
            if (Mouse.LeftButton == MouseButtonState.Released || !_drawing || _position == position)
                return;
            _position = position;
            PutBrush(_brush, _position);
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
            _startPoint = e.GetPosition(Canvas);
            switch (_mode)
            {
                case Mode.None:
                    break;
                case Mode.Line:
                    _myLine = new Line
                    {
                        X1 = e.GetPosition(Canvas).X,
                        Y1 = e.GetPosition(Canvas).Y,
                        X2 = e.GetPosition(Canvas).X,
                        Y2 = e.GetPosition(Canvas).Y,
                        Stroke = Brushes.Red,
                        StrokeThickness = 4
                    };
                    Canvas.Children.Add(_myLine);
                    break;
                case Mode.Rectangle:
                    _myRectangle = new Rectangle
                    {
                        Stroke = Brushes.LightBlue,
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(_myRectangle, _startPoint.X);
                    Canvas.SetTop(_myRectangle, _startPoint.Y);
                    Canvas.Children.Add(_myRectangle);
                    break;
                case Mode.Ellipse:
                    _myEllipse = new Ellipse
                    {
                        Stroke = Brushes.Blue,
                        StrokeThickness = 3
                    };
                    Canvas.SetLeft(_myEllipse, _startPoint.X);
                    Canvas.SetTop(_myEllipse, _startPoint.Y);
                    Canvas.Children.Add(_myEllipse);
                    break;
                case Mode.Brush:
                    _brushTimer.Start();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Canvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_drawing)
            {
                _drawing = false;
                _myLine = null;
                _myRectangle = null;
                _myEllipse = null;
                _brushTimer.Stop();
            }
        }

        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            
            if (e.LeftButton == MouseButtonState.Released || !_drawing)
                return;

            var pos = e.GetPosition(Canvas);

            var x = Math.Min(pos.X, _startPoint.X);
            var y = Math.Min(pos.Y, _startPoint.Y);

            var w = Math.Max(pos.X, _startPoint.X) - x;
            var h = Math.Max(pos.Y, _startPoint.Y) - y;

            switch (_mode)
            {
                case Mode.None:
                    break;
                case Mode.Line:
                    _myLine.X2 = e.GetPosition(Canvas).X;
                    _myLine.Y2 = e.GetPosition(Canvas).Y;
                    break;
                case Mode.Rectangle:
                    
                    _myRectangle.Width = w;
                    _myRectangle.Height = h;

                    Canvas.SetLeft(_myRectangle, x);
                    Canvas.SetTop(_myRectangle, y);
                    break;
                case Mode.Ellipse:

                    _myEllipse.Width = w;
                    _myEllipse.Height = h;

                    Canvas.SetLeft(_myEllipse, x);
                    Canvas.SetTop(_myEllipse, y);
                    break;
                case Mode.Brush:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ChangeMode(Mode mode, UIElement sender)
        {
            _mouseOverTimer.Stop();
            _tmpMouseOverObject = sender;
            _tmpMode = mode;
            LoadingProgressBar.Visibility = Visibility.Visible;
            _mouseOverTimer.Start();
        }

        private void ChangeModeRectangle_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ChangeMode(Mode.Rectangle, (UIElement)sender);
        }

        private void ChangeModeEllipse_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ChangeMode(Mode.Ellipse, (UIElement)sender);
        }
        private void ChangeModeLine_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ChangeMode(Mode.Line, (UIElement)sender);
        }

        private void PutBrush(Shape brush, Point position)
        {
            var clonedEllipse = new Ellipse()
            {
                Stroke = brush.Stroke,
                StrokeThickness = brush.StrokeThickness,
                Height = brush.Height,
                Width = brush.Width,
                Fill = brush.Fill
            };
            Canvas.Children.Add(clonedEllipse);
            Canvas.SetLeft(clonedEllipse, position.X - clonedEllipse.Width / 2.0);
            Canvas.SetTop(clonedEllipse, position.Y - clonedEllipse.Height / 2.0);
        }
        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _programmRunning = false;
        }
    }
}
