using System.Text;

namespace Docora.Documents
{
    public sealed class TextRun : Inline
    {
        private readonly StringBuilder _buffer = new StringBuilder();

        private int _contentVersion = 0;
        private int _cachedContentVersion = -1;
        private string? _cachedContent;

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
        public TextRunStyles Styles { get; } = new TextRunStyles();

        public override string Markdown
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < Styles.Count; i++)
                {
                    TextRunStyle style = Styles[i];
                    sb.Append(style switch
                    {
                        TextRunStyle.BoldWithAsterisk => "**",
                        TextRunStyle.ItalicWithAsterisk => "*",
                        TextRunStyle.BoldWithUnderscore => "__",
                        TextRunStyle.ItalicWithUnderscore => "_",
                        TextRunStyle.Strikethrough => "~~",
                        TextRunStyle.InlineCode => "`",
                        _ => null
                    });
                }

                sb.Append(Content);

                for (int i = Styles.Count - 1; i >= 0; i--)
                {
                    TextRunStyle style = Styles[i];
                    sb.Append(style switch
                    {
                        TextRunStyle.BoldWithAsterisk => "**",
                        TextRunStyle.ItalicWithAsterisk => "*",
                        TextRunStyle.BoldWithUnderscore => "__",
                        TextRunStyle.ItalicWithUnderscore => "_",
                        TextRunStyle.Strikethrough => "~~",
                        TextRunStyle.InlineCode => "`",
                        _ => null
                    });
                }

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
