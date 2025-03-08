using System.Collections.ObjectModel;

namespace Docora.Documents
{
    public class BlockCollection : Collection<Block>
    {
        public Block? First => Count > 0 ? this[0] : null;
        public Block? Last => Count > 0 ? this[Count - 1] : null;
    }
}

