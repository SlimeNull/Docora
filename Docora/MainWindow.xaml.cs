using Docora.Models;
using Docora.Processing;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Docora;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _userChanging = false;
    private Paragraph? _lastOperatingParagraph;


    private TextPointer? _testTextPointer;

    public MainWindow()
    {
        InitializeComponent();
    }

    private static void RenderParagraph(Paragraph paragraph)
    {
        var config = MarkdownConfig.Default;

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
            RenderParagraph(_lastOperatingParagraph);
        }

        if (currentParagraph is Paragraph paragraph)
        {
            RenderParagraph(paragraph);
        }

        tb.Text = XamlWriter.Save(rtb.Document);
    }
}