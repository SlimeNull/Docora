using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Docora.Models;

namespace Docora.Controls
{
    public partial class InteractiveEditor
    {
        private static class MarkdownProcessing
        {
            public record struct FlowDocumentTextPredicateResult(bool Success, int MatchedLength);
            public record struct FindTextResult(TextPointer TextPointer, int TextLength);

            public delegate FlowDocumentTextPredicateResult FlowDocumentTextPredicate(string text, int index);


            public static IEnumerable<FindTextResult> FindText(TextPointer start, FlowDocumentTextPredicate predicate, TextPointer end)
            {
                TextPointer? current = start;

                while (
                    current is not null &&
                    current.CompareTo(end) <= 0)
                {
                    if (current.Parent is Run run)
                    {
                        int indexStart = 0;
                        FlowDocumentTextPredicateResult match;

                        while (indexStart < run.Text.Length)
                        {
                            match = predicate.Invoke(run.Text, indexStart);

                            if (match.Success)
                            {
                                yield return new FindTextResult(run.ContentStart.GetPositionAtOffset(indexStart), match.MatchedLength);
                                indexStart = indexStart + match.MatchedLength;
                            }
                            else
                            {
                                indexStart++;
                            }
                        }

                        current = run.NextInline?.ElementStart;
                    }
                    else
                    {
                        current = current.GetNextContextPosition(LogicalDirection.Forward);
                    }
                }
            }

            private static FlowDocumentTextPredicate CreateMatchLetterPredicate(char c, int count)
            {
                return (text, index) =>
                {
                    if (index + count > text.Length)
                    {
                        return new FlowDocumentTextPredicateResult(false, count);
                    }

                    bool match = true;
                    for (int i = 0; i < count; i++)
                    {
                        if (text[index + i] != c)
                        {
                            match = false;
                            break;
                        }
                    }

                    if (index - 1 >= 0 &&
                        text[index - 1] == c)
                    {
                        match = false;
                    }
                    else if (
                        index + count < text.Length &&
                        text[index + count] == c)
                    {
                        match = false;
                    }

                    return new FlowDocumentTextPredicateResult(match, count);
                };
            }

            private static FlowDocumentTextPredicate MatchItalicTag { get; } = CreateMatchLetterPredicate('*', 1);
            private static FlowDocumentTextPredicate MatchBoldTag { get; } = CreateMatchLetterPredicate('*', 2);
            private static FlowDocumentTextPredicate MatchStrikethroughTag { get; } = CreateMatchLetterPredicate('~', 2);

            private static int GetHeadingLevel(string text)
            {
                int level = 0;
                bool isInHeading = false;

                foreach (var c in text)
                {
                    if (!isInHeading)
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            continue;
                        }
                        else if (c == '#')
                        {
                            isInHeading = true;
                            level = 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (c == '#')
                        {
                            level++;
                        }
                        else
                        {
                            if (!char.IsWhiteSpace(c))
                            {
                                level = 0;
                            }

                            break;
                        }
                    }
                }

                return level;
            }

            private static void PrepareParagraph(Paragraph paragraph, MarkdownConfig config)
            {
                var paragraphRange = new TextRange(paragraph.ElementStart, paragraph.ElementEnd);
                paragraphRange.ApplyPropertyValue(TextElement.FontSizeProperty, config.FontSize);
                paragraphRange.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
                paragraphRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                paragraphRange.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
            }

            private static bool ProcessForTagPairs(Paragraph paragraph, FlowDocumentTextPredicate tagPredicate, params MarkdownTextRangePropertyAndValue[] propertyAndValues)
            {
                var paragraphRange = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                bool anyProcessed = false;

                TextPointer? tagStart = null;
                foreach (var findResult in FindText(paragraphRange.Start, tagPredicate, paragraphRange.End))
                {
                    if (tagStart is null)
                    {
                        tagStart = findResult.TextPointer;
                    }
                    else
                    {
                        var tagEnd = findResult.TextPointer.GetPositionAtOffset(findResult.TextLength);
                        var tagRange = new TextRange(tagStart, tagEnd);
                        var afterTagRange = new TextRange(tagEnd, tagEnd);

                        foreach (var propertyAndValue in propertyAndValues)
                        {
                            tagRange.ApplyPropertyValue(propertyAndValue.Property, propertyAndValue.Value);
                            afterTagRange.ApplyPropertyValue(propertyAndValue.Property, propertyAndValue.NormalValue);
                        }

                        tagStart = null;
                        anyProcessed = true;
                    }
                }

                return anyProcessed;
            }

            private static bool ProcessParagraphBold(Paragraph paragraph)
            {
                return ProcessForTagPairs(paragraph, MatchBoldTag, new MarkdownTextRangePropertyAndValue(TextElement.FontWeightProperty, FontWeights.Bold, FontWeights.Normal));
            }

            private static bool ProcessParagraphItalic(Paragraph paragraph)
            {
                return ProcessForTagPairs(paragraph, MatchItalicTag, new MarkdownTextRangePropertyAndValue(TextElement.FontStyleProperty, FontStyles.Italic, FontStyles.Normal));
            }

            private static bool ProcessParagraphStrikethrough(Paragraph paragraph)
            {
                return ProcessForTagPairs(paragraph, MatchStrikethroughTag, new MarkdownTextRangePropertyAndValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough, null));
            }

            private static bool ProcessParagraphSuperscript(Paragraph paragraph, MarkdownConfig config)
            {
                return ProcessForTagPairs(paragraph, MatchStrikethroughTag,
                    new MarkdownTextRangePropertyAndValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript, BaselineAlignment.Baseline),
                    new MarkdownTextRangePropertyAndValue(TextElement.FontSizeProperty, config.SuperscriptFontSize, config.FontSize));
            }

            private static bool ProcessParagraphSubscript(Paragraph paragraph, MarkdownConfig config)
            {
                return ProcessForTagPairs(paragraph, MatchStrikethroughTag,
                    new MarkdownTextRangePropertyAndValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Subscript, BaselineAlignment.Baseline),
                    new MarkdownTextRangePropertyAndValue(TextElement.FontSizeProperty, config.SubscriptFontSize, config.FontSize));
            }

            public static bool ProcessHeaderCreation(RichTextBox richTextBox, MarkdownConfig config)
            {
                var document = richTextBox.Document;
                var textPointer = richTextBox.CaretPosition;
                var paragraph = textPointer.Paragraph;
                if (paragraph is null)
                {
                    return false;
                }

                if (textPointer.Parent is Run run &&
                    paragraph.Inlines.FirstInline == run &&
                    paragraph.Inlines.LastInline == run)
                {
                    var offset = run.ContentStart.GetOffsetToPosition(textPointer);

                    if (run.Text.Length < 2)
                    {
                        return false;
                    }

                    if (run.Text[0] != '#')
                    {
                        return false;
                    }

                    int level = 1;
                    while (
                        level < run.Text.Length &&
                        run.Text[level] == '#')
                    {
                        level++;
                    }

                    if (level >= run.Text.Length ||
                        run.Text[level] != ' ')
                    {
                        return false;
                    }

                    run.Text = run.Text.Substring(level + 1);
                    run.Tag = new MarkdownHeaderTag()
                    {
                        Level = level,
                    };

                    ApplyHeaderStyle(paragraph, level, config);

                    richTextBox.CaretPosition = run.ContentStart
                        .GetPositionAtOffset(Math.Max(offset - level - 1, 0))
                        .GetInsertionPosition(LogicalDirection.Forward);

                    return true;
                }

                return false;
            }

            public static bool ProcessHeaderDeleting(RichTextBox richTextBox, MarkdownConfig config)
            {
                var document = richTextBox.Document;
                var textPointer = richTextBox.CaretPosition;
                var paragraph = textPointer.Paragraph;
                if (paragraph is null ||
                    paragraph.Inlines.FirstInline?.Tag is not MarkdownHeaderTag)
                {
                    return false;
                }

                if (textPointer.Parent is Run run &&
                    paragraph.Inlines.FirstInline == run &&
                    paragraph.Inlines.LastInline == run)
                {
                    var offset = run.ContentStart.GetOffsetToPosition(textPointer);

                    if (offset != 0)
                    {
                        return false;
                    }

                    paragraph.Tag = new MarkdownParagraphTag();
                    ApplyParagraphStyle(paragraph, config);

                    return true;
                }

                return false;
            }

            public static bool ProcessListCreation(RichTextBox richTextBox, MarkdownConfig config)
            {
                var document = richTextBox.Document;
                var textPointer = richTextBox.CaretPosition;
                var paragraph = textPointer.Paragraph;
                if (paragraph is null)
                {
                    return false;
                }

                if (textPointer.Parent is Run run &&
                    paragraph.Inlines.LastInline == run)
                {
                    var offset = run.ContentStart.GetOffsetToPosition(textPointer);

                    if (run.Text.Length < 3)
                    {
                        return false;
                    }

                    if (char.IsAsciiDigit(run.Text[0]))
                    {
                        int index = 1;
                        while (
                            index < run.Text.Length &&
                            char.IsAsciiDigit(run.Text[index]))
                        {
                            if (index > offset)
                            {
                                return false;
                            }

                            index++;
                        }

                        if (index > offset ||
                            index >= run.Text.Length ||
                            run.Text[index] != '.')
                        {
                            return false;
                        }

                        index++;
                        if (index > offset ||
                            index >= run.Text.Length ||
                            run.Text[index] != ' ')
                        {
                            return false;
                        }

                        List list = new List()
                        {
                            MarkerStyle = TextMarkerStyle.Decimal,
                        };

                        ListItem listItem = new ListItem();
                        list.ListItems.Add(listItem);

                        var afterRun = run.ElementEnd.InsertParagraphBreak();
                        document.Blocks.InsertAfter(paragraph, list);

                        if (run.PreviousInline is LineBreak previousLineBreak)
                        {
                            paragraph.Inlines.Remove(previousLineBreak);
                        }
                        paragraph.Inlines.Remove(run);

                        if (paragraph.Inlines.FirstInline is not Run paragraphFirstInline ||
                            string.IsNullOrEmpty(paragraphFirstInline.Text))
                        {
                            document.Blocks.Remove(paragraph);
                        }

                        richTextBox.CaretPosition = listItem.ContentStart;

                        return true;
                    }
                    else if (run.Text[0] == '-')
                    {
                        if (offset < 2 ||
                            run.Text.Length < 2 ||
                            run.Text[1] != ' ')
                        {
                            return false;
                        }

                        List list = new List()
                        {
                            MarkerStyle = TextMarkerStyle.Disc,
                        };

                        ListItem listItem = new ListItem();
                        list.ListItems.Add(listItem);

                        var afterRun = run.ElementEnd.InsertParagraphBreak();
                        document.Blocks.InsertAfter(paragraph, list);

                        if (run.PreviousInline is LineBreak previousLineBreak)
                        {
                            paragraph.Inlines.Remove(previousLineBreak);
                        }
                        paragraph.Inlines.Remove(run);

                        if (paragraph.Inlines.FirstInline is not Run paragraphFirstInline ||
                            string.IsNullOrEmpty(paragraphFirstInline.Text))
                        {
                            document.Blocks.Remove(paragraph);
                        }

                        richTextBox.CaretPosition = listItem.ContentStart;

                        return true;
                    }
                }

                return false;
            }

            public static void ApplyParagraphStyle(Paragraph paragraph, MarkdownConfig config)
            {
                PrepareParagraph(paragraph, config);
                ProcessParagraphBold(paragraph);
                ProcessParagraphItalic(paragraph);
                ProcessParagraphStrikethrough(paragraph);
            }

            public static void ApplyHeaderStyle(Paragraph headerParagraph, int level, MarkdownConfig config)
            {
                headerParagraph.FontSize = level switch
                {
                    1 => config.Heading1FontSize,
                    2 => config.Heading2FontSize,
                    3 => config.Heading3FontSize,
                    4 => config.Heading4FontSize,
                    5 => config.Heading5FontSize,
                    _ => config.Heading6FontSize,
                };

                headerParagraph.FontWeight = level switch
                {
                    1 => config.Heading1FontWeight,
                    2 => config.Heading2FontWeight,
                    3 => config.Heading3FontWeight,
                    4 => config.Heading4FontWeight,
                    5 => config.Heading5FontWeight,
                    _ => config.Heading6FontWeight,
                };

                headerParagraph.FontStyle = FontStyles.Normal;

                foreach (var run in headerParagraph.Inlines.OfType<Run>())
                {
                    run.FontSize = level switch
                    {
                        1 => config.Heading1FontSize,
                        2 => config.Heading2FontSize,
                        3 => config.Heading3FontSize,
                        4 => config.Heading4FontSize,
                        5 => config.Heading5FontSize,
                        _ => config.Heading6FontSize,
                    };

                    run.FontWeight = level switch
                    {
                        1 => config.Heading1FontWeight,
                        2 => config.Heading2FontWeight,
                        3 => config.Heading3FontWeight,
                        4 => config.Heading4FontWeight,
                        5 => config.Heading5FontWeight,
                        _ => config.Heading6FontWeight,
                    };

                    run.FontStyle = FontStyles.Normal;
                }
            }
        }

    }
}
