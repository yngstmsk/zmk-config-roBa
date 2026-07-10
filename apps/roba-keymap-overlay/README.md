# roBa Keymap Overlay

Windows 向けの roBa キーマップ常時最前面オーバーレイアプリです。BLE 接続中に MO1/MO2 等でレイヤーが変わると、表示も自動で切り替わります。

## 機能

- レイヤー 0〜5 のグラフィカルキーマップ表示
- **BLE レイヤー同期**（ファーム `CONFIG_ZMK_LAYER_STATUS_BLE_HID` 対応）
- ウィンドウリサイズに応じたスケール調整（全体表示優先）
- 透明度 0〜100%（スライダー）
- 常時最前面表示
- ロック時はクリックスルー（下のアプリを操作可能）
- 編集モードで移動・リサイズ・透明度調整
- 設定の永続化（`%APPDATA%\RoBaKeymapOverlay\settings.json`）

## 前提

1. 右側（roBa_R）に **レイヤー通知付きファーム** をフラッシュ済みであること
2. Windows でキーボード名 **roBa** として BLE ペアリング済みであること

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
2. roBa を BLE 接続する
3. MO1 / MO2 / FN 等を押すとオーバーレイのレイヤー表示が切り替わる
4. トレイの「レイヤー同期」行で接続状態を確認できる

### ショートカット

- `Ctrl+Alt+L` : ロック / 編集モード切替

### トレイメニュー

- 編集モード
- ロック
- 透明度 +10% / -10%
- レイヤー同期ステータス（表示のみ）
- 終了

## レイヤー対応

| ファーム送信値 | レイヤー | 主な用途 |
|---|---|---|
| 0 | default | 通常 QWERTY |
| 1 | MO1 | 記号 |
| 2 | MO2 | 矢印・数字 |
| 3 | MO1+MO2 / MO3 | Fn・BT |
| 4 | FN (mo4) | F6〜F9 |
| 5 | layer_6 | BT 選択・bootloader |

## レイアウトデータの同期

- 座標: [`config/roBa.json`](../../config/roBa.json) → `layout/layer0.json`
- ラベル: [`config/roBa.keymap`](../../config/roBa.keymap) → `layout/layer0.json` と `layout/layer-labels.json`

キーマップ変更時は上記 JSON を更新してから再ビルドしてください。

## 手動テストチェックリスト

- [ ] exe 起動でレイヤー0が表示される
- [ ] MO1 押下でレイヤー1表示に切り替わる
- [ ] MO2 押下でレイヤー2表示に切り替わる
- [ ] キーを離すとレイヤー0に戻る
- [ ] トレイに「レイヤー同期: roBa」と表示される
- [ ] ウィンドウリサイズでキーマップが比例縮小される
- [ ] ロック時に下のウィンドウをクリックできる
- [ ] 再起動後に位置・サイズ・透明度が復元される

## レイヤー同期の仕組み

MO1/MO2 は PC に通常キーを送りません。そのため:

1. **ファーム**がレイヤー番号を HID 予約バイトに埋め込み、あわせて **F14〜F18 を押しっぱなし**にする（Windows Raw Input 用）
2. **アプリ**は Raw Input で F14=レイヤー1 … F18=レイヤー5 を検知して表示を切替
3. HID が開ける場合は予約バイトも併用

| 操作 | 期待される表示 |
|---|---|
| MO1 押下中 | レイヤー1 + MO1 が赤 |
| MO2 押下中 | レイヤー2 + MO2 が赤 |
| MO1+MO2 | レイヤー3 |
| FN 押下中 | レイヤー4 |

**注意:** レイヤー同期には **右側の最新 `roBa_right.uf2`**（F14〜通知入り）が必要です。

## 診断（切り分け）

| 症状 | 意味 |
|---|---|
| A が赤くならない | Raw Input 未受信 |
| A は赤いが MO1 でレイヤーが変わらない | 右ファームが古い／F14 通知なし |
| トレイに `レイヤー 1 (Raw F-key)` | レイヤー同期 OK |

## トラブルシューティング

**レイヤーが切り替わらない**

- roBa_R に最新ファーム（`CONFIG_ZMK_LAYER_STATUS_BLE_HID=y`）が入っているか確認
- トレイの同期ステータスが「キーボードを待機中」のままなら、BLE 再接続後にアプリを再起動
- デバイス名が `roBa` でない場合は `%APPDATA%\RoBaKeymapOverlay\settings.json` の `keyboardDeviceName` を変更

## 注意

- self-contained 単一 exe は約 60〜80 MB 程度になります
- HID 読み取りは [keyboard_layers_app_companion](https://github.com/maatthc/keyboard_layers_app_companion) と同様の方式です
- クリックスルー中はウィンドウ内スライダーは操作できません（トレイまたは編集モードを使用）
