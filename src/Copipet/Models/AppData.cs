using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Copipet.Models
{
    /// <summary>data.json に保存するデータ全体。</summary>
    [DataContract]
    internal sealed class AppData
    {
        public const int CurrentVersion = 2;
        public const int MaxTabs = 8;
        public const int MaxItemsPerTab = 20;
        public const int InitialItemsPerTab = 10;
        public const int InitialTabs = 3;
        public const int MaxTitleLength = 30;
        public const int MaxTabNameLength = 8;

        public const string ThemeAuto = "auto";
        public const string ThemeLight = "light";
        public const string ThemeDark = "dark";

        public const string LayoutTwoColumns = "two";
        public const string LayoutOneColumn = "one";

        [DataMember(Name = "version", Order = 0)]
        public int Version { get; set; }

        [DataMember(Name = "lastTab", Order = 1)]
        public int LastTab { get; set; }

        [DataMember(Name = "theme", Order = 2)]
        public string Theme { get; set; }

        [DataMember(Name = "topMost", Order = 3)]
        public bool TopMost { get; set; }

        [DataMember(Name = "layout", Order = 4)]
        public string Layout { get; set; }

        [DataMember(Name = "tabs", Order = 5)]
        public List<TabData> Tabs { get; set; }

        public static string DefaultTabName(int index)
        {
            return "タブ" + (index + 1);
        }

        public static AppData CreateDefault()
        {
            var data = new AppData
            {
                Version = CurrentVersion,
                LastTab = 0,
                Theme = ThemeAuto,
                TopMost = false,
                Layout = LayoutTwoColumns,
                Tabs = new List<TabData>()
            };
            for (int i = 0; i < InitialTabs; i++)
            {
                data.Tabs.Add(TabData.Create(DefaultTabName(i)));
            }
            return data;
        }

        /// <summary>読み込んだデータの欠けや範囲外の値を補正する。</summary>
        public void Normalize()
        {
            // version 1 以前（空の項目を詰めていた版）のデータは、各タブを10件まで空で埋める
            bool padTabs = Version < 2;
            Version = CurrentVersion;
            if (Tabs == null) Tabs = new List<TabData>();
            Tabs.RemoveAll(t => t == null);
            if (Tabs.Count > MaxTabs) Tabs.RemoveRange(MaxTabs, Tabs.Count - MaxTabs);
            if (Tabs.Count == 0) Tabs.Add(TabData.Create(DefaultTabName(0)));

            for (int i = 0; i < Tabs.Count; i++)
            {
                Tabs[i].Normalize(i);
                if (padTabs) Tabs[i].PadToInitial();
            }

            if (LastTab < 0 || LastTab >= Tabs.Count) LastTab = 0;
            if (Theme != ThemeLight && Theme != ThemeDark) Theme = ThemeAuto;
            if (Layout != LayoutOneColumn) Layout = LayoutTwoColumns;
        }
    }

    [DataContract]
    internal sealed class TabData
    {
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        [DataMember(Name = "items", Order = 1)]
        public List<ItemData> Items { get; set; }

        /// <summary>空の項目を10件持ったタブを作る。</summary>
        public static TabData Create(string name)
        {
            var tab = new TabData { Name = name, Items = new List<ItemData>() };
            tab.PadToInitial();
            return tab;
        }

        /// <summary>10件に満たなければ空の項目で埋める。</summary>
        public void PadToInitial()
        {
            while (Items.Count < AppData.InitialItemsPerTab) Items.Add(new ItemData());
        }

        public int RegisteredCount
        {
            get
            {
                int n = 0;
                foreach (var item in Items) if (!item.IsEmpty) n++;
                return n;
            }
        }

        public void Normalize(int index)
        {
            Name = (Name ?? string.Empty).Trim();
            if (Name.Length > AppData.MaxTabNameLength) Name = Name.Substring(0, AppData.MaxTabNameLength);
            if (Name.Length == 0) Name = AppData.DefaultTabName(index);

            if (Items == null) Items = new List<ItemData>();
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] == null) Items[i] = new ItemData();
                Items[i].Normalize();
            }
            if (Items.Count > AppData.MaxItemsPerTab) Items.RemoveRange(AppData.MaxItemsPerTab, Items.Count - AppData.MaxItemsPerTab);

        }
    }

    [DataContract]
    internal sealed class ItemData
    {
        [DataMember(Name = "title", Order = 0)]
        public string Title { get; set; }

        [DataMember(Name = "content", Order = 1)]
        public string Content { get; set; }

        public ItemData()
        {
            Title = string.Empty;
            Content = string.Empty;
        }

        /// <summary>内容が空なら「未登録」として扱う。</summary>
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(Content); }
        }

        /// <summary>タイトルも内容も空。</summary>
        public bool IsBlank
        {
            get { return string.IsNullOrEmpty(Content) && string.IsNullOrEmpty(Title); }
        }

        public void Normalize()
        {
            Title = (Title ?? string.Empty).Trim();
            if (Title.Length > AppData.MaxTitleLength) Title = Title.Substring(0, AppData.MaxTitleLength);
            if (Content == null) Content = string.Empty;
        }
    }
}
