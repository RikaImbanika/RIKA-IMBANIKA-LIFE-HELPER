using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RIKA_IMBANIKA_LIFE_HELPER
{
    public class MovingImageWindow : Window
    {
        private enum Side { Top, Bottom, Left, Right }
        private enum Direction { Forward, Backward }

        private readonly Image _image;
        private readonly Grid _worldGrid;
        private readonly Canvas _canvas;

        private readonly ColumnDefinition _colLeft, _colMiddle, _colRight;
        private readonly RowDefinition _rowTop, _rowMiddle, _rowBottom;

        private readonly double _screenWidth, _screenHeight;

        public double SizeFactor { get; set; } = 0.2;
        public double Amplitude { get; set; } = 0.5;
        public double Frequency { get; set; } = 1.35;
        public double Duration { get; set; } = 5.0;

        private Side _currentSide;
        private Direction _currentDirection;
        private DateTime _startTime;

        private double _baseWidth;
        private double _baseHeight;
        private double _maxWidth;
        private double _maxHeight;

        public MovingImageWindow(string imagePath)
        {
            _screenWidth = SystemParameters.PrimaryScreenWidth;
            _screenHeight = SystemParameters.PrimaryScreenHeight;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            IsHitTestVisible = false;
            ShowActivated = false;
            Focusable = false;

            Left = 0;
            Top = 0;
            Width = _screenWidth;
            Height = _screenHeight;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.EndInit();
            bmp.Freeze();

            _baseWidth = _screenWidth * SizeFactor;
            _baseHeight = _baseWidth * bmp.PixelHeight / bmp.PixelWidth;
            _maxWidth = _baseWidth * (1 + Amplitude);
            _maxHeight = _baseHeight * (1 + Amplitude);

            _image = new Image
            {
                Source = bmp,
                Stretch = Stretch.Fill,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.HighQuality);

            _worldGrid = new Grid();

            _colLeft = new ColumnDefinition { Width = new GridLength(_maxWidth) };
            _colMiddle = new ColumnDefinition { Width = new GridLength(_baseWidth) };
            _colRight = new ColumnDefinition { Width = new GridLength(_maxWidth) };
            _worldGrid.ColumnDefinitions.Add(_colLeft);
            _worldGrid.ColumnDefinitions.Add(_colMiddle);
            _worldGrid.ColumnDefinitions.Add(_colRight);

            _rowTop = new RowDefinition { Height = new GridLength(_maxHeight) };
            _rowMiddle = new RowDefinition { Height = new GridLength(_baseHeight) };
            _rowBottom = new RowDefinition { Height = new GridLength(_maxHeight) };
            _worldGrid.RowDefinitions.Add(_rowTop);
            _worldGrid.RowDefinitions.Add(_rowMiddle);
            _worldGrid.RowDefinitions.Add(_rowBottom);

            Grid.SetColumn(_image, 1);
            Grid.SetRow(_image, 1);
            _worldGrid.Children.Add(_image);

            _canvas = new Canvas
            {
                Width = _screenWidth,
                Height = _screenHeight,
                ClipToBounds = true,
                Background = Brushes.Transparent
            };
            _canvas.Children.Add(_worldGrid);
            Content = _canvas;

            Hide();
        }

        public void StartAnimation(string side, string dir)
        {
            if (!Enum.TryParse(side, out Side parsedSide) ||
                !Enum.TryParse(dir, out Direction parsedDir))
                return;

            _currentSide = parsedSide;
            _currentDirection = parsedDir;

            double rotationAngle;
            switch (_currentSide)
            {
                case Side.Bottom: rotationAngle = 0; break;
                case Side.Top: rotationAngle = 180; break;
                case Side.Left: rotationAngle = 90; break;
                case Side.Right: rotationAngle = -90; break;
                default: rotationAngle = 0; break;
            }

            double scaleX = 1, scaleY = 1;
            bool needReflection =
                (_currentDirection == Direction.Backward && (_currentSide == Side.Bottom || _currentSide == Side.Left))
             || (_currentDirection == Direction.Forward && (_currentSide == Side.Top || _currentSide == Side.Right));
            if (needReflection)
            {
                if (_currentSide == Side.Bottom || _currentSide == Side.Top) scaleX = -1;
                else scaleY = -1;
            }

            var tg = new TransformGroup();
            tg.Children.Add(new RotateTransform(rotationAngle));
            tg.Children.Add(new ScaleTransform(scaleX, scaleY));
            _image.RenderTransform = tg;

            Duration = (_currentSide == Side.Left || _currentSide == Side.Right) ? 3.5 : 5.0;

            _startTime = DateTime.Now;

            UpdateAnimation(0);

            CompositionTarget.Rendering += OnRendering;
            Show();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            double elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

            if (elapsedSeconds >= Duration)
            {
                CompositionTarget.Rendering -= OnRendering;
                Hide();
                return;
            }

            UpdateAnimation(elapsedSeconds);
        }

        private void UpdateAnimation(double elapsedSeconds)
        {
            double t = elapsedSeconds / Duration;
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            double pulsePhase = (elapsedSeconds * Frequency) % 1.0;
            if (pulsePhase < 0) pulsePhase += 1.0;

            double x = 2.0 * pulsePhase - 1.0;
            double shape = 1.0 - 2.0 * Math.Abs(x * x * x);

            double scale = 1.0 + Amplitude * shape;

            double cellW = _baseWidth;
            double cellH = _baseHeight * scale;

            _colMiddle.Width = new GridLength(cellW);
            _rowMiddle.Height = new GridLength(cellH);

            double visW, visH;
            switch (_currentSide)
            {
                case Side.Left:
                case Side.Right:
                    visW = cellH; visH = cellW; break;
                default:
                    visW = cellW; visH = cellH; break;
            }

            double moveT = (_currentDirection == Direction.Forward) ? t : (1 - t);

            double targetCenterX, targetCenterY;
            switch (_currentSide)
            {
                case Side.Bottom:
                    targetCenterY = _screenHeight - visH / 2.0;
                    targetCenterX = -visW / 2.0 + moveT * (_screenWidth + visW);
                    break;

                case Side.Top:
                    targetCenterY = visH / 2.0;
                    targetCenterX = -visW / 2.0 + moveT * (_screenWidth + visW);
                    break;

                case Side.Left:
                    targetCenterX = visW / 2.0;
                    targetCenterY = -visH / 2.0 + moveT * (_screenHeight + visH);
                    break;

                case Side.Right:
                    targetCenterX = _screenWidth - visW / 2.0;
                    targetCenterY = -visH / 2.0 + moveT * (_screenHeight + visH);
                    break;

                default:
                    targetCenterX = 0; targetCenterY = 0;
                    break;
            }

            double gridX = targetCenterX - _maxWidth - cellW / 2.0;
            double gridY = targetCenterY - _maxHeight - cellH / 2.0;

            Canvas.SetLeft(_worldGrid, gridX);
            Canvas.SetTop(_worldGrid, gridY);
        }
    }
}