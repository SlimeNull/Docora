using Docora.Documents;

namespace Docora.Parsing
{
    public class MarkdownParseContext
    {
        private bool _blockClosed = false;

        public BlockCollection Blocks { get; } = new BlockCollection();

        public Paragraph EnsureParagraph()
        {
            if (!_blockClosed &&
                Blocks.Last is Paragraph currentParagraph)
            {
                return currentParagraph;
            }

            _blockClosed = false;
            var newParagraph = new Paragraph();
            Blocks.Add(newParagraph);
            return newParagraph;
        }

        public Header EnsureHeader(int level)
        {
            if (!_blockClosed &&
                Blocks.Last is Header currentHeader &&
                currentHeader.Level == level)
            {
                return currentHeader;
            }

            _blockClosed = false;
            var newHeader = new Header()
            {
                Level = level,
            };

            Blocks.Add(newHeader);
            return newHeader;
        }

        public TextRun EnsureParagraphTextRun(TextRunStyles styles)
        {
            var paragraph = EnsureParagraph();
            if (paragraph.Inlines.Last is TextRun currentTextRun)
            {
                if (currentTextRun.Styles == styles)
                {
                    return currentTextRun;
                }
                else if (string.IsNullOrEmpty(currentTextRun.Content))
                {
                    currentTextRun.Styles.Clear();
                    foreach (var style in styles)
                    {
                        currentTextRun.Styles.Add(style);
                    }

                    return currentTextRun;
                }
            }

            var newTextRun = new TextRun();
            newTextRun.Styles.CopyFrom(styles);
            paragraph.Inlines.Add(newTextRun);
            return newTextRun;
        }

        public void CloseBlock()
        {
            _blockClosed = true;
        }
    }
}
