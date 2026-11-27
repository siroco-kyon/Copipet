# コピペット（Copipet）

よく使う定型文を登録して、**ワンクリックでクリップボードにコピー**するWindows用の小さなツールです。

- 1タブに10件まで登録でき、タブは1〜8つまで増減できます（最大80件）
- 左クリックでコピー、右クリックで編集
- ライト／ダークモード対応（Windowsの設定に合わせることもできます）
- exe1つで動作します。インストールは不要です

## 動作環境

- Windows 10 / 11
- .NET Framework 4.8（Windows 10 1903以降と Windows 11 には最初から入っています）

## 使い方

| 操作 | 動作 |
|---|---|
| コピーボタンを左クリック | 内容をクリップボードへコピー（画面下に「コピーしました」と2秒表示） |
| コピーボタンを右クリック | コピー内容の編集画面を開く |
| 未登録のボタンを左クリック | 編集画面を開く |
| コピーボタンにマウスを乗せる | 内容の先頭100文字をプレビュー |
| タブを左クリック | 表示する10件を切り替え |
| タブを右クリック | 名前を変更／左へ移動／右へ移動／削除 |
| 「＋」 | タブを追加（最大8つ） |
| 右下の ⚙ | テーマ切り替え、常に最前面に表示、データフォルダを開く |

編集画面では `Ctrl+S` で保存、`Esc` で保存せずに戻ります。
タイトルと内容を両方空にして保存すると、そのボタンは「（未登録）」に戻ります。

## データの保存場所

```
%APPDATA%\Copipet\data.json       … 登録内容と設定
%APPDATA%\Copipet\data.json.bak   … 1つ前の保存内容（自動）
```

- exeをどこへ移動しても、登録内容はそのまま残ります
- **別のPCへ移すとき**は、`%APPDATA%\Copipet` フォルダごとコピーしてください（⚙ →「データフォルダを開く」で開けます）
- データファイルが壊れていた場合は、自動で `data.json.bak` から復元します。壊れたファイルは `data.json.broken_日時` として残します

> **注意**：データは暗号化せずに保存しています。パスワードや個人情報など、他人に見られて困る内容は登録しないでください。

## ビルド

Visual Studio 2019 以降で `Copipet.sln` を開いてビルドするか、.NET SDK で次を実行します。

```
dotnet build Copipet.sln -c Release
```

出力先：`src/Copipet/bin/Release/net48/Copipet.exe`

GitHub Actions でも Windows 上で自動ビルドし、成果物（Copipet.exe）をダウンロードできます。`v1.0.0` のようなタグを付けてプッシュすると、Release に zip を添付します。

## フォルダ構成

```
Copipet.sln
src/Copipet/
├─ Program.cs                 … 起動処理（二重起動の防止）
├─ Models/AppData.cs          … データ構造と補正処理
├─ Services/DataStore.cs      … data.json の読み書き（安全な保存・破損時の復元）
├─ Services/ClipboardHelper.cs… 再試行つきのコピー
├─ Services/Theme.cs          … ライト／ダークの配色、タイトルバー・メニューの描画
├─ Forms/MainForm.cs          … トップ画面
├─ Forms/ItemEditForm.cs      … コピー内容の編集画面
├─ Forms/TabNameForm.cs       … タブ名の変更画面
└─ app.ico                    … アイコン（tools/make_icon.py で生成）
docs/SPEC.md                  … 仕様書
docs/TEST_CHECKLIST.md        … テスト項目
```

## 由来

YouTube「IT係長」チャンネルの [超便利コピペ君 C#で作ってみた](https://youtu.be/duYrASAFyy8) で紹介されている「コピペ君」の操作（左クリックでコピー、右クリックで編集）をもとに、中身を作り直したものです。
