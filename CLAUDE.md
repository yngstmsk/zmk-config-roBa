# roBa ZMK Config - Claude Code Instructions

## ABSOLUTE RULE: Explain Before Changing

**このリポジトリでは、いかなる設定ファイルの変更も、以下を先に説明してから行うこと。**

1. **現状の何が問題か** — 具体的にどの設定が、なぜ問題を引き起こしているか
2. **根拠となる情報源** — ZMK 公式ドキュメント、GitHub Issue、Zephyr ドキュメント等の具体的な参照先
3. **提案する変更内容と理由** — 各変更が何をどう解決するかの対応関係
4. **ユーザーの承認を得てから変更を実行する**

この手順を省略した「思いつきの変更」は過去に繰り返し失敗している。**説明なしの変更は絶対に行わない。**

---

## Project Overview

roBa はオリジナルの自作分割キーボード。ZMK ファームウェアを使い、GitHub Actions でビルドする。
ハードウェアは左右とも Seeed XIAO BLE (nRF52840)。

- **右側 (roBa_R)**: Central ロール。PMW3610 トラックボール (SPI) + EC11 エンコーダ搭載。
- **左側 (roBa_L)**: Peripheral ロール。EC11 エンコーダのみ。
- BLE スプリット通信は固定 UUID (`ZMK_SPLIT_BT_SERVICE_UUID`) で行う。キーボード名は無関係。
- ZMK は `main` ブランチを使用。PMW3610 ドライバは ZMK 本体に内蔵 (`CONFIG_INPUT_PMW3610`)。
- 参考上流: `https://github.com/kumamuk-git/zmk-config-roBa` (`v0.3-branch`, 外部ドライバ `CONFIG_PMW3610`)
- ZMK peripheral は USB キーボードとして機能しない。これは仕様どおり。

---

## ファイル構成と役割

```
config/
  west.yml                              # ZMK mainブランチをソースに指定
  roBa.keymap                           # 左右共通のキーマップ（7レイヤー）
  roBa.json                             # キーマップエディタ用レイアウト定義
  boards/shields/roBa/
    Kconfig.shield                      # SHIELD_ROBA_R / SHIELD_ROBA_L の条件定義
    Kconfig.defconfig                   # ZMK_SPLIT / ZMK_SPLIT_ROLE_CENTRAL の定義
    roBa.dtsi                           # 共通ハードウェア定義（マトリクス行, エンコーダ, 物理レイアウト）
    roBa_L.overlay                      # 左側: マトリクス列GPIO, エンコーダ有効化, xiao_serial無効化
    roBa_R.overlay                      # 右側: マトリクス列GPIO, トラックボールSPI, xiao_serial無効化
    roBa_L.conf                         # 左側ビルド設定
    roBa_R.conf                         # 右側ビルド設定
    roBa.zmk.yml                        # ZMKシールドメタデータ
    roBa_L.zmk.yml                      # 左側シールドメタデータ
    roBa_R.zmk.yml                      # 右側シールドメタデータ
build.yaml                              # GitHub Actions ビルドマトリクス
```

---

## Kconfig 設定詳細

### roBa_L.conf（左側 / Peripheral）

```
CONFIG_ZMK_BLE=y
CONFIG_NFCT_PINS_AS_GPIOS=y
CONFIG_EC11=y
CONFIG_EC11_TRIGGER_GLOBAL_THREAD=y
CONFIG_ZMK_BATTERY_REPORTING=y
CONFIG_ZMK_POINTING=y               # &mkp バインディング使用のため必須
CONFIG_BT_CTLR_PHY_2M=n
```

### roBa_R.conf（右側 / Central）

```
CONFIG_ZMK_KEYBOARD_NAME="roBa"
CONFIG_ZMK_POINTING=y
CONFIG_NFCT_PINS_AS_GPIOS=y
CONFIG_ZMK_BLE=y
CONFIG_SPI=y
CONFIG_INPUT=y
CONFIG_INPUT_PMW3610=y              # ZMK mainブランチ内蔵ドライバ
CONFIG_EC11=y
CONFIG_EC11_TRIGGER_GLOBAL_THREAD=y
CONFIG_ZMK_BATTERY_REPORTING=y
CONFIG_ZMK_SPLIT_BLE_CENTRAL_BATTERY_LEVEL_PROXY=y
CONFIG_ZMK_SPLIT_BLE_CENTRAL_BATTERY_LEVEL_FETCHING=y
CONFIG_BT_CTLR_PHY_2M=n
```

---

## ハードウェア詳細

### キーマトリクス（共通 / roBa.dtsi）

- 構成: 4行 × 11列（左5列 + 右6列）
- 方向: col2row（ダイオード COL → ROW 方向）
- 行GPIO（両側共通、`xiao_d` 1〜3 + D6）:
  - D1, D2, D3, D6

### 列GPIO

**左側 (roBa_L.overlay)**:
- D10, D9, D8, D7, P0.10, P0.09

**右側 (roBa_R.overlay)** (col-offset=6):
- D10, D9, D8, D7, P0.10

### エンコーダ（共通 / roBa.dtsi）

- 左エンコーダ: A=D5, B=D0（左側 overlay で有効化）
- 右エンコーダ: roBa.dtsi で宣言のみ（右側 overlay で個別設定する場合）
- triggers-per-rotation: 10

### トラックボール PMW3610（右側 / roBa_R.overlay）

- SPI バス: spi0
- SCK: P0.05, MOSI/MISO: P0.04（3線SPI）
- CS: P0.09 (active low)
- MOTION: P0.02 (active low, pull-up)
- 軸マッピング: axis-x=INPUT_REL_Y, axis-y=INPUT_REL_X（X/Y 入れ替え）
- CPI: 400, smart-mode 有効, invert-x 有効
- `xiao_serial` は両側とも disabled（D6/D7 をマトリクスに使用するため）

---

## キーマップ構成（config/roBa.keymap）

7レイヤー構成。日本語キーボード設定を前提とした記号マッピング定義あり。

| レイヤー番号 | 名前 | 主な用途 |
|---|---|---|
| 0 | default_layer | 通常入力（QWERTY）, `&mkp` 使用 |
| 1 | Layer1 | 記号・括弧 |
| 2 | Layer2 | 矢印・数字 |
| 3 | Layer3 | Fn・BT操作・エンコーダ設定 |
| 4 | MOUSE | マウスボタン操作 |
| 5 | SCROLL | スクロール操作 |
| 6 | layer_6 | BT選択・bootloader |

**コンボ**:
- key-positions 37+38 → `&to 4` (MOUSEレイヤー)
- key-positions 39+38 → `&to 5` (SCROLLレイヤー)

**注意**: `&mkp LCLK`, `&mkp RCLK`, `&mkp MB3` を使用 → **両側とも `CONFIG_ZMK_POINTING=y` 必須**

---

## Kconfig.defconfig（スプリット役割定義）

```
if SHIELD_ROBA_R
  ZMK_KEYBOARD_NAME = "roBa_R"
  ZMK_SPLIT = y
  ZMK_SPLIT_ROLE_CENTRAL = y
endif

if SHIELD_ROBA_L
  ZMK_KEYBOARD_NAME = "roBa_L"
  ZMK_SPLIT = y
  (ZMK_SPLIT_ROLE_CENTRAL は設定しない)
endif
```

---

## GitHub Actions ビルド（build.yaml）

3つのアーティファクト:
- `roBa_right` : board=xiao_ble//zmk, shield=roBa_R
- `roBa_left`  : board=xiao_ble//zmk, shield=roBa_L
- `settings_reset` : board=xiao_ble//zmk, shield=settings_reset

**注意**: `xiao_ble` ではなく `xiao_ble//zmk` を使うこと。
ZMK commit `a23aa009`（2026-03-03）で `ZMK_BOARD_COMPAT` チェックが追加され、
zmk バリアントが存在するのに旧形式を使うとビルドが error で失敗する。

---

## 過去の障害記録

### Windows BLE ランダム切断問題（2026-03）

**症状**: Windows PC に接続後、ランダムなタイミングで切断され、再接続できなくなる。

**根本原因（特定済み）**: 以下の2点を修正済み。

1. **Windows GATT サブスクリプションバグ**
   - `CONFIG_ZMK_BATTERY_REPORTING=y` + `CONFIG_ZMK_SPLIT_BLE_CENTRAL_BATTERY_LEVEL_PROXY=y` の組み合わせで、キーボードが Windows にバッテリー GATT 通知を送り続ける。
   - Windows の既知バグにより、この通知に対して不正な GATT サブスクリプション更新を返す。
   - ZMK のデフォルト `CONFIG_BT_GATT_ENFORCE_SUBSCRIPTION=y` はこれを接続違反として切断する。
   - **対策**: 両 `.conf` に `CONFIG_BT_GATT_ENFORCE_SUBSCRIPTION=n` を追加。
   - **根拠**: https://zmk.dev/docs/config/bluetooth

2. **TX 出力がデフォルト（0 dBm）のまま**
   - 距離・障害物・BT 干渉があると信号品質が劣化し切断を誘発する。
   - **対策**: 両 `.conf` に `CONFIG_BT_CTLR_TX_PWR_PLUS_8=y` を追加。消費電力増加は無視できるレベル。
   - **根拠**: https://zmk.dev/docs/troubleshooting/connection-issues

**既設定で問題なし**: `CONFIG_BT_CTLR_PHY_2M=n` は Intel/Realtek BT アダプタとの互換性対策として既に適用済み。

3. **`xiao_ble//zmk` が `CONFIG_ZMK_USB=y` を強制する問題**
   - `xiao_ble//zmk` の defconfig に `CONFIG_ZMK_USB=y` が含まれ、USB 接続時に BLE アドバタイズを行わなくなる。
   - USB 経由でフラッシュした直後にそのまま BLE 接続を試みると、キーボードが BT スキャンに現れない。
   - **対策**: 両 `.conf` に `CONFIG_ZMK_USB=n` を追加して上書き。roBa は無線専用キーボードであり USB HID 出力は不要。
   - フラッシュは UF2 ブートローダー経由のため `CONFIG_ZMK_USB=n` でも書き込みには影響なし。

#### この問題で試したが採用しなかった設定（将来の調査用）

もし上記対策後も切断が続く場合の追加候補:

| 設定 | 説明 | 採用しなかった理由 |
|---|---|---|
| `CONFIG_ZMK_BLE_EXPERIMENTAL_CONN=y` | ZMK が計画する接続安定化設定群 | `BT_CTLR_PHY_2M=n` と重複。競合リスクあり |
| `CONFIG_CLOCK_CONTROL_NRF_K32SRC_RC=y` | 外部発振器の代わりに内部RC発振器を使用 | 外部発振器の物理不良がある個体向け。「特定デバイスとだけ接続できない」症状が出る場合に試す |
| `CONFIG_ZMK_BLE_PASSKEY_ENTRY=y` | ペアリング時にパスキー入力を要求 | 企業管理 Windows PC で接続できない場合向け。全デバイスの再ペアリングが必要 |
| `CONFIG_BT_BAS=n` | バッテリーサービス (BAS) を完全無効化 | `GATT_ENFORCE_SUBSCRIPTION=n` で代替解決済み。バッテリー表示が消える副作用あり |

**ZMK 接続パラメータ（変更不要）**:
- `BT_PERIPHERAL_PREF_MIN_INT=6` (7.5ms), `MAX_INT=12` (15ms)
- `BT_PERIPHERAL_PREF_LATENCY=30`, `TIMEOUT=400` (4000ms)
- これらは ZMK チームが全キーボード向けにチューニングした値。公式ドキュメントに変更推奨なし。

---

### 左側キーが動作しない問題（2026-02）

**根本原因**: `CONFIG_ZMK_POINTING=y` が `roBa_L.conf` に未記載。
`&mkp` バインディングを使用するキーマップがあるため、左側ビルドが完全に失敗し `.uf2` が生成されなかった。

**やってはいけない誤った対処法**:
1. `roBa_L.conf` からバッテリーProxy設定を削除 → 無害なので不要
2. `ZMK_KEYBOARD_NAME` を左右で同じにする → UUID で発見するので名前は無関係
3. `CONFIG_ZMK_BLE_EXPERIMENTAL_CONN` を削除 → ペリフェラル発見とは無関係
4. `CONFIG_EC11` を `roBa_R.conf` から削除 → 両側とも必要

---

## 変更時の必須チェックリスト

1. **キーマップに `&mkp` / `&mmv` / `&msc` があれば両側とも `CONFIG_ZMK_POINTING=y`**
   確認: `grep -c '&mkp\|&mmv\|&msc' config/roBa.keymap`

2. **両側とも `.uf2` が GitHub Actions で生成されること。片側だけ無い場合はビルド失敗。**

3. **設定を削除・変更する前に目的を理解すること。** ZMK ドキュメントまたは上流リポジトリで確認。

4. **変更前に上流と比較すること。** `https://github.com/kumamuk-git/zmk-config-roBa`

5. **`Kconfig.defconfig` は両側に `ZMK_SPLIT=y`、右側のみに `ZMK_SPLIT_ROLE_CENTRAL=y`。**

6. **`xiao_serial` は両側とも `disabled`。** D6/D7 をキーマトリクスの行に使用しているため。
