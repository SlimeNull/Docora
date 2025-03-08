using System.Text;
using Docora.Utilities;

namespace Docora.Documents
{
    public sealed class List : Block
    {
        public bool IsOrdered { get; set; }

        public override string Markdown
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (IsOrdered)
                {
                    int counter = 1;
                    foreach (var item in Items)
                    {
                        int textLength = 0;
                        foreach (var line in StringUtils.EnumerateLines(item.Markdown))
                        {
                            if (textLength == 0)
                            {
                                sb.Append(counter);
                                sb.Append(". ");
                                sb.Append(line);
                                textLength = StringUtils.GetTextLength(counter);
                            }
                            else
                            {
                                sb.Append(' ', textLength + 2);
                                sb.Append(line);
                            }

                            sb.Append('\n');
                        }

                        counter++;
                    }
                }
                else
                {
                    foreach (var item in Items)
                    {
                        bool isFirstLine = true;
                        foreach (var line in StringUtils.EnumerateLines(item.Markdown))
                        {
                            if (isFirstLine)
                            {
                                sb.Append("- ");
                                sb.Append(line);
                            }
                            else
                            {
                                sb.Append("  ");
                                sb.Append(line);
                            }

                            sb.Append('\n');
                        }
                    }
                }

                return sb.ToString();
            }
        }

        public ListItemCollection Items { get; } = new ListItemCollection();

        public List() { }
    }
}

