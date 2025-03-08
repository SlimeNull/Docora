using System.Text;

namespace Docora.Documents
{
    public sealed class CodeBlock : Block
    {
        private readonly StringBuilder _buffer = new StringBuilder();

        private int _contentVersion = 0;
        private int _cachedContentVersion = -1;
        private string? _cachedContent;

        public string? Language { get; set; }

        public bool IsContentEmpty => _buffer.Length == 0;

        public string Content
        {
            get
            {
                if (_cachedContent is null ||
                    _cachedContentVersion != _contentVersion)
                {
                    _cachedContent = _buffer.ToString();
                    _cachedContentVersion = _contentVersion;
                }

                return _cachedContent;
            }
            set
            {
                _buffer.Clear();
                _buffer.Append(value);

                _contentVersion++;
                _cachedContentVersion = _contentVersion;
                _cachedContent = value;
            }
        }

        public override string Markdown
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("```");
                sb.Append(Language);
                sb.Append('\n');

                if (!IsContentEmpty)
                {
                    sb.Append(Content);
                    sb.Append('\n');
                }

                sb.Append("```");

                return sb.ToString();
            }
        }

        public void Clear()
        {
            _buffer.Clear();
            _contentVersion++;
        }

        public void Append(char value)
        {
            _buffer.Append(value);
            _contentVersion++;
        }

        public void Append(string value)
        {
            _buffer.Append(value);
            _contentVersion++;
        }

        public void Insert(int index, char value)
        {
            _buffer.Insert(index, value);
            _contentVersion++;
        }

        public void Insert(int index, string value)
        {
            _buffer.Insert(index, value);
            _contentVersion++;
        }
    }
}

