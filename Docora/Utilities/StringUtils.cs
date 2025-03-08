using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Docora.Models;

namespace Docora.Utilities
{
    public static class StringUtils
    {
        public static int GetTextLength(int number)
        {
            return (int)Math.Ceiling(Math.Log10(number + 1));
        }

        public static IEnumerable<string> EnumerateLines(string text)
        {
            int startIndex = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r' ||
                    text[i] == '\n')
                {
                    if (startIndex == i)
                    {
                        continue;
                    }

                    yield return text.Substring(startIndex, i - startIndex);
                    startIndex = i + 1;
                }
            }

            if (startIndex < text.Length)
            {
                if (startIndex == 0)
                {
                    yield return text;
                }
                else
                {
                    yield return text.Substring(startIndex);
                }
            }
        }
    }
}
