using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        private static void ProcessParagraph(Paragraph paragraph, MarkdownConfig config)
        {
            paragraph.Prepare(config);

            var headingProcessed =
            paragraph.ProcessHeading(config);

            if (!headingProcessed)
            {
                paragraph.ProcessBold();
                paragraph.ProcessItalic();
                paragraph.ProcessStrikethrough();
                paragraph.ProcessSuperscript(config);
                paragraph.ProcessSubscript(config);
            }
        }

        private void rtb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _userChanging = true;
            _lastOperatingParagraph = rtb.CaretPosition.Paragraph;
        }

        private void rtb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_userChanging)
            {
                return;
            }

            _userChanging = false;

            if (e.Changes.LastOrDefault() is not { } lastChange)
            {
                return;
            }

            var currentParagraph = rtb.CaretPosition.Paragraph;

            if (_lastOperatingParagraph is not null &&
                _lastOperatingParagraph != currentParagraph)
            {
                ProcessParagraph(_lastOperatingParagraph, _config);
            }

            if (currentParagraph is Paragraph paragraph)
            {
                ProcessParagraph(paragraph, _config);
                rtb.ProcessList(_config);

            }

            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
