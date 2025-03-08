using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docora.Documents;

namespace Docora.Parsing
{
    public static partial class MarkdownParser
    {

        private enum MarkdownContextKind
        {
            Bold,
            Italic,
            Strikethrough,
            Code,
            Link,
            LinkTitle,
            LinkTarget,
            List,
            ListItem,
            Reference,
            CodeBlock,
        }

        private record struct MarkdownContext(MarkdownContextKind Kind, string? Label);

        private static void Parse(MarkdownParseContext context, string text)
        {
            var stateMachine = new StateMachine(context);

            stateMachine.Start();
            foreach (var c in text)
            {
                stateMachine.Push(c);
            }
        }

        public static MarkdownDocument Parse(string text)
        {
            var parseContext = new MarkdownParseContext();
            Parse(parseContext, text);

            var document = new MarkdownDocument();

            foreach (var block in parseContext.Blocks)
            {
                document.Blocks.Add(block);
            }

            return document;
        }
    }
}
