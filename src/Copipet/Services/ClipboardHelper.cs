using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Copipet.Services
{
    internal static class ClipboardHelper
    {
        private const int RetryTimes = 5;
        private const int RetryDelayMs = 100;
        private static readonly Regex NewLine = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled);

        /// <summary>
        /// テキストをクリップボードへコピーする。改行は CRLF に揃える。
        /// 他のアプリが使用中で失敗した場合は 100ms 間隔で 5 回まで再試行する。
        /// </summary>
        public static bool TrySetText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            string normalized = NewLine.Replace(text, "\r\n");
            var data = new DataObject();
            data.SetText(normalized, TextDataFormat.UnicodeText);

            try
            {
                Clipboard.SetDataObject(data, true, RetryTimes, RetryDelayMs);
                return true;
            }
            catch (ExternalException)
            {
                return false;
            }
            catch (ThreadStateException)
            {
                return false;
            }
        }
    }
}
