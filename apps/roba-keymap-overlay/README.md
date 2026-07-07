# roBa Keymap Overlay

Windows 向けの roBa レイヤー0キーマップ常時最前面オーバーレイアプリです。

## 機能

- レイヤー0のグラフィカルキーマップ表示（`layout/layer0.json`）
- ウィンドウリサイズに応じたスケール調整（全体表示優先）
- 透明度 0〜100%（スライダー）
- 常時最前面表示
- ロック時はクリックスルー（下のアプリを操作可能）
- 編集モードで移動・リサイズ・透明度調整
- 設定の永続化（`%APPDATA%\RoBaKeymapOverlay\settings.json`）

## ビルド要件

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (x64)

## ビルド

```powershell
cd apps/roba-keymap-overlay
dotnet build RoBaKeymapOverlay.sln -c Release
```

## 単一 exe の発行

```powershell
cd apps/roba-keymap-overlay
dotnet publish RoBaKeymapOverlay/RoBaKeymapOverlay.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

成果物:

`RoBaKeymapOverlay/bin/Release/net8.0-windows/win-x64/publish/RoBaKeymapOverlay.exe`

## 使い方

1. `RoBaKeymapOverlay.exe` をダブルクリック
2. デフォルトはロック状態（クリックスルー有効）
3. トレイアイコンを右クリック →「編集モード」で設定変更
4. 編集モード中: ドラッグで移動、端でリサイズ、スライダーで透明度変更
5. 「ロック」ボタンまたはトレイの「ロック」で確定

### ショートカット

- `Ctrl+Alt+L` : ロック / 編集モード切替

### トレイメニュー

- 編集モード
- ロック
- 透明度 +10% / -10%
- 終了

## レイアウトデータの同期

`layout/layer0.json` は [`config/roBa.json`](../../config/roBa.json) の座標と [`config/roBa.keymap`](../../config/roBa.keymap) のラベルを基にしています。

キーマップ変更時は以下を更新してください:

1. `config/roBa.keymap` のレイヤー0バインディング
2. `apps/roba-keymap-overlay/RoBaKeymapOverlay/layout/layer0.json` のラベル・`visible`
3. 座標変更時は `config/roBa.json` から `x`, `y`, `r`, `rx`, `ry` を反映

## 手動テストチェックリスト

- [ ] exe ダブルクリックでレイヤー0が表示される
- [ ] ウィンドウリサイズでキーマップが比例縮小される
- [ ] 極小ウィンドウでクラッシュしない
- [ ] ロック時に下のウィンドウをクリックできる
- [ ] 編集モードでスライダー・移動・リサイズができる
- [ ] 再起動後に位置・サイズ・透明度が復元される
- [ ] 常時最前面が維持される
- [ ] `Ctrl+Alt+L` でロック切替できる

## 注意

- self-contained 単一 exe は約 60〜80 MB 程度になります
- クリックスルー中はウィンドウ内スライダーは操作できません（トレイまたは編集モードを使用）
