using Docora.Models;
using System.Windows.Documents;
using System.Windows;

namespace Docora.Utilities
{
    public static class MarkdownUtils
    {
        public static FlowDocument CreateFlowDocument(Documents.MarkdownDocument markdownDocument, MarkdownConfig config)
        {
            FlowDocument flowDocument = new FlowDocument();

            foreach (var block in markdownDocument.Blocks)
            {
                var flowBlock = default(Block);

                if (block is Documents.Paragraph paragraph)
                {
                    var flowParagraph = new Paragraph();

                    foreach (var inline in paragraph.Inlines)
                    {
                        var flowInline = default(Inline);

                        if (inline is Documents.TextRun textRun)
                        {
                            flowInline = new Run(textRun.Content)
                            {
                                FontWeight = textRun.Styles.Bold ? FontWeights.Bold : FontWeights.Normal,
                                FontStyle = textRun.Styles.Italic ? FontStyles.Italic : FontStyles.Normal,
                                TextDecorations = textRun.Styles.Strikethrough ? TextDecorations.Strikethrough : null
                            };
                        }

                        flowParagraph.Inlines.Add(flowInline);
                    }

                    flowBlock = flowParagraph;
                }
                else if (block is Documents.Header header)
                {
                    flowBlock = new Paragraph(
                        new Run(header.Content)
                        {
                            FontSize = header.Level switch
                            {
                                1 => config.Heading1FontSize,
                                2 => config.Heading2FontSize,
                                3 => config.Heading3FontSize,
                                4 => config.Heading4FontSize,
                                5 => config.Heading5FontSize,
                                _ => config.Heading6FontSize,
                            },
                            FontWeight = header.Level switch
                            {
                                1 => config.Heading1FontWeight,
                                2 => config.Heading2FontWeight,
                                3 => config.Heading3FontWeight,
                                4 => config.Heading4FontWeight,
                                5 => config.Heading5FontWeight,
                                _ => config.Heading6FontWeight,
                            }
                        });
                }

                if (flowBlock is not null)
                {
                    flowDocument.Blocks.Add(flowBlock);
                }
            }

            return flowDocument;
        }
    }
}
