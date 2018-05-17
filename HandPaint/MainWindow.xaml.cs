using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Emgu.CV.CvEnum;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Ellipse = System.Windows.Shapes.Ellipse;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;


namespace HandPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const int MillisecondsToLoad = 500;
        private const int MillisecondsPerTick = 10;
        private static bool handDetecting = false;

        private VideoCapture _capture;
        private DispatcherTimer _timer;
        private DispatcherTimer _mouseOverTimer;

        private Point _startPoint;
        private Rectangle _myRectangle;
        private Line _myLine;
        private Ellipse _myEllipse;
        private bool _drawing;
        private Mode _mode = Mode.Brush;
        private Mode _tmpMode;
        private UIElement _tmpMouseOverObject;
        private int _millisecondsWhenMouseOver;

        private PathGeometry _pathGeometry;
        private Path _path;
        private PathFigure _pathFigure;

        private int _strokeThickness = 5;
        private Brush _selectedBrush = Brushes.Red;
        private bool _colorSelecting = false;



        public MainWindow()
        {
            InitializeComponent();
            LoadingProgressBar.Maximum = MillisecondsToLoad;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _capture = new VideoCapture();
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            _timer.Start();

            _mouseOverTimer = new DispatcherTimer();
            _mouseOverTimer.Tick += MauseOverTimer_Tick;
            _mouseOverTimer.Interval = new TimeSpan(0, 0, 0, 0, MillisecondsPerTick);
            SelectedColorRectangle.Fill = _selectedBrush;

            SelectedModeTextBox.Text = _mode.ToString();
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

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            var handDetection = new HandDetection();
            if (handDetecting)
            {
                using (var source = handDetection.DetectHand(image.Bitmap))
                {
                    var ptr = source.GetHbitmap(); //obtain the Hbitmap

                    var bs = System.Windows.Interop
                        .Imaging.CreateBitmapSourceFromHBitmap(
                            ptr,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                    DeleteObject(ptr); //release the HBitmap
                    return bs;
                }
            }
            else
            {
                using (var source = image.Bitmap)
                {
                    var ptr = source.GetHbitmap(); //obtain the Hbitmap

                    var bs = System.Windows.Interop
                        .Imaging.CreateBitmapSourceFromHBitmap(
                            ptr,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                    DeleteObject(ptr); //release the HBitmap
                    return bs;
                }
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
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
                        Stroke = _selectedBrush,
                        StrokeThickness = _strokeThickness
                    };
                    Canvas.Children.Add(_myLine);
                    break;
                case Mode.Rectangle:
                    _myRectangle = new Rectangle
                    {
                        Stroke = _selectedBrush,
                        StrokeThickness = _strokeThickness
                    };
                    Canvas.SetLeft(_myRectangle, _startPoint.X);
                    Canvas.SetTop(_myRectangle, _startPoint.Y);
                    Canvas.Children.Add(_myRectangle);
                    break;
                case Mode.Ellipse:
                    _myEllipse = new Ellipse
                    {
                        Stroke = _selectedBrush,
                        StrokeThickness = _strokeThickness
                    };
                    Canvas.SetLeft(_myEllipse, _startPoint.X);
                    Canvas.SetTop(_myEllipse, _startPoint.Y);
                    Canvas.Children.Add(_myEllipse);
                    break;
                case Mode.Brush:
                    //_brushTimer.Start();
                    _pathGeometry = new PathGeometry();
                    _pathFigure = new PathFigure();
                    _pathFigure.StartPoint = _startPoint;
                    _pathFigure.IsClosed = false;
                    _pathGeometry.Figures.Add(_pathFigure);
                    _path = new Path();
                    _path.Stroke = _selectedBrush;
                    _path.StrokeThickness = _strokeThickness;
                    _path.Data = _pathGeometry;
                    Canvas.Children.Add(_path);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_drawing)
            {
                _drawing = false;
                _myLine = null;
                _myRectangle = null;
                _myEllipse = null;
                _path = null;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
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
                    LineSegment ls = new LineSegment();
                    ls.Point = pos;
                    _pathFigure.Segments.Add(ls);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SrartChangingMode(Mode mode, UIElement sender)
        {
            _mouseOverTimer.Stop();
            _tmpMouseOverObject = sender;
            _tmpMode = mode;
            LoadingProgressBar.Visibility = Visibility.Visible;
            _mouseOverTimer.Start();
        }

        private void ChangeModeBrush_OnMouseEnter(object sender, MouseEventArgs e)
        {
            SrartChangingMode(Mode.Brush, (UIElement) sender);
            Interface_MouseEnter(sender, e);
        }

        private void ChangeModeRectangle_OnMouseEnter(object sender, MouseEventArgs e)
        {
            SrartChangingMode(Mode.Rectangle, (UIElement) sender);
            Interface_MouseEnter(sender, e);
        }

        private void ChangeModeEllipse_OnMouseEnter(object sender, MouseEventArgs e)
        {
            SrartChangingMode(Mode.Ellipse, (UIElement) sender);
            Interface_MouseEnter(sender, e);
        }

        private void ChangeModeLine_OnMouseEnter(object sender, MouseEventArgs e)
        {
            SrartChangingMode(Mode.Line, (UIElement) sender);
            Interface_MouseEnter(sender, e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void ColorWheel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _colorSelecting = true;
            var point = e.GetPosition(ColorWheel);
            SelectColorFromImage(point, ColorWheel, "kolo-barw.png");
        }

        private void ChangeSelectedColor(Brush brush)
        {
            _selectedBrush = brush;
            SelectedColorRectangle.Fill = _selectedBrush;
        }

        private void SelectColorFromImage(Point point, System.Windows.Controls.Image image, string filename)
        {
            Bitmap bitmap = new Bitmap(filename);
            var xBitmap = point.X / image.Width * bitmap.Width;
            var yBitmap = point.Y / image.Height * bitmap.Height;
            var color = bitmap.GetPixel((int)xBitmap, (int)yBitmap);
            ChangeSelectedColor(new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B)));
        }

        private void ColorWheel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _colorSelecting = false;
        }

        private void ColorWheel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_colorSelecting)
            {
                var point = e.GetPosition(ColorWheel);
                SelectColorFromImage(point, ColorWheel, "kolo-barw.png");
            }
        }

        private void Interface_MouseEnter(object sender, MouseEventArgs e)
        {
            _drawing = false;
        }

        private void ColorWheel_MouseLeave(object sender, MouseEventArgs e)
        {
            _colorSelecting = false;
        }
    }
}