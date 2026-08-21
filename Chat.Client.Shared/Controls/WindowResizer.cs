using System;
using System.Windows;
using System.Windows.Input;

namespace Chat.Client.Shared.Controls
{
    public class WindowResizer
    {
        private const int ResizeBorderThickness = 6;
        private readonly Window _window;
        private bool _isResizing = false;
        private ResizeDirection _resizeDirection = ResizeDirection.None;
        private Point _previousMousePosition;

        public WindowResizer(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            AttachEvents();
        }

        private void AttachEvents()
        {
            _window.MouseMove += OnMouseMove;
            _window.MouseLeftButtonDown += OnMouseLeftButtonDown;
            _window.MouseLeftButtonUp += OnMouseLeftButtonUp;
            _window.LostMouseCapture += OnLostMouseCapture;
        }

        private void DetachEvents()
        {
            _window.MouseMove -= OnMouseMove;
            _window.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            _window.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            _window.LostMouseCapture -= OnLostMouseCapture;
        }

        private ResizeDirection GetResizeDirection(Point position)
        {
            if (position.X < ResizeBorderThickness && position.Y < ResizeBorderThickness)
                return ResizeDirection.TopLeft;
            if (position.X > _window.ActualWidth - ResizeBorderThickness && position.Y < ResizeBorderThickness)
                return ResizeDirection.TopRight;
            if (position.X < ResizeBorderThickness && position.Y > _window.ActualHeight - ResizeBorderThickness)
                return ResizeDirection.BottomLeft;
            if (position.X > _window.ActualWidth - ResizeBorderThickness && position.Y > _window.ActualHeight - ResizeBorderThickness)
                return ResizeDirection.BottomRight;
            if (position.X < ResizeBorderThickness)
                return ResizeDirection.Left;
            if (position.X > _window.ActualWidth - ResizeBorderThickness)
                return ResizeDirection.Right;
            if (position.Y < ResizeBorderThickness)
                return ResizeDirection.Top;
            if (position.Y > _window.ActualHeight - ResizeBorderThickness)
                return ResizeDirection.Bottom;
            return ResizeDirection.None;
        }

        private void UpdateCursor(ResizeDirection direction)
        {
            switch (direction)
            {
                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    _window.Cursor = Cursors.SizeWE;
                    break;
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    _window.Cursor = Cursors.SizeNS;
                    break;
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomRight:
                    _window.Cursor = Cursors.SizeNWSE;
                    break;
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomLeft:
                    _window.Cursor = Cursors.SizeNESW;
                    break;
                default:
                    _window.Cursor = Cursors.Arrow;
                    break;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _isResizing)
            {
                ResizeWindow(e.GetPosition(_window));
            }
            else
            {
                _resizeDirection = GetResizeDirection(e.GetPosition(_window));
                UpdateCursor(_resizeDirection);
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_resizeDirection != ResizeDirection.None)
            {
                _isResizing = true;
                _previousMousePosition = e.GetPosition(_window);
                _window.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _resizeDirection = ResizeDirection.None;
                UpdateCursor(ResizeDirection.None);
                _window.ReleaseMouseCapture();
            }
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _resizeDirection = ResizeDirection.None;
                UpdateCursor(ResizeDirection.None);
            }
        }

        private void ResizeWindow(Point position)
        {
            double horizontalChange = position.X - _previousMousePosition.X;
            double verticalChange = position.Y - _previousMousePosition.Y;

            switch (_resizeDirection)
            {
                case ResizeDirection.Right:
                    _window.Width = Math.Max(300, _window.Width + horizontalChange);
                    break;
                case ResizeDirection.Bottom:
                    _window.Height = Math.Max(200, _window.Height + verticalChange);
                    break;
                case ResizeDirection.Left:
                    if (_window.Width - horizontalChange >= 300)
                    {
                        _window.Width -= horizontalChange;
                        _window.Left += horizontalChange;
                    }
                    break;
                case ResizeDirection.Top:
                    if (_window.Height - verticalChange >= 200)
                    {
                        _window.Height -= verticalChange;
                        _window.Top += verticalChange;
                    }
                    break;
                case ResizeDirection.TopLeft:
                    if (_window.Width - horizontalChange >= 300 && _window.Height - verticalChange >= 200)
                    {
                        _window.Width -= horizontalChange;
                        _window.Height -= verticalChange;
                        _window.Left += horizontalChange;
                        _window.Top += verticalChange;
                    }
                    break;
                case ResizeDirection.TopRight:
                    if (_window.Height - verticalChange >= 200)
                    {
                        _window.Width += horizontalChange;
                        _window.Height -= verticalChange;
                        _window.Top += verticalChange;
                    }
                    break;
                case ResizeDirection.BottomLeft:
                    if (_window.Width - horizontalChange >= 300)
                    {
                        _window.Width -= horizontalChange;
                        _window.Height += verticalChange;
                        _window.Left += horizontalChange;
                    }
                    break;
                case ResizeDirection.BottomRight:
                    _window.Width = Math.Max(300, _window.Width + horizontalChange);
                    _window.Height = Math.Max(200, _window.Height + verticalChange);
                    break;
            }

            _previousMousePosition = position;
        }

        public void Dispose()
        {
            DetachEvents();
        }
    }

    public enum ResizeDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
