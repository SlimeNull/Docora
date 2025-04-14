using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LibMarkdownEditor
{
    public class TextEditArea : ImeEditArea
    {
        private readonly TextBlock _renderer;
        private readonly StringBuilder _textBuilder;

        private Point _textPoint;
        private int _editIndex;
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

            _editIndex = _renderer.ContentStart
                .GetInsertionPosition(System.Windows.Documents.LogicalDirection.Forward)
                .GetOffsetToPosition(textPointer);

            InvalidateVisual();

            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                e.Handled = true;

                if (_editIndex > 0)
                {
                    _editIndex--;
                    _textBuilder.Remove(_editIndex, 1);

                    Text = _textBuilder.ToString();
                }
            }
            else if (e.Key == Key.Left)
            {
                e.Handled = true;

                if (_editIndex > 0)
                {
                    _editIndex--;
                    InvalidateVisual();
                }
            }
            else if (e.Key == Key.Right)
            {
                e.Handled = true;

                if (_editIndex < _textBuilder.Length)
                {
                    _editIndex++;
                    InvalidateVisual();
                }
            }
            else if (e.Key == Key.Home)
            {
                e.Handled = true;
                _editIndex = 0;
                InvalidateVisual();
            }
            else if (e.Key == Key.End)
            {
                e.Handled = true;
                _editIndex = _textBuilder.Length;
                InvalidateVisual();
            }

            base.OnKeyDown(e);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (_editIndex < 0)
            {
                _editIndex = 0;
            }
            else if (_editIndex > _textBuilder.Length)
            {
                _editIndex = _textBuilder.Length;
            }

            if (!_overwriteMode)
            {
                _textBuilder.Insert(_editIndex, e.Text);
                _editIndex += e.Text.Length;
            }
            else
            {
                _textBuilder.Remove(_editIndex, Math.Min(e.Text.Length, _textBuilder.Length - _editIndex));
                _textBuilder.Insert(_editIndex, e.Text);
                _editIndex += e.Text.Length;
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

        protected override Point GetEditorLeftTop()
        {
            return default;
        }

        protected override Point GetCaretLeftTop()
        {
            var textPointer = _renderer.ContentStart
                .GetInsertionPosition(System.Windows.Documents.LogicalDirection.Forward)
                .GetPositionAtOffset(_editIndex, System.Windows.Documents.LogicalDirection.Forward);

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
