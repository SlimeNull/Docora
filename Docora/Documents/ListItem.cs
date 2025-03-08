using System.Text;

namespace Docora.Documents
{
    public sealed class ListItem : Block
    {
        public bool CanCheck { get; set; }
        public bool IsChecked { get; set; }

        public override string Markdown
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (CanCheck)
                {
                    if (IsChecked)
                    {
                        sb.Append("[x] ");
                    }
                    else
                    {
                        sb.Append("[ ] ");
                    }
                }

                foreach (var inline in Inlines)
                {
                    sb.Append(inline);
                }

                return sb.ToString();
            }
        }

        public InlineCollection Inlines { get; } = new InlineCollection();

        public ListItem()
        {

        }
    }
}

