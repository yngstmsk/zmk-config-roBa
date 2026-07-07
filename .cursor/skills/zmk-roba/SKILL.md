---
name: zmk-roba
description: >-
  ZMK ファームウェアと roBa 分割キーボード（Seeed XIAO BLE nRF52840、シールド roBa_L /
  roBa_R、PMW3610 トラックボール、EC11、BLE スプリット中央 roBa_R）のこのリポジトリ向け知識。
  キーマップ config/roBa.keymap、Kconfig（roBa_L.conf / roBa_R.conf）、boards/shields/roBa の
  dtsi/overlay、west.yml、build.yaml と GitHub Actions、Windows の BLE（GATT・ペアリング・切断）
  の既知対策を含む。ユーザーが roBa、ZMK、キーマップ、Bluetooth、トラックボール、ビルド、
  UF2、settings_reset、左右ペリフェラル、central、xiao_ble を言及したとき、またはワークスペースが
  zmk-config-roBa / roBa の設定ファイルを触るときは、このスキルとリポジトリ直下の CLAUDE.md を
  前提に答えること。
---

# zmk-config-roBa（roBa）エージェント向けコンテキスト

## 最初に読むファイル

設定変更・トラブル対応では **リポジトリ直下の `CLAUDE.md` を必ず読む**。そこに公式ドキュメントへの根拠、過去障害の記録、チェックリストが書いてある。このスキルは要約であり **`CLAUDE.md` と矛盾したら `CLAUDE.md` を優先**する。

## 絶対規約（変更作業）

`CLAUDE.md` の「Explain Before Changing」に従う。

1. 現状の問題点を具体的に述べる  
2. 根拠（ZMK docs / Issue / Zephyr 等）を示す  
3. 変更内容と理由を対応づけて説明する  
4. **ユーザー承認後にだけ**ファイルを編集する  

思いつきの変更は禁止。

## ハードウェアと役割

| 項目 | 内容 |
|------|------|
| MCU | Seeed XIAO BLE（nRF52840）、左右とも同ボード |
| 右 roBa_R | **Central**。PMW3610（SPI）トラックボール + EC11。公開名は `CONFIG_ZMK_KEYBOARD_NAME="roBa"`（HID 名） |
| 左 roBa_L | **Peripheral**。EC11 のみ。Peripheral は USB HID にならない（ZMK の仕様） |
| スプリット BLE | 固定 UUID（`ZMK_SPLIT_BT_SERVICE_UUID`）。キーボード表示名とは無関係 |
| ZMK | **main** ブランチ。PMW3610 は本体の `CONFIG_INPUT_PMW3610`（上流の外部 `CONFIG_PMW3610` とは別世代の構成があるので混同しない） |

## パス早見（編集で触る場所）

- `config/roBa.keymap` — 左右共通キーマップ（7 レイヤー、`&mkp` 等）
- `config/roBa.json` — レイアウトエディタ用
- `config/west.yml` — ZMK のソース参照
- `config/boards/shields/roBa/` — `Kconfig.*`、`roBa.dtsi`、`roBa_L/R.overlay`、`roBa_L/R.conf`、`*.zmk.yml`
- `build.yaml` — CI の board/shield マトリクス（`xiao_ble//zmk` を使う）

ビルド成果物の意味: `roBa_right` / `roBa_left` / `settings_reset`（NVS クリア用）。

## キーマップと Kconfig の依存

- キーマップに **`&mkp` / `&mmv` / `&msc` が 1 つでもあれば、左右両方の `.conf` で `CONFIG_ZMK_POINTING=y` が必須**。左だけ欠けると左の `.uf2` が出ずビルドが片側だけ失敗する典型原因。
- レイヤー構成・コンボ等の詳細は `CLAUDE.md` の表を参照。

## BLE / Windows で既に取り込まれている方針（要約）

左右 `.conf` に `CONFIG_BT_GATT_ENFORCE_SUBSCRIPTION=n`（Windows GATT とバッテリー通知の組み合わせ対策）、`CONFIG_BT_CTLR_PHY_2M=n`（一部 BT アダプタ互換）、`CONFIG_ZMK_USB=n`（xiao_ble//zmk の USB 既定上書き・無線運用）、`CONFIG_ZMK_BLE_EXPERIMENTAL_CONN=y`、`CONFIG_ZMK_SLEEP=n`、NVS デバウンス・接続数など **プロジェクト固有の安定化値**が入っている。値を削る・変える前は `CLAUDE.md` の「過去の障害記録」と公式ドキュメントで理由を確認する。

右（Central）のみ `CONFIG_BT_MAX_CONN` / `CONFIG_BT_MAX_PAIRED`、`CONFIG_BT_GATT_CACHING=n`、BLE スタック・HID キュー拡張などがある。

## ビルド・CI

- **board は `xiao_ble//zmk`**（`xiao_ble` だけだと ZMK の board 互換チェックで失敗しうる）
- ビルド定義は `build.yaml` と `.github/workflows/build.yml` を確認

## 作業時チェックリスト（短縮）

1. `&mkp` 等を変えたら grep でポインティング設定を再確認（`CLAUDE.md` の grep 例）  
2. 左右両方のアーティファクトが CI で生成されるか意識する  
3. `Kconfig.defconfig` の split / central の対応を壊さない  
4. `xiao_serial` は両側 disabled（D6/D7 をマトリクスに使用）

## ドキュメント検索を省くために

- ZMK の **一般的な**構文・ビヘイビアは Context7 / `https://zmk.dev` を参照してよい。  
- **このキーボード固有の pin・役割・既入りの Kconfig 値・Windows での経緯**は **`CLAUDE.md` とこのリポジトリのファイルが正**。

回答では、ユーザーの依頼が「キーマップだけ」でも、変更が BLE や `*.conf` に影響しないか一言確認すると安全。
