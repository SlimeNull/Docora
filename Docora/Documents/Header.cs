using System.Text;

namespace Docora.Documents
{
    public sealed class Header : Block
    {
        private readonly StringBuilder _buffer = new StringBuilder();

        private int _contentVersion = 0;
        private int _cachedContentVersion = -1;
        private string? _cachedContent;


        private int _level = 1;
        public int Level
        {
            get => _level;
            set
            {
                if (_level < 1 ||
                    _level > 6)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _level = value;
            }
        }

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
                StringBuilder sb = new StringBuilder(Content?.Length ?? 0 + Level + 1);

                sb.Append('#', _level);
                sb.Append(' ');
                sb.Append(Content);

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

