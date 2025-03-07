using System.Windows;

namespace Docora.Models
{
    public record struct MarkdownConfig
    {
        public double FontSize { get; set; }

        public double Heading1FontSize { get; set; }
        public double Heading2FontSize { get; set; }
        public double Heading3FontSize { get; set; }
        public double Heading4FontSize { get; set; }
        public double Heading5FontSize { get; set; }
        public double Heading6FontSize { get; set; }
        public double SuperscriptFontSize { get; set; }
        public double SubscriptFontSize { get; set; }


        public FontWeight Heading1FontWeight { get; set; }
        public FontWeight Heading2FontWeight { get; set; }
        public FontWeight Heading3FontWeight { get; set; }
        public FontWeight Heading4FontWeight { get; set; }
        public FontWeight Heading5FontWeight { get; set; }
        public FontWeight Heading6FontWeight { get; set; }

        public static MarkdownConfig Default { get; } = new MarkdownConfig()
        {
            FontSize = 14,
            Heading1FontSize = 42,
            Heading2FontSize = 36,
            Heading3FontSize = 18,
            Heading4FontSize = 16,
            Heading5FontSize = 14,
            Heading6FontSize = 12,
            SuperscriptFontSize = 8,
            SubscriptFontSize = 8,
            Heading1FontWeight = FontWeights.Bold,
            Heading2FontWeight = FontWeights.Bold,
            Heading3FontWeight = FontWeights.Bold,
            Heading4FontWeight = FontWeights.Bold,
            Heading5FontWeight = FontWeights.Bold,
            Heading6FontWeight = FontWeights.Bold,
        };
    }
}
