using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace LibMarkdownEditor
{
    public class TextEditArea : ImeEditArea
    {
        private readonly TextBlock _renderer;
        private readonly StringBuilder _textBuilder;

        private Point _textPoint;

        private int _caret;
        private int? _selectionEnd;
        private bool _overwriteMode;

        public TextEditArea()
        {
            _renderer = new TextBlock();
            _textBuilder = new StringBuilder();

            AddVisualChild(_renderer);
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public bool Wrapping
        {
            get { return (bool)GetValue(WrappingProperty); }
            set { SetValue(WrappingProperty, value); }
        }

        public VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment)GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        protected override int VisualChildrenCount => 1;

        private void CorrectCursor()
        {
            if (_caret < 0)
            {
                _caret = 0;
                InvalidateVisual();
            }
            else if (_caret > _textBuilder.Length)
            {
                _caret = _textBuilder.Length;
                InvalidateVisual();
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
            {
                return _renderer;
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var pointRelatedRenderer = e.GetPosition(_renderer);
            var textPointer = _renderer.GetPositionFromPoint(pointRelatedRenderer, true);

            _caret = _renderer.ContentStart
                .GetInsertionPosition(System.Windows.Documents.LogicalDirection.Forward)
                .GetOffsetToPosition(textPointer);
            _selectionEnd = null;

            CaptureMouse();
            InvalidateVisual();

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                e.Handled = true;

                var pointRelatedRenderer = e.GetPosition(_renderer);
                var textPointer = _renderer.GetPositionFromPoint(pointRelatedRenderer, true);

                _selectionEnd = _renderer.ContentStart
                    .GetInsertionPosition(System.Windows.Documents.LogicalDirection.Forward)
                    .GetOffsetToPosition(textPointer);

                InvalidateVisual();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                e.Handled = true;

                if (_caret > 0)
                {
                    _caret--;
                    _textBuilder.Remove(_caret, 1);

                    Text = _textBuilder.ToString();
                }
            }
            else if (e.Key == Key.Left)
            {
                e.Handled = true;

                if (_caret > 0)
                {
                    _caret--;
                    InvalidateVisual();
                }
            }
            else if (e.Key == Key.Right)
            {
                e.Handled = true;

                if (_caret < _textBuilder.Length)
                {
                    _caret++;
                    InvalidateVisual();
                }
            }
            else if (e.Key == Key.Home)
            {
                e.Handled = true;
                _caret = 0;
                InvalidateVisual();
            }
            else if (e.Key == Key.End)
            {
                e.Handled = true;
                _caret = _textBuilder.Length;
                InvalidateVisual();
            }

            base.OnKeyDown(e);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (_caret < 0)
            {
                _caret = 0;
            }
            else if (_caret > _textBuilder.Length)
            {
                _caret = _textBuilder.Length;
            }

            if (!_overwriteMode)
            {
                _textBuilder.Insert(_caret, e.Text);
                _caret += e.Text.Length;
            }
            else
            {
                _textBuilder.Remove(_caret, Math.Min(e.Text.Length, _textBuilder.Length - _caret));
                _textBuilder.Insert(_caret, e.Text);
                _caret += e.Text.Length;
            }

            Text = _textBuilder.ToString();

            base.OnTextInput(e);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _renderer.Text = Text;
            _renderer.TextWrapping = Wrapping ? TextWrapping.Wrap : TextWrapping.NoWrap;
            _renderer.Measure(availableSize);

            return _renderer.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;

            var verticalContentAlignment = VerticalContentAlignment;
            var horizontalContentAlignment = HorizontalContentAlignment;

            _textPoint = new Point(0, 0);
            if (horizontalContentAlignment == HorizontalAlignment.Center)
            {
                _textPoint.X = (actualWidth - _renderer.DesiredSize.Width) / 2;
            }
            else if (horizontalContentAlignment == HorizontalAlignment.Right)
            {
                _textPoint.X = actualWidth - _renderer.DesiredSize.Width;
            }

            if (verticalContentAlignment == VerticalAlignment.Center)
            {
                _textPoint.Y = (actualHeight - _renderer.DesiredSize.Height) / 2;
            }
            else if (verticalContentAlignment == VerticalAlignment.Bottom)
            {
                _textPoint.Y = actualHeight - _renderer.DesiredSize.Height;
            }

            _renderer.Arrange(new Rect(_textPoint, _renderer.DesiredSize));

            return base.ArrangeOverride(finalSize);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var selectionPoint = _renderer.ContentStart
                .GetInsertionPosition(LogicalDirection.Forward)
                .GetPositionAtOffset(_caret);

            var highlightBrush = new SolidColorBrush(Color.FromRgb(151, 198, 235));

            for (int i = _caret; i < _selectionEnd; i++)
            {
                if (selectionPoint is null)
                {
                    break;
                }

                var rect = selectionPoint
                    .GetInsertionPosition(LogicalDirection.Forward)
                    .GetCharacterRect(LogicalDirection.Backward);

                drawingContext.DrawRectangle(highlightBrush, null, rect);
            }

            base.OnRender(drawingContext);
        }

        protected override Point GetEditorLeftTop()
        {
            return default;
        }

        protected override Point GetCaretLeftTop()
        {
            var textPointer = _renderer.ContentStart
                .GetInsertionPosition(System.Windows.Documents.LogicalDirection.Forward)
                .GetPositionAtOffset(_caret, System.Windows.Documents.LogicalDirection.Forward);

            var rect = textPointer.GetCharacterRect(System.Windows.Documents.LogicalDirection.Backward);

            if (double.IsFinite(rect.Right) &&
                double.IsFinite(rect.Top))
            {
                return new Point(_textPoint.X + rect.Left, _textPoint.Y + rect.Top);
            }
            else
            {
                return new Point(_textPoint.X, _textPoint.Y);
            }
        }

        protected override double GetCaretHeight()
        {
            return _renderer.FontSize;
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TextEditArea),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty WrappingProperty =
            DependencyProperty.Register("Wrapping", typeof(bool), typeof(TextEditArea),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty VerticalContentAlignmentProperty =
            DependencyProperty.Register("VerticalContentAlignment", typeof(VerticalAlignment), typeof(TextEditArea),
                new FrameworkPropertyMetadata(VerticalAlignment.Center, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HorizontalContentAlignmentProperty =
            DependencyProperty.Register("HorizontalContentAlignment", typeof(HorizontalAlignment), typeof(TextEditArea),
                new FrameworkPropertyMetadata(HorizontalAlignment.Left, FrameworkPropertyMetadataOptions.AffectsRender));
    }
}
