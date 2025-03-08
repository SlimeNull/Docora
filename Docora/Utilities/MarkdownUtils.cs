using Docora.Models;
using System.Windows.Documents;
using System.Windows;

namespace Docora.Utilities
{
    public static class MarkdownUtils
    {
        public static void UpdateTextRun(Run flowRun, Documents.TextRun run, MarkdownConfig config)
        {
            flowRun.Text = run.Content;
            flowRun.FontSize = config.FontSize;
            flowRun.FontWeight = run.Styles.Bold ? FontWeights.Bold : FontWeights.Normal;
            flowRun.FontStyle = run.Styles.Italic ? FontStyles.Italic : FontStyles.Normal;
            flowRun.TextDecorations = run.Styles.Strikethrough ? TextDecorations.Strikethrough : null;
        }

        public static void UpdateParagraph(Paragraph flowParagraph, Documents.Paragraph paragraph, MarkdownConfig config)
        {
            var inline = flowParagraph.Inlines.FirstInline;
            int inlineIndex = 0;

            while (inlineIndex < paragraph.Inlines.Count)
            {
                if (paragraph.Inlines[inlineIndex] is Documents.TextRun textRun)
                {
                    if (inline is Run flowRun)
                    {
                        inline = inline.NextInline;
                    }
                    else
                    {
                        var newFlowRun = new Run();
                        flowRun = newFlowRun;

                        if (inline is not null)
                        {
                            flowParagraph.Inlines.InsertAfter(inline, newFlowRun);
                        }
                        else
                        {
                            flowParagraph.Inlines.Add(newFlowRun);
                        }
                    }

                    UpdateTextRun(flowRun, textRun, config);
                }
                else
                {
                    break;
                }

                inlineIndex++;
            }

            while (flowParagraph.Inlines.Count > paragraph.Inlines.Count)
            {
                flowParagraph.Inlines.Remove(flowParagraph.Inlines.LastInline);
            }
        }

        public static void UpdateHeader(Paragraph flowParagraph, Documents.Header header, MarkdownConfig config)
        {
            while (flowParagraph.Inlines.Count > 1)
            {
                flowParagraph.Inlines.Remove(flowParagraph.Inlines.LastInline);
            }

            if (flowParagraph.Inlines.FirstInline is not Run flowRun)
            {
                if (flowParagraph.Inlines.FirstInline is not null)
                {
                    flowParagraph.Inlines.Remove(flowParagraph.Inlines.FirstInline);
                }

                var newFlowRun = new Run();
                flowParagraph.Inlines.Add(newFlowRun);

                flowRun = newFlowRun;
            }

            flowRun.Text = header.Content;

            flowRun.FontSize = header.Level switch
            {
                1 => config.Heading1FontSize,
                2 => config.Heading2FontSize,
                3 => config.Heading3FontSize,
                4 => config.Heading4FontSize,
                5 => config.Heading5FontSize,
                _ => config.Heading6FontSize,
            };

            flowRun.FontWeight = header.Level switch
            {
                1 => config.Heading1FontWeight,
                2 => config.Heading2FontWeight,
                3 => config.Heading3FontWeight,
                4 => config.Heading4FontWeight,
                5 => config.Heading5FontWeight,
                _ => config.Heading6FontWeight,
            };

            flowRun.FontStyle = FontStyles.Normal;
            flowRun.TextDecorations = null;
        }

        public static void UpdateDocument(FlowDocument flowDocument, Documents.MarkdownDocument document, MarkdownConfig config)
        {
            var block = flowDocument.Blocks.FirstBlock;
            int blockIndex = 0;

            while (blockIndex < document.Blocks.Count)
            {
                if (document.Blocks[blockIndex] is Documents.Paragraph paragraph)
                {
                    if (block is Paragraph flowParagraph)
                    {
                        block = block.NextBlock;
                    }
                    else
                    {
                        var newFlowParagraph = new Paragraph();
                        flowParagraph = newFlowParagraph;

                        if (block is not null)
                        {
                            flowDocument.Blocks.InsertAfter(block, newFlowParagraph);
                        }
                        else
                        {
                            flowDocument.Blocks.Add(newFlowParagraph);
                        }
                    }

                    UpdateParagraph(flowParagraph, paragraph, config);
                }
                else if (document.Blocks[blockIndex] is Documents.Header header)
                {
                    if (block is Paragraph flowParagraph)
                    {
                        block = block.NextBlock;
                    }
                    else
                    {
                        var newFlowParagraph = new Paragraph();
                        flowParagraph = newFlowParagraph;

                        if (block is not null)
                        {
                            flowDocument.Blocks.InsertAfter(block, newFlowParagraph);
                        }
                        else
                        {
                            flowDocument.Blocks.Add(newFlowParagraph);
                        }
                    }

                    UpdateHeader(flowParagraph, header, config);
                }
                else
                {
                    break;
                }

                blockIndex++;
            }

            while (flowDocument.Blocks.Count > document.Blocks.Count)
            {
                flowDocument.Blocks.Remove(flowDocument.Blocks.LastBlock);
            }
        }

        public static FlowDocument CreateFlowDocument(Documents.MarkdownDocument document, MarkdownConfig config)
        {
            FlowDocument flowDocument = new FlowDocument();

            UpdateDocument(flowDocument, document, config);

            return flowDocument;
        }
    }
}
