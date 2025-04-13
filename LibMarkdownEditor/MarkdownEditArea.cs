using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LibMarkdownEditor
{
    public class MarkdownEditArea : ImeEditArea
    {
        static MarkdownEditArea()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownEditArea), new FrameworkPropertyMetadata(typeof(MarkdownEditArea)));
        }

        protected override Point GetEditorLeftTop() => default;
        protected override Point GetCaretLeftTop() => throw new NotImplementedException();
    }
}
