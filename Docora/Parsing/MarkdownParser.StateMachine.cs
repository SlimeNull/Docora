using System.Text;
using Docora.Documents;

namespace Docora.Parsing
{
    public static partial class MarkdownParser
    {

        private class StateMachine
        {
            public bool InCR { get; set; }
            public bool InLineStart { get; set; }
            public bool IsEscaping { get; set; }

            public bool ReadyForBlock { get; set; }
            public bool InLineStartPoundSign { get; set; }
            public bool InLineStartListStart { get; set; }
            public bool InLineStartCodeBlockStart { get; set; }
            public bool InHeader { get; set; }
            public bool InCodeBlock { get; set; }
            public bool InList { get; set; }

            // tags
            public bool InBacktick { get; set; }
            public bool InAsterisk { get; set; }
            public bool InUnderscore { get; set; }
            public bool InTilde { get; set; }
            public bool InCurlyBraces { get; set; }
            public bool InBrackets { get; set; }
            public bool InParentheses { get; set; }
            public bool InPoundSign { get; set; }
            public bool InPlusSign { get; set; }
            public bool InMinusSign { get; set; }
            public bool InDot { get; set; }
            public bool InExclamationMark { get; set; }
            public bool InPipe { get; set; }
            public bool InNumber { get; set; }
            public bool InSpace { get; set; }

            public int TagSize { get; set; }
            public int HeaderLevel { get; set; }
            public string? CodeBlockLanguage { get; set; }

            public TextRunStyles TextRunStyles { get; } = new TextRunStyles();
            public StringBuilder CachedContent { get; } = new StringBuilder();
            public Stack<string> HtmlTagStack { get; } = new Stack<string>();
            public MarkdownParseContext Context { get; }

            public StateMachine(MarkdownParseContext context)
            {
                Context = context;
            }

            public void Start()
            {
                ReadyForBlock = true;
            }

            public void Push(int c)
            {
                if (InCR && c == '\n')
                {
                    return;
                }

                InCR = false;
                if (c == '\r')
                {
                    InCR = true;
                    c = '\n';
                }

                try
                {
                    bool inParagraph = false;
                    if (ReadyForBlock)
                    {
                        ReadyForBlock = false;

                        if (c == '\n')
                        {
                            ResetStates();
                            Context.CloseBlock();

                            ReadyForBlock = true;
                        }
                        else if (c == '#')
                        {
                            InLineStartPoundSign = true;
                            TagSize = 1;
                        }
                        else if (c == '`')
                        {
                            InLineStartCodeBlockStart = true;
                            TagSize = 1;
                        }
                        else if (char.IsNumber((char)c))
                        {
                            InLineStartListStart = true;
                            CachedContent.Append((char)c);
                        }
                        else
                        {
                            inParagraph = true;
                        }
                    }
                    else if (InLineStartPoundSign)
                    {
                        if (c == -1)
                        {
                            var poundSignCount = TagSize;

                            InLineStartPoundSign = false;
                            TagSize = 0;

                            var textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                            for (int i = 0; i < poundSignCount; i++)
                            {
                                textRun.Append('#');
                            }
                        }
                        else if (c == '\n')
                        {
                            var poundSignCount = TagSize;

                            InLineStartPoundSign = false;
                            TagSize = 0;

                            var textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                            for (int i = 0; i < poundSignCount; i++)
                            {
                                textRun.Append('#');
                            }

                            textRun.Append(' ');
                        }
                        else if (c == '#')
                        {
                            TagSize++;
                        }
                        else if (c == ' ')
                        {
                            HeaderLevel = TagSize;

                            InLineStartPoundSign = false;
                            TagSize = 0;

                            InHeader = true;

                            Context.EnsureHeader(HeaderLevel);
                        }
                        else
                        {
                            // save some info
                            var poundSignCount = TagSize;

                            // clear old state
                            InLineStartPoundSign = false;
                            TagSize = 0;

                            // create paragraph and text run
                            var textRun = Context.EnsureParagraphTextRun(TextRunStyles);

                            for (int i = 0; i < poundSignCount; i++)
                            {
                                textRun.Append('#');
                            }

                            textRun.Append((char)c);
                        }
                    }
                    else if (InLineStartCodeBlockStart)
                    {
                        // 还在 ` 中, 累计 `
                        if (TagSize > 0)
                        {
                            if (c == -1)
                            {
                                var poundSignCount = TagSize;

                                InLineStartPoundSign = false;
                                TagSize = 0;

                                var textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                                for (int i = 0; i < poundSignCount; i++)
                                {
                                    textRun.Append('`');
                                }
                            }
                            else if (c == '\n')
                            {
                                var poundSignCount = TagSize;

                                InLineStartPoundSign = false;
                                TagSize = 0;

                                var textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                                for (int i = 0; i < poundSignCount; i++)
                                {
                                    textRun.Append('`');
                                }

                                textRun.Append(' ');
                            }
                            else if (c == '`')
                            {
                                TagSize++;

                                if (TagSize > 3)
                                {
                                    var backtickCount = TagSize;

                                    InLineStartCodeBlockStart = false;
                                    TagSize = 0;

                                    var textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                                    for (int i = 0; i < backtickCount; i++)
                                    {
                                        textRun.Append('`');
                                    }
                                }
                            }
                            else
                            {
                                if (TagSize != 3)
                                {
                                    var poundSignCount = TagSize;

                                    InLineStartPoundSign = false;
                                    TagSize = 0;

                                    var textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                                    for (int i = 0; i < poundSignCount; i++)
                                    {
                                        textRun.Append('`');
                                    }

                                    inParagraph = true;
                                }
                                else
                                {
                                    TagSize = 0;
                                    CachedContent.Append((char)c);
                                }
                            }
                        }
                        else
                        {
                            if (c == -1 ||
                                c == '\n')
                            {
                                CodeBlockLanguage = CachedContent.ToString();
                                CachedContent.Clear();

                                InLineStartCodeBlockStart = false;
                                InCodeBlock = true;
                            }
                            else
                            {
                                CachedContent.Append((char)c);
                            }
                        }
                    }
                    else if (InLineStartListStart)
                    {

                    }
                    else if (InHeader)
                    {
                        var header = Context.EnsureHeader(HeaderLevel);

                        if (c == -1 ||
                            c == '\n')
                        {
                            InHeader = false;
                            ReadyForBlock = true;

                            Context.CloseBlock();
                        }
                        else
                        {
                            header.Append((char)c);
                        }
                    }
                    else if (InCodeBlock)
                    {
                        var codeBlock = Context.EnsureCodeBlock(CodeBlockLanguage);

                        if (InBacktick)
                        {
                            if (c == -1)
                            {
                                if (TagSize < 3)
                                {
                                    var backtickCount = TagSize;

                                    InBacktick = false;
                                    TagSize = 0;

                                    for (int i = 0; i < TagSize; i++)
                                    {
                                        codeBlock.Append('`');
                                    }
                                }
                                else
                                {
                                    var backtickCount = TagSize;

                                    InBacktick = false;
                                    TagSize = 0;
                                    InCodeBlock = false;

                                    var textRun = Context.EnsureParagraphTextRun(TextRunStyles);

                                    for (int i = 3; i < backtickCount; i++)
                                    {
                                        textRun.Append('`');
                                    }
                                }
                            }
                            else if (c == '\n')
                            {
                                if (TagSize < 3)
                                {
                                    var backtickCount = TagSize;

                                    InBacktick = false;
                                    TagSize = 0;

                                    for (int i = 0; i < TagSize; i++)
                                    {
                                        codeBlock.Append('`');
                                    }

                                    codeBlock.Append('\n');
                                }
                                else
                                {
                                    var backtickCount = TagSize;

                                    InBacktick = false;
                                    TagSize = 0;
                                    InCodeBlock = false;

                                    var textRun = Context.EnsureParagraphTextRun(TextRunStyles);

                                    for (int i = 3; i < backtickCount; i++)
                                    {
                                        textRun.Append('`');
                                    }
                                }
                            }
                            else if (c == '`')
                            {
                                TagSize++;
                            }
                            else
                            {
                                var backtickCount = TagSize;

                                InBacktick = false;
                                TagSize = 0;

                                for (int i = 0; i < TagSize; i++)
                                {
                                    codeBlock.Append('`');
                                }

                                codeBlock.Append((char)c);
                            }
                        }
                        else if (InLineStart)
                        {
                            if (c == '`')
                            {
                                InBacktick = true;
                                TagSize = 1;
                            }
                            else if (c == '\n')
                            {
                                for (int i = 0; i < CachedContent.Length; i++)
                                {
                                    codeBlock.Append(CachedContent[0]);
                                }

                                CachedContent.Clear();
                                CachedContent.Append('\n');
                            }
                            else
                            {
                                for (int i = 0; i < CachedContent.Length; i++)
                                {
                                    codeBlock.Append(CachedContent[0]);
                                }

                                CachedContent.Clear();
                                codeBlock.Append((char)c);
                            }
                        }
                        else
                        {
                            if (c == '\n')
                            {
                                CachedContent.Append('\n');
                            }
                            else
                            {
                                codeBlock.Append((char)c);
                            }
                        }
                    }
                    else
                    {
                        inParagraph = true;
                    }

                    while (inParagraph)
                    {
                        inParagraph = false;

                        var textRun = Context.EnsureParagraphTextRun(TextRunStyles);

                        // space and return
                        if (InSpace)
                        {
                            if (c == '\n')
                            {
                                var spaceCount = TagSize;

                                InSpace = false;
                                TagSize = 0;

                                if (spaceCount >= 2)
                                {
                                    textRun.Append('\n');
                                }
                                else
                                {
                                    textRun.Append(' ');
                                }

                                return;
                            }
                            else if (c == ' ')
                            {
                                TagSize++;
                                return;
                            }
                            else
                            {
                                var spaceCount = TagSize;

                                InSpace = false;
                                TagSize = 0;

                                for (int i = 0; i < spaceCount; i++)
                                {
                                    textRun.Append(' ');
                                }
                            }
                        }

                        if (IsEscaping)
                        {
                            textRun.Append((char)c);
                        }
                        else if (InBacktick)
                        {
                            if (c == '\n')
                            {
                                InBacktick = false;
                                TagSize = 0;

                                ReadyForBlock = true;
                            }
                            else if (c == '`')
                            {
                                InBacktick = false;
                                TagSize = 0;

                                textRun.Append('`');
                                textRun.Append('`');
                            }
                            else
                            {
                                InBacktick = false;
                                TagSize = 0;

                                TextRunStyles.Toggle(TextRunStyle.InlineCode);

                                textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                                inParagraph = true;
                            }
                        }
                        else if (InAsterisk)
                        {
                            if (c == '\n')
                            {
                                InAsterisk = false;
                                TagSize = 0;

                                ReadyForBlock = true;
                            }
                            else if (c == '*')
                            {
                                TagSize++;
                            }
                            else
                            {
                                if (TagSize >= 2)
                                {
                                    TextRunStyles.Toggle(TextRunStyle.BoldWithAsterisk);
                                    TagSize -= 2;
                                }

                                if (TagSize >= 1)
                                {
                                    TextRunStyles.Toggle(TextRunStyle.ItalicWithAsterisk);
                                    TagSize -= 1;
                                }

                                InAsterisk = TagSize > 0;

                                textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                                inParagraph = true;
                            }
                        }
                        else if (InUnderscore)
                        {
                            if (c == '\n')
                            {
                                InUnderscore = false;
                                TagSize = 0;

                                ReadyForBlock = true;
                            }
                            else if (c == '_')
                            {
                                TagSize++;
                            }
                            else
                            {
                                if (TagSize >= 2)
                                {
                                    TextRunStyles.Toggle(TextRunStyle.BoldWithUnderscore);
                                    TagSize -= 2;
                                }

                                if (TagSize >= 1)
                                {
                                    TextRunStyles.Toggle(TextRunStyle.ItalicWithUnderscore);
                                    TagSize -= 1;
                                }

                                InUnderscore = TagSize > 0;

                                textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                                inParagraph = true;
                            }
                        }
                        else if (InTilde)
                        {
                            if (c == '\n')
                            {
                                InTilde = false;
                                TagSize = 0;

                                ReadyForBlock = true;
                            }
                            else if (c == '~')
                            {
                                TagSize++;
                            }
                            else
                            {
                                if (TagSize == 2)
                                {
                                    InTilde = false;
                                    TagSize = 0;

                                    TextRunStyles.Toggle(TextRunStyle.Strikethrough);

                                    textRun = Context.EnsureParagraphTextRun(TextRunStyles);
                                    inParagraph = true;
                                }
                                else
                                {
                                    var tildeCount = TagSize;

                                    InTilde = false;
                                    TagSize = 0;

                                    for (int i = 0; i < tildeCount; i++)
                                    {
                                        textRun.Append('~');
                                    }

                                    inParagraph = true;
                                }
                            }
                        }
                        else
                        {
                            if (c == '\n')
                            {
                                textRun.Append(' ');
                                ReadyForBlock = true;
                            }
                            else if (c == '`')
                            {
                                InBacktick = true;
                                TagSize = 1;
                            }
                            else if (c == '*')
                            {
                                InAsterisk = true;
                                TagSize = 1;
                            }
                            else if (c == '_')
                            {
                                InUnderscore = true;
                                TagSize = 1;
                            }
                            else if (c == '~')
                            {
                                InTilde = true;
                                TagSize = 1;
                            }
                            else if (c == '!')
                            {
                                InExclamationMark = true;
                                TagSize = 1;
                            }
                            else if (c == ' ')
                            {
                                InSpace = true;
                                TagSize = 1;
                            }
                            else if (c == '\\')
                            {
                                IsEscaping = true;
                            }
                            else
                            {
                                textRun.Append((char)c);
                            }
                        }
                    }
                }
                finally
                {
                    InLineStart = c == '\n';
                }
            }

            public void ResetStates()
            {
                InCR = false;
                IsEscaping = false;
                ReadyForBlock = false;
                InLineStartPoundSign = false;
                InLineStartListStart = false;
                InHeader = false;
                InList = false;
                InBacktick = false;
                InAsterisk = false;
                InUnderscore = false;
                InCurlyBraces = false;
                InBrackets = false;
                InParentheses = false;
                InPoundSign = false;
                InPlusSign = false;
                InMinusSign = false;
                InDot = false;
                InExclamationMark = false;
                InPipe = false;
                InNumber = false;

                TextRunStyles.Clear();
            }
        }
    }
}
