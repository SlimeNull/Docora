using System.Text;
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
using Docora.Parsing;
using Docora.Utilities;

namespace ParserText
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void input_TextChanged(object sender, TextChangedEventArgs e)
        {
            var document = MarkdownParser.Parse(input.Text);
            output.Text = document.Markdown;

            rendered.Document = MarkdownUtils.CreateFlowDocument(document, MarkdownConfig.Default);
        }
    }
}