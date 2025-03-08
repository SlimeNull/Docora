using System.Text;
using Docora.Documents;

namespace Docora.Parsing
{
    public static partial class MarkdownParser
    {

        private class StateMachine
        {
            public bool InCR { get; set; }
            public bool IsEscaping { get; set; }

            public bool InLineStart { get; set; }
            public bool InLineStartPoundSign { get; set; }
            public bool InLineStartListStart { get; set; }
            public bool InHeader { get; set; }
            public bool InList { get; set; }

            // tags
            public bool InBacktick { get; set; }
            public bool InAsterisk { get; set; }
            public bool InUnderscore { get; set; }
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

            public int TagSize { get; set; }
            public int HeaderLevel { get; set; }

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
                InLineStart = true;
            }

            public void Push(char c)
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

                bool inParagraph = false;
                if (InLineStart)
                {
                    InLineStart = false;

                    if (c == '\n')
                    {
                        ResetStates();
                        Context.CloseBlock();

                        InLineStart = true;
                    }
                    else if (c == '#')
                    {
                        InLineStartPoundSign = true;
                        TagSize = 1;
                    }
                    else if (c == '\\')
                    {
                        IsEscaping = true;
                    }
                    else if (char.IsNumber(c))
                    {
                        InLineStartListStart = true;
                        CachedContent.Append(c);
                    }
                    else
                    {
                        inParagraph = true;
                    }
                }
                else if (InLineStartPoundSign)
                {
                    if (c == '#')
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

                        textRun.Append(c);
                    }
                }
                else if (InLineStartListStart)
                {

                }
                else if (InHeader)
                {
                    var header = Context.EnsureHeader(HeaderLevel);

                    if (c == '\n')
                    {
                        InHeader = false;
                        InLineStart = true;
                    }
                    else
                    {
                        header.Append(c);
                    }
                }
                else
                {
                    inParagraph = true;
                }

                if (inParagraph)
                {
                    var textRun = Context.EnsureParagraphTextRun(TextRunStyles);

                    if (IsEscaping)
                    {
                        textRun.Append(c);
                    }
                    else if (InBacktick)
                    {
                        if (c == '\n')
                        {
                            InBacktick = false;
                            TagSize = 0;

                            InLineStart = true;
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
                            textRun.Append(c);
                        }
                    }
                    else if (InAsterisk)
                    {
                        if (c == '\n')
                        {
                            InAsterisk = false;
                            TagSize = 0;

                            InLineStart = true;
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
                            textRun.Append(c);
                        }
                    }
                    else if (InUnderscore)
                    {
                        if (c == '\n')
                        {
                            InUnderscore = false;
                            TagSize = 0;

                            InLineStart = true;
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
                            textRun.Append(c);
                        }
                    }
                    else
                    {
                        if (c == '\n')
                        {
                            InLineStart = true;
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
                        else if (c == '!')
                        {
                            InExclamationMark = true;
                            TagSize = 1;
                        }
                        else
                        {
                            textRun.Append(c);
                        }
                    }
                }
            }

            public void ResetStates()
            {
                InCR = false;
                IsEscaping = false;
                InLineStart = false;
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
