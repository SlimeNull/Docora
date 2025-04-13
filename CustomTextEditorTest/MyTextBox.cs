using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace CustomTextEditorTest
{
    public class MyTextBox : TextBox
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Debug.WriteLine($"Text Box Render {DateTime.Now}");
        }
    }
}
