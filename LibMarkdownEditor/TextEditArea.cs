using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LibMarkdownEditor
{
    public class TextEditArea : ImeEditArea
    {
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

        private FormattedText CreateFormattedText(double containerWidth, double containerHeight)
        {
            var result = new FormattedText(Text, CultureInfo.CurrentCulture, FlowDirection, FontFamily?.GetTypefaces()?.FirstOrDefault(), FontSize, Foreground, 1);

            result.SetFontStyle(FontStyle);
            result.SetFontWeight(FontWeight);

            if (Wrapping)
            {
                result.MaxTextWidth = containerWidth;
                result.MaxTextHeight = containerHeight;
            }

            return result;
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            Text += e.Text;

            base.OnTextInput(e);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var formattedText = CreateFormattedText(availableSize.Width, availableSize.Height);

            return new Size(
                Math.Min(availableSize.Width, formattedText.Width),
                Math.Min(availableSize.Height, formattedText.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;

            var verticalContentAlignment = VerticalContentAlignment;
            var horizontalContentAlignment = HorizontalContentAlignment;

            var formattedText = CreateFormattedText(ActualWidth, ActualHeight);
            var textPoint = new Point(0, 0);

            if (horizontalContentAlignment == HorizontalAlignment.Center)
            {
                textPoint.X = (actualWidth - formattedText.Width) / 2;
            }
            else if (horizontalContentAlignment == HorizontalAlignment.Right)
            {
                textPoint.X = actualWidth - formattedText.Width;
            }

            if (verticalContentAlignment == VerticalAlignment.Center)
            {
                textPoint.Y = (actualHeight - formattedText.Height) / 2;
            }
            else if (verticalContentAlignment == VerticalAlignment.Bottom)
            {
                textPoint.Y = actualHeight - formattedText.Height;
            }

            drawingContext.DrawText(formattedText, textPoint);
        }
        protected override Point GetEditorLeftTop()
        {
            return default;
        }

        protected override Point GetCaretLeftTop()
        {
            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;

            var verticalContentAlignment = VerticalContentAlignment;
            var horizontalContentAlignment = HorizontalContentAlignment;

            var formattedText = CreateFormattedText(ActualWidth, ActualHeight);
            var textWidth = formattedText.Width;
            var textHeight = formattedText.Height;

            if (textHeight == 0)
            {
                textHeight = FontSize;
            }
            
            var textPoint = new Point(0, 0);

            if (horizontalContentAlignment == HorizontalAlignment.Center)
            {
                textPoint.X = (actualWidth - textWidth) / 2;
            }
            else if (horizontalContentAlignment == HorizontalAlignment.Right)
            {
                textPoint.X = actualWidth - textWidth;
            }

            if (verticalContentAlignment == VerticalAlignment.Center)
            {
                textPoint.Y = (actualHeight - textHeight) / 2;
            }
            else if (verticalContentAlignment == VerticalAlignment.Bottom)
            {
                textPoint.Y = actualHeight - textHeight;
            }

            if (!string.IsNullOrEmpty(formattedText.Text))
            {
                var lastCharGeometry = formattedText.BuildHighlightGeometry(textPoint, formattedText.Text.Length - 1, 1);

                return new Point(
                    lastCharGeometry.Bounds.Right,
                    lastCharGeometry.Bounds.Top);
            }
            else
            {
                return textPoint;
            }
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
