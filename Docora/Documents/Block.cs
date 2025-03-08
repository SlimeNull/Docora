namespace Docora.Documents
{
    public abstract class Block : IMarkdownElement
    {
        public abstract string Markdown { get; }
    }
}

