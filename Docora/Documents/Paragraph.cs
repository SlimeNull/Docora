namespace Docora.Documents
{
    public sealed class Paragraph : Block
    {
        public override string Markdown => string.Concat(Inlines.Select(inline => inline.Markdown));

        public InlineCollection Inlines { get; } = new InlineCollection();

        public Paragraph() { }
    }
}

