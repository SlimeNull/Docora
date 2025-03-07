using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Docora.Models
{
    public record struct MarkdownTextRangePropertyAndValue(DependencyProperty Property, object? Value, object? NormalValue);
}
