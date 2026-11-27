using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Copipet.Models;

namespace Copipet.Services
{
    internal sealed class LoadResult
    {
        public AppData Data { get; set; }

        /// <summary>起動時に利用者へ知らせる内容（なければ null）。</summary>
        public string Notice { get; set; }
    }

    /// <summary>
    /// %APPDATA%\Copipet\data.json の読み書き。
    /// 保存は「一時ファイルに書いてから差し替え」で行い、直前の版を data.json.bak に1世代残す。
    /// </summary>
    internal sealed class DataStore
    {
        private static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(typeof(AppData));

        public string DirectoryPath { get; private set; }
        public string FilePath { get; private set; }
        public string BackupPath { get; private set; }
        private string TempPath { get; set; }

        public DataStore()
            : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Copipet"))
        {
        }

        public DataStore(string directoryPath)
        {
            DirectoryPath = directoryPath;
            FilePath = Path.Combine(directoryPath, "data.json");
            BackupPath = FilePath + ".bak";
            TempPath = FilePath + ".tmp";
        }

        public LoadResult Load()
        {
            Directory.CreateDirectory(DirectoryPath);

            if (!File.Exists(FilePath))
            {
                // 本体がなくバックアップだけある場合（保存の途中で止まった等）はバックアップを使う
                AppData fromBackup = TryRead(BackupPath);
                AppData initial = fromBackup ?? AppData.CreateDefault();
                TrySaveSilently(initial);
                return new LoadResult
                {
                    Data = initial,
                    Notice = fromBackup != null ? "データファイルが見つからなかったため、1つ前の保存内容から復元しました。" : null
                };
            }

            try
            {
                return new LoadResult { Data = Read(FilePath) };
            }
            catch (Exception ex) when (IsReadFailure(ex))
            {
                string brokenPath = FilePath + ".broken_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string movedTo = null;
                try
                {
                    File.Move(FilePath, brokenPath);
                    movedTo = brokenPath;
                }
                catch (Exception moveEx) when (moveEx is IOException || moveEx is UnauthorizedAccessException)
                {
                    // 退避できなくても起動は続ける
                }

                AppData fromBackup = TryRead(BackupPath);
                AppData data = fromBackup ?? AppData.CreateDefault();
                TrySaveSilently(data);

                var notice = new StringBuilder();
                notice.AppendLine("データファイルが壊れていたため読み込めませんでした。");
                notice.AppendLine(fromBackup != null
                    ? "1つ前の保存内容から復元しました。"
                    : "空の状態で起動しました。");
                if (movedTo != null)
                {
                    notice.AppendLine();
                    notice.AppendLine("壊れたファイルは次の場所に退避しています：");
                    notice.Append(movedTo);
                }
                return new LoadResult { Data = data, Notice = notice.ToString() };
            }
        }

        public void Save(AppData data)
        {
            Directory.CreateDirectory(DirectoryPath);

            using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, new UTF8Encoding(false), false, true, "  "))
                {
                    Serializer.WriteObject(writer, data);
                    writer.Flush();
                }
                stream.Flush(true);
            }

            if (File.Exists(FilePath))
            {
                try
                {
                    File.Replace(TempPath, FilePath, BackupPath, true);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is PlatformNotSupportedException)
                {
                    // ネットワーク上のフォルダなど File.Replace が使えない環境向けの代替手順
                    File.Copy(FilePath, BackupPath, true);
                    File.Copy(TempPath, FilePath, true);
                    File.Delete(TempPath);
                }
            }
            else
            {
                File.Move(TempPath, FilePath);
            }
        }

        private void TrySaveSilently(AppData data)
        {
            try
            {
                Save(data);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 初回作成に失敗しても起動は続ける（次の保存時に再挑戦する）
            }
        }

        private static AppData Read(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var data = Serializer.ReadObject(stream) as AppData;
                if (data == null) throw new InvalidDataException("データが空です。");
                data.Normalize();
                return data;
            }
        }

        private static AppData TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                return Read(path);
            }
            catch (Exception ex) when (IsReadFailure(ex))
            {
                return null;
            }
        }

        private static bool IsReadFailure(Exception ex)
        {
            return ex is SerializationException
                || ex is InvalidDataException
                || ex is System.Xml.XmlException
                || ex is IOException
                || ex is UnauthorizedAccessException
                || ex is InvalidCastException;
        }
    }
}
