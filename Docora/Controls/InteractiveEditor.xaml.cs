using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Docora.Models;
using Docora.Processing;

namespace Docora.Controls
{
    /// <summary>
    /// Interaction logic for InteractiveEditor.xaml
    /// </summary>
    public partial class InteractiveEditor : UserControl
    {
        private bool _userChanging = false;
        private Paragraph? _lastOperatingParagraph;
        private Documents.MarkdownDocument? _cachedMarkdown;
        private MarkdownConfig _config = MarkdownConfig.Default;

        public InteractiveEditor()
        {
            InitializeComponent();
        }

        public Documents.MarkdownDocument Document => _cachedMarkdown ??= BuildMarkdown();
        public event EventHandler? DocumentChanged;

        private Documents.MarkdownDocument BuildMarkdown()
        {
            var result = new Documents.MarkdownDocument();

            foreach (var block in rtb.Document.Blocks)
            {

            }

            return result;
        }

        private void rtb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _userChanging = true;
            _lastOperatingParagraph = rtb.CaretPosition.Paragraph;

            if (e.Key == Key.Enter)
            {
                var caretPosition = rtb.CaretPosition;
                if (caretPosition.Paragraph is null)
                {
                    return;
                }

                if (!Keyboard.IsKeyDown(Key.LeftShift))
                {
                    if (caretPosition.Paragraph.Parent is ListItem listItem)
                    {
                        var newListItem = new ListItem();
                        var newListItemParagraph = new Paragraph();

                        newListItem.Blocks.Add(newListItemParagraph);
                        listItem.List.ListItems.InsertAfter(listItem, newListItem);

                        rtb.CaretPosition = newListItemParagraph.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
                    }
                    else
                    {
                        var newParagraph = new Paragraph();
                        caretPosition.Paragraph.SiblingBlocks.InsertAfter(caretPosition.Paragraph, newParagraph);

                        rtb.CaretPosition = newParagraph.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
                    }
                }
                else
                {
                    if (caretPosition.Paragraph.Parent is ListItem listItem)
                    {
                        var newParagraph = new Paragraph();
                        caretPosition.Paragraph.SiblingBlocks.InsertAfter(caretPosition.Paragraph, newParagraph);

                        rtb.CaretPosition = newParagraph.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
                    }
                    else
                    {
                        rtb.CaretPosition = rtb.CaretPosition.InsertLineBreak();
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                e.Handled =
                    MarkdownProcessing.ProcessHeaderDeleting(rtb, _config);
            }
        }

        private void rtb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_userChanging)
            {
                return;
            }

            _userChanging = false;
            var currentParagraph = rtb.CaretPosition.Paragraph;

            if (_lastOperatingParagraph is not null &&
                _lastOperatingParagraph != currentParagraph)
            {
                if (_lastOperatingParagraph.Inlines.FirstInline?.Tag is not MarkdownTag ||
                    _lastOperatingParagraph.Inlines.FirstInline?.Tag is MarkdownParagraphTag)
                {
                    MarkdownProcessing.ApplyParagraphStyle(_lastOperatingParagraph, _config);
                }
            }

            if (currentParagraph is Paragraph paragraph)
            {
                bool anyBlockCreated =
                    MarkdownProcessing.ProcessHeaderCreation(rtb, _config) ||
                    MarkdownProcessing.ProcessListCreation(rtb, _config);

                if (!anyBlockCreated)
                {
                    if (paragraph.Inlines.FirstInline?.Tag is not MarkdownTag ||
                        paragraph.Inlines.FirstInline?.Tag is MarkdownParagraphTag)
                    {
                        MarkdownProcessing.ApplyParagraphStyle(paragraph, _config);
                    }
                }
            }

            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class MarkdownTag
    {

    }

    public class MarkdownHeaderTag : MarkdownTag
    {
        public int Level { get; set; }
    }

    public class MarkdownParagraphTag : MarkdownTag
    {

    }
}
