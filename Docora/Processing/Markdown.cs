using Docora.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using static System.Net.Mime.MediaTypeNames;

namespace Docora.Processing
{
    public record struct FlowDocumentTextPredicateResult(bool Success, int MatchedLength);
    public record struct FindTextResult(TextPointer TextPointer, int TextLength);

    public delegate FlowDocumentTextPredicateResult FlowDocumentTextPredicate(string text, int index);

    public static class Markdown
    {
        public static IEnumerable<FindTextResult> FindText(this TextPointer start, FlowDocumentTextPredicate predicate, TextPointer end)
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

        public static void Prepare(this Paragraph paragraph, MarkdownConfig config)
        {
            var paragraphRange = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
            paragraphRange.ApplyPropertyValue(TextElement.FontSizeProperty, config.FontSize);
            paragraphRange.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
            paragraphRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            paragraphRange.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
        }

        public static void RemoveTailingLineBreak(this Paragraph paragraph)
        {
            if (paragraph.Inlines.LastInline is LineBreak lastAsLineBreak)
            {
                paragraph.Inlines.Remove(lastAsLineBreak);
            }
            else if (
                paragraph.Inlines.LastInline is Run lastAsRun &&
                string.IsNullOrEmpty(lastAsRun.Text) &&
                lastAsRun.PreviousInline is LineBreak lineBreakBeforeEmptyRun)
            {
                paragraph.Inlines.Remove(lastAsRun);
                paragraph.Inlines.Remove(lineBreakBeforeEmptyRun);
            }
        }

        public static bool ProcessHeading(this Paragraph paragraph, MarkdownConfig config)
        {
            if (paragraph.Inlines.FirstInline is not Run run)
            {
                return false;
            }

            int headingLevel = GetHeadingLevel(run.Text);
            if (headingLevel == 0)
            {
                return false;
            }

            double fontSize = headingLevel switch
            {
                1 => config.Heading1FontSize,
                2 => config.Heading2FontSize,
                3 => config.Heading3FontSize,
                4 => config.Heading4FontSize,
                5 => config.Heading5FontSize,
                6 => config.Heading6FontSize,
                _ => config.Heading6FontSize,
            };

            FontWeight fontWeight = headingLevel switch
            {
                1 => config.Heading1FontWeight,
                2 => config.Heading2FontWeight,
                3 => config.Heading3FontWeight,
                4 => config.Heading4FontWeight,
                5 => config.Heading5FontWeight,
                6 => config.Heading6FontWeight,
                _ => config.Heading6FontWeight,
            };

            var paragraphRange = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
            paragraphRange.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
            paragraphRange.ApplyPropertyValue(TextElement.FontWeightProperty, fontWeight);
            return true;
        }

        private static bool ProcessForTagPairs(this Paragraph paragraph, FlowDocumentTextPredicate tagPredicate, params MarkdownTextRangePropertyAndValue[] propertyAndValues)
        {
            var paragraphRange = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
            bool anyProcessed = false;

            TextPointer? tagStart = null;
            foreach (var findResult in paragraphRange.Start.FindText(tagPredicate, paragraphRange.End))
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

        public static bool ProcessBold(this Paragraph paragraph)
        {
            return paragraph.ProcessForTagPairs(MatchBoldTag, new MarkdownTextRangePropertyAndValue(TextElement.FontWeightProperty, FontWeights.Bold, FontWeights.Normal));
        }

        public static bool ProcessItalic(this Paragraph paragraph)
        {
            return paragraph.ProcessForTagPairs(MatchItalicTag, new MarkdownTextRangePropertyAndValue(TextElement.FontStyleProperty, FontStyles.Italic, FontStyles.Normal));
        }

        public static bool ProcessStrikethrough(this Paragraph paragraph)
        {
            return paragraph.ProcessForTagPairs(MatchStrikethroughTag, new MarkdownTextRangePropertyAndValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough, null));
        }

        public static bool ProcessSuperscript(this Paragraph paragraph, MarkdownConfig config)
        {
            return paragraph.ProcessForTagPairs(MatchStrikethroughTag, 
                new MarkdownTextRangePropertyAndValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript, BaselineAlignment.Baseline),
                new MarkdownTextRangePropertyAndValue(TextElement.FontSizeProperty, config.SuperscriptFontSize, config.FontSize));
        }

        public static bool ProcessSubscript(this Paragraph paragraph, MarkdownConfig config)
        {
            return paragraph.ProcessForTagPairs(MatchStrikethroughTag, 
                new MarkdownTextRangePropertyAndValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Subscript, BaselineAlignment.Baseline),
                new MarkdownTextRangePropertyAndValue(TextElement.FontSizeProperty, config.SubscriptFontSize, config.FontSize));
        }
    }
}
