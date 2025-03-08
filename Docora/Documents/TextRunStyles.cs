using System.Collections.ObjectModel;

namespace Docora.Documents
{
    public class TextRunStyles : Collection<TextRunStyle>, IEquatable<TextRunStyles>
    {
        protected override void InsertItem(int index, TextRunStyle item)
        {
            if (IndexOf(item) >= 0)
            {
                throw new InvalidOperationException("The same style has already added");
            }

            base.InsertItem(index, item);
        }

        public void CopyFrom(TextRunStyles other)
        {
            Clear();
            foreach (var style in other)
            {
                Add(style);
            }
        }

        public bool Bold =>
            Contains(TextRunStyle.BoldWithAsterisk) ||
            Contains(TextRunStyle.BoldWithUnderscore);

        public bool Italic =>
            Contains(TextRunStyle.ItalicWithAsterisk) ||
            Contains(TextRunStyle.ItalicWithUnderscore);

        public bool Strikethrough =>
            Contains(TextRunStyle.Strikethrough);

        public bool InlineCode =>
            Contains(TextRunStyle.InlineCode);

        public void Enable(TextRunStyle style)
        {
            if (IndexOf(style) < 0)
            {
                Add(style);
            }
        }

        public void Disable(TextRunStyle style)
        {
            var index = IndexOf(style);
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        public void Toggle(TextRunStyle style)
        {
            var index = IndexOf(style);
            if (index >= 0)
            {
                RemoveAt(index);
            }
            else
            {
                Add(style);
            }
        }

        public bool Equals(TextRunStyles? other)
        {
            if (other == null)
            {
                return false;
            }

            return Enumerable.SequenceEqual(this, other);
        }

        public override bool Equals(object? obj)
        {
            if (obj is TextRunStyles otherStyles)
            {
                return Equals(otherStyles);
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (Count == 0)
            {
                return 0;
            }

            var hashCode = this[0].GetHashCode();
            for (int i = 1; i < Count; i++)
            {
                hashCode ^= this[i].GetHashCode();
            }

            return hashCode;
        }

        public static bool operator ==(TextRunStyles? left, TextRunStyles? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }


        public static bool operator !=(TextRunStyles? left, TextRunStyles? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return !left.Equals(right);
        }
    }
}
