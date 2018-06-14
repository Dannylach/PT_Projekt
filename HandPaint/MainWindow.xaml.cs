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
using Emgu.CV.Structure;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Ellipse = System.Windows.Shapes.Ellipse;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;


namespace HandPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private delegate void Action();

        private const int MillisecondsToLoad = 200;
        private const int MillisecondsPerTick = 10;
        private HandDetection handDetection = new HandDetection();
        private VideoCapture _capture;
        private DispatcherTimer _videoTimer;
        private DispatcherTimer _detectorTimer;
        private DispatcherTimer _mouseOverTimer;
        private Action _action;
        private bool _isMouseOverAction;

        private Point _startPoint;
        private Rectangle _myRectangle;
        private Line _myLine;
        private Ellipse _myEllipse;
        private bool _drawing;
        private Mode _mode = Mode.Brush;
        private Mode _tmpMode;
        private Shape _tmpMouseOverObject;
        private int _millisecondsWhenMouseOver;

        private PathGeometry _pathGeometry;
        private Path _path;
        private PathFigure _pathFigure;

        private int _strokeThickness = 5;
        private Brush _selectedBrush = Brushes.Red;
        private bool _colorSelecting;

        private int _dpiSavedImage = 96;
        private string _filenameSavedImage = "image.jpeg";
        private const double ColorWheelScale = 4;

        private const int PixelDistance = 2000;

        private BackgroundMode _currentBackgroundMode;
        private bool _detectionEnabled;
        private Hsv _minHsv;
        private Hsv _maxHsv;
        private bool _detectDrawing = false;
        

        public MainWindow()
        {
            InitializeComponent();
            LoadingProgressBar.Maximum = MillisecondsToLoad;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _capture = new VideoCapture();
            _videoTimer = new DispatcherTimer();
            _videoTimer.Tick += VideoTimerTick;
            _videoTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            _videoTimer.Start();

            _detectorTimer = new DispatcherTimer();
            _detectorTimer.Tick += DetectorTimerTick;
            _detectorTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            _detectorTimer.Start();

            _mouseOverTimer = new DispatcherTimer();
            _mouseOverTimer.Tick += MauseOverTimer_Tick;
            _mouseOverTimer.Interval = new TimeSpan(0, 0, 0, 0, MillisecondsPerTick);
            SelectedColorRectangle.Fill = _selectedBrush;

            SelectedModeTextBox.Text = _mode.ToString();

            _currentBackgroundMode = BackgroundMode.Camera;
            _detectionEnabled = false;
            _minHsv = handDetection.hsv_min;
            _maxHsv = handDetection.hsv_max;
            SizeTextBlock.Text = "Size: " + _strokeThickness;

        }

        private void VideoTimerTick(object sender, EventArgs e)
        {
            var currentFrame = _capture.QueryFrame();
            var mirrorFrame = new Mat();
            CvInvoke.Flip(currentFrame, mirrorFrame, FlipType.Horizontal);
            if (_currentBackgroundMode == BackgroundMode.Camera)
            {
                Canvas.Background = new ImageBrush(ToBitmapSource(mirrorFrame));
            }
        }

        private void DetectorTimerTick(object sender, EventArgs e)
        {
            var currentFrame = _capture.QueryFrame();
            var mirrorFrame = new Mat();
            CvInvoke.Flip(currentFrame, mirrorFrame, FlipType.Horizontal);
            var pointer = handDetection.DetectHand(mirrorFrame.Bitmap);
            var mousePosition = CountMousePosition(mirrorFrame, pointer);

            if ((mousePosition.X - System.Windows.Forms.Cursor.Position.X) < PixelDistance &&
                (mousePosition.X - System.Windows.Forms.Cursor.Position.X) > -PixelDistance &&
                (mousePosition.Y - System.Windows.Forms.Cursor.Position.Y) < PixelDistance &&
                (mousePosition.Y - System.Windows.Forms.Cursor.Position.Y) > -PixelDistance)
            {
                if (_detectionEnabled)
                {
                    System.Windows.Forms.Cursor.Position = mousePosition;
                    bool handDetectDrowing = handDetection.IsDrawing();
                    if (!_detectDrawing && handDetectDrowing)
                    {
                        _detectDrawing = true;

                        _drawing = true;
                        _startPoint = Mouse.GetPosition(Canvas);
                        switch (_mode)
                        {
                            case Mode.None:
                                break;
                            case Mode.Line:
                                _myLine = new Line
                                {
                                    X1 = Mouse.GetPosition(Canvas).X,
                                    Y1 = Mouse.GetPosition(Canvas).Y,
                                    X2 = Mouse.GetPosition(Canvas).X,
                                    Y2 = Mouse.GetPosition(Canvas).Y,
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
                    else
                    {
                        if (_detectDrawing && handDetectDrowing)
                        {
                            _detectDrawing = false;
                            if (_drawing)
                            {
                                _drawing = false;
                                _myLine = null;
                                _myRectangle = null;
                                _myEllipse = null;
                                _path = null;
                            }
                        }
                    }
                }
            }
        }

        private void MauseOverTimer_Tick(object sender, EventArgs e)
        {
            if (_tmpMouseOverObject.IsMouseOver && _isMouseOverAction)
            {
                if (_millisecondsWhenMouseOver < MillisecondsToLoad)
                {
                    _millisecondsWhenMouseOver += MillisecondsPerTick;
                    LoadingProgressBar.Value = _millisecondsWhenMouseOver;
                }
                else
                {
                    _action();
                    LoadingProgressBar.Value = 0;
                    LoadingProgressBar.Visibility = Visibility.Hidden;
                    _isMouseOverAction = false;
                }
            }
            else
            {
                _millisecondsWhenMouseOver = 0;
                LoadingProgressBar.Value = 0;
                LoadingProgressBar.Visibility = Visibility.Hidden;
                _isMouseOverAction = false;
            }
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

        private void StartChangingMode(Mode mode, Shape sender)
        {
            _tmpMode = mode;
            StartMauseOverAction(ChangeMode, sender);
        }

        private void ChangeMode()
        {
            _mode = _tmpMode;

            SelectedModeTextBox.Text = _mode.ToString();
            SelectedModeRectangle.Fill = _tmpMouseOverObject.Fill;
            _tmpMode = Mode.None;
        }

        private void ChangeModeBrush_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartChangingMode(Mode.Brush, (Shape) sender);
            Interface_MouseEnter();
        }

        private void ChangeModeRectangle_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartChangingMode(Mode.Rectangle, (Shape) sender);
            Interface_MouseEnter();
        }

        private void ChangeModeEllipse_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartChangingMode(Mode.Ellipse, (Shape) sender);
            Interface_MouseEnter();
        }

        private void ChangeModeLine_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartChangingMode(Mode.Line, (Shape) sender);
            Interface_MouseEnter();
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
            var xBitmap = point.X / image.ActualWidth * bitmap.Width;
            var yBitmap = point.Y / image.ActualHeight * bitmap.Height;
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

        private void Interface_MouseEnter()
        {
            _drawing = false;
        }
        
        private void SaveCanvas()
        {
            Size size = new Size(Width, Height);
            Canvas.Measure(size);
            //canvas.Arrange(new Rect(size));

            var rtb = new RenderTargetBitmap(
                (int)Width, //width 
                (int)Height, //height 
                _dpiSavedImage, //dpi x 
                _dpiSavedImage, //dpi y 
                PixelFormats.Pbgra32 // pixelformat 
            );
            rtb.Render(Canvas);

            SaveRtbAsJpeg(rtb, _filenameSavedImage);
        }

        private void SaveRtbAsJpeg(RenderTargetBitmap bmp, string filename)
        {
            var enc = new JpegBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));

            using (var stm = System.IO.File.Create(filename))
            {
                enc.Save(stm);
            }
        }

        private void Save_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartMauseOverAction(SaveCanvas, sender);
        }

        private void ColorWheel_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Interface_MouseEnter();
            ColorWheelGrid.Height *= ColorWheelScale;
            ColorWheelGrid.Width = ((ColorWheelGrid.Width - ColorWheel.Margin.Left) * ColorWheelScale) +
                                   ColorWheel.Margin.Left;
        }

        private void ColorWheel_OnMouseLeave(object sender, MouseEventArgs e)
        {
            _colorSelecting = false;
            ColorWheelGrid.Height /= ColorWheelScale;
            ColorWheelGrid.Width = ((ColorWheelGrid.Width - ColorWheel.Margin.Left) / ColorWheelScale) +
                                   ColorWheel.Margin.Left;
        }

        private void ClearAll_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartMauseOverAction(ClearCanvas, sender);
        }

        private void StartMauseOverAction(Action action, object sender)
        {
            _tmpMouseOverObject = (Shape)sender;
            _mouseOverTimer.Stop();
            _action = action;
            _isMouseOverAction = true;
            LoadingProgressBar.Visibility = Visibility.Visible;
            _mouseOverTimer.Start();
        }

        private void ClearCanvas()
        {
            Canvas.Children.Clear();
        }

        private System.Drawing.Point CountMousePosition(IImage image, PointF pointRelativeToImage)
        {
            var imageHeight = image.Bitmap.Height;
            var imageWidth = image.Bitmap.Width;
            var xMultiplier = 1920 / imageWidth; //TODO zmienić wartości nie na sztywno
            var yMultiplier = 1080 / imageHeight;
            var result = new System.Drawing.Point(Convert.ToInt32(pointRelativeToImage.X * xMultiplier), Convert.ToInt32(pointRelativeToImage.Y * yMultiplier));
            return result;
        }

        private void ChangeBackground_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartMauseOverAction(ChangeBackgroundMethod, sender);
        }

        private void ChangeBackgroundMethod()
        {
            if (_currentBackgroundMode == BackgroundMode.Camera)
            {
                _currentBackgroundMode = BackgroundMode.White;
                Canvas.Background = Brushes.White;
                return;
            }

            if (_currentBackgroundMode == BackgroundMode.White)
            {
                _currentBackgroundMode = BackgroundMode.Camera;
                return;
            }
        }

        private void ChangeDetection_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartMauseOverAction(ChangeDetectionMethod, sender);
        }

        private void ChangeDetectionMethod()
        {
            _detectionEnabled = !_detectionEnabled;
        }

        public void SetHsvValues(Hsv min, Hsv max)
        {
            _minHsv = min;
            _maxHsv = max;
            handDetection.hsv_min = _minHsv;
            handDetection.hsv_max = _maxHsv;
        }

        private void OpenConfigurationWindow_OnMouseEnter(object sender, MouseEventArgs e)
        {
            StartMauseOverAction(OpenConfigurationWindowMethod, sender);
        }

        private void OpenConfigurationWindowMethod()
        {
            _minHsv.Hue = 0;
            _minHsv.Satuation = 0;
            _minHsv.Value = 0;
            _maxHsv.Hue = 180;
            _maxHsv.Satuation = 255;
            _maxHsv.Value = 255;
            ColorConfiguration colorConfiguration = new ColorConfiguration(_minHsv, _maxHsv, this);
            //colorConfiguration.Parent = this;
            colorConfiguration.Show();
        }
    }
}