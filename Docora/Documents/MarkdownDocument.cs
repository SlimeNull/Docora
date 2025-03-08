using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Docora.Documents
{
    public class MarkdownDocument : IMarkdownElement
    {
        public BlockCollection Blocks { get; } = new BlockCollection();

        public string Markdown
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (var block in Blocks)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append('\n');
                        sb.Append('\n');
                    }

                    sb.Append(block.Markdown);
                }

                return sb.ToString();
            }
        }
    }
}

