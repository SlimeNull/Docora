using System.Collections.ObjectModel;

namespace Docora.Documents
{
    public class InlineCollection : Collection<Inline>
    {
        public Inline? First => Count > 0 ? this[0] : null;
        public Inline? Last => Count > 0 ? this[Count - 1] : null;
    }
}