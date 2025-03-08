namespace Docora.Documents
{
    public abstract class Inline : IMarkdownElement
    {
        public abstract string Markdown { get; }
    }
}
