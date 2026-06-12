# 詳細設計書：ピタブロ

## 1. アーキテクチャ・ディレクトリ構成

本章では、Unityプロジェクト内におけるモジュール間の依存関係と、ファイル群の整理方針（ディレクトリ構造）を定義します。本プロジェクトは、機能追加や改修（新しい動物やブロックの追加など）を容易にするため、ロジックとUIを分離した「イベント駆動アーキテクチャ」を採用します。

### 1.1 全体クラス相関図（依存関係・イベント通知のフロー）

各クラスは過度な密結合を避け、`System.Action`（デリゲート）を用いたイベント通知によって状態を連携します。

* **GameManager (Singleton)**
  * **役割:** ゲーム全体のステート（タイトル、プレイ中、ポーズ、リザルト）を管理するハブ。
  * **依存:** `UIManager`, `GridManager` など各マネージャーの初期化を統括。
* **InputManager (Singleton)**
  * **役割:** 画面のタッチ・ドラッグ操作を検知。
  * **通知先:** タッチされた `BlockController` へ直接ドラッグイベントを発行する。
* **GridManager (Singleton)**
  * **役割:** 8×12の配列データ（盤面状態）の保持、ブロックの配置可否判定、ライン消去判定。
  * **イベント発行:** `OnLineCleared(int lines)`（ラインが消えた時）、`OnGameOver()`（置ける場所がなくなった時）。
  * **通知先:** `UIManager`（シールの獲得演出など）、`ParticleController`（消去エフェクト再生）。
* **BlockController (MonoBehaviour)**
  * **役割:** 各ブロックオブジェクトの座標計算、回転、スナップ処理。
  * **依存:** `InputManager` から操作を受け取り、ドロップ時に `GridManager` へ「現在座標に配置可能か」を問い合わせる。
* **UIManager (Singleton)**
  * **役割:** Canvas上のUI（ボタン、スコア、リザルト画面）の表示切り替え。
  * **依存:** 各マネージャーからのイベントを購読（Subscribe）し、画面表示を更新する。

**【主要なイベントフローの例】**  

* `InputManager` (指を離す)
  → `BlockController` (配置判定を要求)
  → `GridManager` (判定OK・配列更新・ライン消去確認)
  → (ライン消去発生) → `GridManager.OnLineCleared` 発火
  → `UIManager` & `ParticleController` (褒める演出・エフェクト再生)

---

### 1.2 フォルダ構成とネームスペース（名前空間）の定義

AI（Cursor）による自動生成コードの品質を保ち、ファイルが散らかるのを防ぐため、Unityの `Assets` フォルダ配下を以下のように厳格に定義します。

**【ネームスペース（名前空間）】**
すべてのC#スクリプトは `Pitablock` 名前空間に配置し、機能ごとにサブネームスペースで分割します。
例: `Pitablock.Managers`, `Pitablock.Controllers`, `Pitablock.Data`, `Pitablock.UI`

**【Assets 配下のフォルダ構造】**  

```text
Assets/
 ├── Animations/          # どうぶつ先生のジャンプやUIアニメーション(.anim, .controller)
 ├── Audio/               # BGM、効果音、ボイス(.mp3, .wav)
 │    ├── BGM/
 │    └── SE/
 ├── Materials/           # URP用マテリアル、パーティクル用マテリアル(.mat)
 ├── Prefabs/             # 生成用プレハブ(.prefab)
 │    ├── Blocks/         # 7種類のブロック
 │    ├── UI/             # 各画面（TitleUI, InGameUIなど）
 │    └── Effects/        # パーティクルエフェクト
 ├── Scenes/              # シーンファイル(.unity)
 │    └── MainScene.unity # 基本は1シーンで運用
 ├── ScriptableObjects/   # マスタデータ(.asset)
 │    ├── Blocks/         # BlockData
 │    └── Animals/        # AnimalData
 ├── Scripts/             # C#スクリプト(.cs)
 │    ├── Controllers/    # BlockController, ParticleController など
 │    ├── Data/           # PlayerData, BlockData定義 など
 │    ├── Managers/       # GameManager, GridManager など
 │    └── UI/             # UIManager, ReportUI など
 └── Sprites/             # 2D画像素材(.png)
      ├── Blocks/         # ブロックのテクスチャ
      ├── Animals/        # どうぶつ先生の画像
      └── UI/             # ボタン、アイコン、背景
```

## 2. マネージャークラス詳細設計（Singleton群）

本章では、ゲームのコアロジックと進行を司る主要なマネージャークラスの内部設計を定義します。各マネージャーは、どこからでもアクセスしやすくするため「Singleton（シングルトン）パターン」で実装し、ヒエラルキー上の空のGameObject（例: `Managers`）にアタッチして使用します。

### 2.1 GameManager

**役割:** ゲーム全体の状態（ステート）を管理し、他のマネージャーの初期化順序を制御します。

* **列挙型 (Enum):**
  * `GameState { Title, ModeSelect, Playing, Paused, Result }`
* **主要プロパティ:**
  * `public static GameManager Instance { get; private set; }`
  * `public GameState CurrentState { get; private set; }`
* **主要イベント (System.Action):**
  * `public event Action<GameState> OnStateChanged;` （状態が変わった時に発行）
* **主要メソッド:**
  * `void Awake()`: Singletonの初期化（`DontDestroyOnLoad`は使用せず、1シーン運用とする）。
  * `public void ChangeState(GameState newState)`: 状態を変更し、`OnStateChanged`イベントを発火する。

---

### 2.2 GridManager

**役割:** 盤面（幅8×高さ12）のデータ保持、配置可否の計算、ライン消去判定、およびUndo（1手戻す）の履歴管理を行います。

* **主要プロパティ:**
  * `public static GridManager Instance { get; private set; }`
  * `private int[,] gridArray = new int[8, 12];` （0: 空き、1以上: ブロックIDまたは色情報）
  * `private Stack<GridStateSnapshot> undoStack;` （Undo機能用の履歴スタック）
    *(※ `GridStateSnapshot` は、盤面配列のコピーと、その手で配置したブロックのインスタンスIDを保持する内部構造体)*
* **主要イベント:**
  * `public event Action<int> OnLineCleared;` （引数は同時消去したライン数）
* **主要メソッド:**
  * `public bool CanPlaceBlock(Vector2Int[] gridPositions)`:
    引数の座標配列が、グリッドの範囲内か、かつ他のブロックと重なっていないか（`gridArray`が0か）を判定して返す。
  * `public void PlaceBlock(Vector2Int[] gridPositions, int blockId, GameObject blockInstance)`:
    ブロックを配置し、配列を更新。同時に現在の状態を `undoStack` にPushする。その後、`CheckAndClearLines()` を呼び出す。
  * `private void CheckAndClearLines()`:
    Y軸（0〜11）を下からスキャンし、すべて埋まっている列を特定。該当列を0クリアし、上部のブロックを下にシフトさせる（内部配列と実際のTransform座標の両方を更新）。
  * `public void UndoLastMove()`:
    `undoStack` から直前の状態をPopし、`gridArray` を復元。対象のブロックインスタンスを手持ちの初期位置に戻す。

---

### 2.3 InputManager

**役割:** スマートフォン（タッチ）およびPC（マウスクリック）の入力を抽象化し、操作対象のブロックへイベントを伝達します。

* **主要プロパティ:**
  * `public static InputManager Instance { get; private set; }`
  * `private BlockController currentDraggingBlock;` （現在掴んでいるブロックの参照）
* **主要メソッド:**
  * `void Update()`: 毎フレーム、入力状態を監視する。
    * **タッチ開始時 (`Input.GetMouseButtonDown(0)` など):**
      タップした画面座標から `Physics2D.Raycast` を飛ばし、`BlockController` がアタッチされたオブジェクトに当たれば、それを `currentDraggingBlock` に設定。
      同ブロックの `OnDragStart()` を呼び出す。
    * **ドラッグ中 (`Input.GetMouseButton(0)`):**
      `currentDraggingBlock` が存在すれば、現在のタッチ座標（ワールド座標変換後）を渡し、ブロックの `OnDragging(Vector2 targetPos)` を呼び出す。
    * **タッチ終了時 (`Input.GetMouseButtonUp(0)`):**
      `currentDraggingBlock.OnDragEnd()` を呼び出し、`currentDraggingBlock` を null にクリアする。

---

### 2.4 UIManager

**役割:** UIのCanvasおよび各パネル（画面）の表示切り替えと、ユーザーのボタン操作に伴うイベント発行を担当します。

* **主要プロパティ:**
  * `public static UIManager Instance { get; private set; }`
  * `[SerializeField] private GameObject titlePanel;`
  * `[SerializeField] private GameObject modeSelectPanel;`
  * `[SerializeField] private GameObject inGamePanel;`
  * `[SerializeField] private GameObject resultPanel;`
* **主要メソッド:**
  * `void Start()`: `GameManager.Instance.OnStateChanged` に `UpdatePanels` メソッドを登録（Subscribe）する。
  * `private void UpdatePanels(GameState state)`:
    現在のStateに応じて、対応するPanelのみ `SetActive(true)` にし、他を `false` にする。
  * `public void ShowPraiseEffect(int lineCount)`:
    `GridManager` の `OnLineCleared` をフックして呼び出され、消した列数に応じて「すごい！」「大技だ！」などのUIアニメーションを再生する。

---

### 2.5 SaveDataManager

**役割:** プレイヤーのプレイ記録や図鑑・シールの解放状況をローカルストレージにJSON形式で保存・読み込みします。

* **主要プロパティ:**
  * `public static SaveDataManager Instance { get; private set; }`
  * `public PlayerData CurrentData { get; private set; }` （メモリ上に保持する現在のセーブデータ）
  * `private string saveFilePath;` （保存先のパス: `Application.persistentDataPath + "/savedata.json"`）
* **主要メソッド:**
  * `public void LoadData()`:
    起動時に呼び出し。ファイルが存在すれば `File.ReadAllText` と `JsonUtility.FromJson<PlayerData>` で読み込む。存在しなければ初期値を入れた新規の `PlayerData` を生成する。
  * `public void SaveData()`:
    `CurrentData` を `JsonUtility.ToJson` で文字列化し、`File.WriteAllText` でストレージに書き込む（課題クリア時やアプリ終了時に呼び出し）。
  * `public void AddPlayTime(int minutes)`:
    プレイ時間を加算して保存する。
  * `public void UnlockAnimal(int animalId)`:
    `CurrentData.unlockedAnimals` に指定IDが含まれていなければ追加し、保存する。

## 3. プレハブ・オブジェクト制御詳細設計

本章では、ゲーム画面内で実際に描画・操作されるオブジェクト（ブロックやエフェクト）の振る舞いと、それらにアタッチされるコンポーネントの内部設計を定義します。

### 3.1 BlockController

**役割:** 7種類の各ブロックプレハブのルート（親）オブジェクトにアタッチされ、指への追従、グリッドへのスナップ計算、およびタップによる回転処理を担当します。

* **状態定義 (Enum):**
  * `BlockState { InHand, Dragging, Placed }`
* **主要プロパティ:**
  * `public BlockState CurrentState { get; private set; }`
  * `public BlockData Data { get; private set; }` （このブロックの形状・色マスタデータ）
  * `private Transform[] squareTransforms;` （子オブジェクトである4つの四角形スプライトの参照）
  * `private Vector3 initialPosition;` （手持ちエリアの初期座標。キャンセル時に戻る場所）
* **主要メソッド:**
  * `public void Initialize(BlockData data)`:
    生成時に呼ばれ、マスタデータをセットし、子オブジェクトの色（Sprite）を適用する。
  * `public void OnDragStart(Vector2 touchPos)`:
    `InputManager` から呼ばれる。`CurrentState` を `Dragging` に変更し、少しだけ上に浮かせる（指で隠れないようにするためのオフセット加算）などの視覚的調整を行う。
  * `public void OnDragging(Vector2 targetPos)`:
    毎フレーム呼ばれ、`Transform.position` を `targetPos`（＋オフセット）に追従させる。スムーズに動かすため `Vector3.Lerp` などを活用する。
  * `public void OnDragEnd()`:
    指を離した時に呼ばれる。
    1. 現在の `Transform.position` と、子オブジェクトのローカル座標から、盤面上の仮想グリッド座標 `Vector2Int[]`（4マス分）を算出する。
    2. `GridManager.Instance.CanPlaceBlock(算出座標)` を呼び出す。
    3. **Trueの場合:** `GridManager.Instance.PlaceBlock(...)` を呼び出し、座標をグリッドの交点に「ピタッ」とスナップ（丸め込み）させる。状態を `Placed` に変更。
    4. **Falseの場合:** 配置不可（枠外や重なり）とみなし、`Transform.position` を `initialPosition` へアニメーションさせながら戻す（Dotween等を使用）。
  * `public void RotateBlock()`:
    ドラッグではなく「タップ」されたと `InputManager` が判定した際に呼ばれる。
    子オブジェクトのローカル座標 `(x, y)` を、90度回転の行列計算 `(y, -x)` に従って更新し、見た目を回転させる。

---

### 3.2 BlockSpawner

**役割:** 画面下部の「手持ちエリア」に新しいブロックを生成し、お助け機能である「チェンジ（魔法のステッキ）」の回数とロジックを管理します。

* **主要プロパティ:**
  * `[SerializeField] private BlockController blockPrefabBase;` （ベースとなるプレハブ）
  * `[SerializeField] private List<BlockData> availableBlocks;` （全7種のブロックデータ）
  * `[SerializeField] private Transform spawnPoint;` （手持ちエリアの座標）
  * `private BlockController currentHandBlock;` （現在手持ちにあるブロックの参照）
  * `public int RemainingChangeCount { get; private set; } = 3;` （チェンジ残り回数）
* **主要メソッド:**
  * `public void SpawnRandomBlock()`:
    `availableBlocks` からランダムに1つ選び、`blockPrefabBase` を `spawnPoint` に `Instantiate` する。生成したインスタンスの `Initialize` を呼び、`currentHandBlock` に保持する。
  * `public void OnChangeButtonPressed()`:
    UIのチェンジボタンから呼び出される。
    1. `RemainingChangeCount` が1以上かチェックする。
    2. `ParticleController.Instance.PlayMagicEffect(spawnPoint.position)` で魔法の演出を再生。
    3. 現在の `currentHandBlock.gameObject` を破棄（Destroy）する。
    4. `RemainingChangeCount` を-1し、`SpawnRandomBlock()` で新しいブロックを即座に生成する。
    5. UIの残り回数表示を更新するようイベントを発行。

---

### 3.3 ParticleController

**役割:** ラインを消去した時や、魔法のチェンジを使った時などに発生するパーティクルシステム（エフェクト）の再生を統括します。オブジェクトプール（Object Pooling）を導入し、メモリ負荷を軽減します。

* **主要プロパティ:**
  * `public static ParticleController Instance { get; private set; }`
  * `[SerializeField] private ParticleSystem lineClearEffectPrefab;` （星が弾けるエフェクト）
  * `[SerializeField] private ParticleSystem magicChangeEffectPrefab;` （魔法の光エフェクト）
  * `private Queue<ParticleSystem> clearEffectPool;` （使い回し用のプール）
* **主要メソッド:**
  * `void Awake()`:
    ゲーム開始時にエフェクトをあらかじめ数個生成し、非表示（`SetActive(false)`）の状態で `clearEffectPool` に格納しておく。
  * `public void PlayLineClearEffect(int rowIndex)`:
    `GridManager.OnLineCleared` イベントから呼ばれる。
    対象となる列（Y座標）のワールド座標を計算し、プールから取り出したパーティクルをその位置に移動させて `Play()` を実行する。再生終了後に再びプールに戻す処理（コルーチン等）を行う。
  * `public void PlayMagicEffect(Vector3 position)`:
    チェンジボタンが押された座標で、魔法のパーティクルを1度だけ再生する。

## 4. データモデル・マスタデータ設計

本章では、ゲーム内で使用される不変の「マスタデータ（ScriptableObject）」と、ユーザーのプレイ状況を保持する可変の「セーブデータ（JSONシリアライズ用クラス）」の構造を定義します。

### 4.1 BlockData (マスタデータ)

**役割:** 7種類のブロック（I, O, T, S, Z, J, L）の形状と見た目を定義します。Unityのインスペクター上でエンジニア以外のメンバー（将来的な外注や協力者など）でも直感的に調整できるよう、`ScriptableObject` として実装します。

* **クラス定義のイメージ:**

  ```csharp
  [CreateAssetMenu(fileName = "NewBlockData", menuName = "Pitablock/BlockData")]
  public class BlockData : ScriptableObject
  {
      [Header("基本情報")]
      public int blockId;              // ブロック固有のID (例: 1〜7)
      public string blockName;         // デバッグ用名称 (例: "T-Block")
      
      [Header("見た目")]
      public Sprite blockSprite;       // 1マスの画像（角丸の四角形など）
      public Color blockColor;         // ブロックの色（パステルカラー等）

      [Header("形状定義")]
      // 基準となるマス(0,0)から見た、4つのマスのローカル相対座標(X, Y)
      // 例: Tブロックの場合 -> (0,0), (-1,0), (1,0), (0,1)
      public Vector2Int[] localPositions = new Vector2Int[4]; 
  }
  ```

### 4.2 PlayerData (セーブデータ)

**役割:** アプリの終了・再起動後も引き継がれるユーザーデータを定義します。`JsonUtility` を用いて軽量なJSON文字列に変換し、端末のローカルストレージに保存します。

* **クラス定義のイメージ:**

```csharp
[System.Serializable]
public class PlayerData
{
    public int totalPlayTimeMinutes;      // 累計プレイ時間（分）
    public int clearedStageCount;         // 「ブロック道」でクリアした課題の累計数
    public string lastPlayDate;           // 最後にプレイした日付（"yyyy-MM-dd"）
    public int consecutivePlayDays;       // 連続プレイ日数（シール獲得条件用）

    public List<int> unlockedAnimalIds;   // 図鑑で解放済みのどうぶつIDリスト
    public List<int> unlockedSealIds;     // 獲得済みシールIDリスト

    // 新規プレイヤー用の初期化コンストラクタ
    public PlayerData()
    {
        totalPlayTimeMinutes = 0;
        clearedStageCount = 0;
        lastPlayDate = "";
        consecutivePlayDays = 0;
        unlockedAnimalIds = new List<int>();
        unlockedSealIds = new List<int>();
    }
}
```

### 4.3 AnimalData / SealData (コレクション用マスタデータ)

**役割:** 「どうぶつ図鑑」や「シール帳」に表示するキャラクターやアイテムの情報を定義します。このデータを分離しておくことで、アップデート時に新しい動物を追加する際、プログラム（C#コード）を一切書き換えることなく、Unityエディタ上でデータを1つ作成するだけで完了するようになります。

* **AnimalData クラス定義 (ScriptableObject):**

```csharp
[CreateAssetMenu(fileName = "NewAnimalData", menuName = "Pitablock/AnimalData")]
public class AnimalData : ScriptableObject
{
    public int animalId;             // 動物固有のID
    public string animalName;        // 名前（例："うさぎ先生"）
    public Sprite animalSprite;      // 図鑑やプレイ画面で表示する画像
    public AudioClip voiceClip;      // タップ時や褒める時のボイス・効果音
    public int unlockRequiredLevel;  // 解放に必要なクリア課題数（ロック解除フラグ用）
}
```

* **SealData クラス定義 (ScriptableObject):**

```csharp
[CreateAssetMenu(fileName = "NewSealData", menuName = "Pitablock/SealData")]
public class SealData : ScriptableObject
{
    public int sealId;               // シール固有のID
    public string sealTitle;         // シールのタイトル（例："はじめての1れつ"）
    public string description;       // 獲得条件の説明（保護者レポート画面用）
    public Sprite sealSprite;        // シールの画像
}
```

## 5. UIコンポーネント・イベント設計

本章では、ユーザー（幼児および保護者）が直接触れる画面インターフェースの構成と、ボタン操作等のUIイベントがゲームロジック（C#）にどのように伝達されるかを定義します。UIとロジックは密結合を避け、`UnityEvent` とデリゲートを活用して連携させます。

### 5.1 画面ごとのUIプレハブ構成

シーン内に配置された1つの巨大な `Canvas` の配下に、各画面を担うPanelを配置し、`UIManager` がアクティブ状態を切り替える設計とします。幼児向けであるため、すべてのボタンは「大きく（指で隠れないサイズ）」「角丸」「文字ではなくアイコン（ピクトグラム）主体」とします。

* **TitlePanel (タイトル画面)**
  * **スタートボタン:** 大きな再生マーク。タップで ModeSelectPanel へ遷移。
  * **保護者ボタン (カギのアイコン):** 画面の隅に配置。幼児の誤タップを防ぐため「3秒間の長押し」または「簡単な計算問題の正解」で ReportPanel へ遷移する。
* **ModeSelectPanel (モード選択画面)**
  * **はじめてモード / ブロック道 / おてほんモード:** 3つの大きなボタン。タップで対象のレベルデータを読み込み、InGamePanel へ遷移。
  * **どうぶつ図鑑・シール帳ボタン:** CollectionPanel へ遷移。
* **InGamePanel (ゲームプレイ画面)**
  * **盤面エリア (非UI):** 中央に配置される8×12のゲーム空間。
  * **どうぶつ先生エリア:** 画面上部。プレイ状況に応じて動物のSpriteが跳ねたり吹き出し（「すごい！」等）を出す。
  * **チェンジボタン (魔法のステッキ):** 手持ちブロックの隣に配置。残り回数を星アイコン等で表示。
  * **Undoボタン (Uターンの矢印):** 画面の右下等、誤タップしにくい位置に配置。
  * **ポーズ／もどるボタン:** 画面左上。タイトルまたはモード選択へ戻る。
* **CollectionPanel (図鑑・シール画面)**
  * **タブ切り替えUI:** 「どうぶつ」と「シール」を切り替え。
  * **ScrollView (スクロールビュー):** 獲得済みの動物やシールをグリッド状（GridLayoutGroup）に並べて表示。未獲得のものはシルエット（黒塗り）で表示。
* **ReportPanel (保護者向け：成長レポート画面)**
  * プレイ時間、達成課題数などの統計テキスト表示エリア。

---

### 5.2 ボタン押下時のイベント・デリゲート定義

UIのボタン（`UnityEngine.UI.Button`）の `OnClick` イベントは、Unityのインスペクター上で直接ロジックの深い部分を呼ぶのではなく、一度 `UIManager` または専用のイベント中継用メソッドを噛ませて発火させます。

* **チェンジボタン押下時**
  * **UI側:** `Button.OnClick` -> `UIManager.OnChangeButtonClicked()`
  * **ロジック側:** `UIManager` は内部で `BlockSpawner.Instance.OnChangeButtonPressed()` を呼び出し、ブロック再生成の可否判定と実行を委譲する。
* **Undo（1手戻す）ボタン押下時**
  * **UI側:** `Button.OnClick` -> `UIManager.OnUndoButtonClicked()`
  * **ロジック側:** `GridManager.Instance.UndoLastMove()` を呼び出す。スタックが空（戻せない状態）の場合はUI側でボタンをグレーアウト（`interactable = false`）しておく。
* **図鑑の動物タップ時**
  * 各動物のアイコン（プレハブ）に `AnimalIconUI` スクリプトをアタッチ。
  * **UI側:** `Button.OnClick` -> `AnimalIconUI.PlayAction()`
  * **ロジック側:** `RectTransform` を用いたジャンプアニメーション（DOTweenの `DOPunchPosition` など）を再生し、`AnimalData.voiceClip` を `AudioSource` で再生する。

---

### 5.3 成長レポート画面のデータバインディング仕様

保護者向け画面（ReportPanel）を開いた際、`SaveDataManager` に保存されている `PlayerData` の数値を読み込み、UIのテキストやゲージに反映（バインディング）させます。

* **データの取得と反映のフロー:**
  1. TitlePanel から ReportPanel へ遷移する際、`ReportUIController.UpdateView()` が呼ばれる。
  2. `SaveDataManager.Instance.CurrentData` を参照する。
  3. **プレイ時間の表示:** `PlayerData.totalPlayTimeMinutes` を取得し、「累計：〇〇時間〇〇分」という文字列にフォーマットして `Text` コンポーネントに代入。
  4. **クリア課題数の表示:** `PlayerData.clearedStageCount` を取得し、目標値（次の動物解放までの目安等）と合わせてプログレスバー（`Slider` コンポーネント等）に反映。
  5. **シールの表示:** `PlayerData.unlockedSealIds` をループで回し、`SealData` マスタと照合。動的にシールアイコンのプレハブを生成（`Instantiate`）して台紙（Panel）に配置する。

## 6. 主要シーケンスフロー（処理のロジック詳細）

本章では、ゲーム内で発生する主要なアクションにおいて、各クラス・コンポーネントがどのような順序でメソッドを呼び出し、データを更新していくかの具体的なシーケンス（処理の流れ）を定義します。

### 6.1 フローA：ブロックを掴んでから、スナップして配置が確定するまで

プレイヤーが手持ちのブロックを盤面に配置するまでの、最も基礎的かつ重要なフローです。

1. **タッチ検知 (InputManager):**
   * 画面がタッチされた座標からRaycastを飛ばし、`BlockController` を取得。
   * `BlockController.OnDragStart()` を呼び出す（ブロックが少し浮かぶ演出）。
2. **ドラッグ中 (InputManager -> BlockController):**
   * 毎フレーム、タッチ座標をワールド座標に変換し、`BlockController.OnDragging(targetPos)` を呼び出してブロックを指に追従させる。
3. **ドロップ・配置判定 (InputManager -> BlockController -> GridManager):**
   * 指が離れた時、`InputManager` は `BlockController.OnDragEnd()` を呼び出す。
   * `BlockController` は、自身を構成する4マスの現在座標から、盤面上の仮想グリッド座標 `Vector2Int[4]` を算出。
   * `GridManager.Instance.CanPlaceBlock(算出座標)` を呼び出し、配置可能か（枠外でないか、既存ブロックと重なっていないか）を問い合わせる。
4. **判定結果による分岐 (BlockController):**
   * **[配置不可 (False) の場合]**
     * 座標を無効とし、手持ちエリアの初期座標（`initialPosition`）へアニメーションさせながら戻す。
   * **[配置可能 (True) の場合]**
     * 座標をグリッドのマス目にスナップ（四捨五入してピタッと吸着）させる。
     * `GridManager.Instance.PlaceBlock(...)` を呼び出し、盤面配列 `gridArray` に自身の情報を書き込む。
     * `BlockController` の状態を `Placed` に変更し、操作対象から外す。
     * 直後に「フローB（ライン消去判定）」へ移行する。

---

### 6.2 フローB：ライン消去判定〜盤面データのシフト〜ブロック破棄処理

ブロックの配置が確定した直後に必ず実行される、列のチェックと消去のフローです。

1. **消去対象の走査 (GridManager):**
   * Y軸（0〜11）を下から上へスキャンする。
   * ある行（X軸0〜7）の値がすべて `0` 以外であれば、その行番号を `linesToClear` リストに追加する。
2. **消去演出のトリガー (GridManager -> UIManager / ParticleController):**
   * `linesToClear.Count > 0` の場合、イベント `OnLineCleared(linesToClear.Count)` を発火。
   * `ParticleController` はイベントを受け取り、該当する行の座標に星が弾けるエフェクトを再生する。
   * `UIManager` はイベントを受け取り、どうぶつ先生が喜ぶアニメーションや「すごい！」等のUIを表示する。
3. **盤面データの更新とシフト (GridManager):**
   * `linesToClear` に該当する行の配列データを `0` にクリアする。
   * 消えた行より上にあるすべての行のデータを、消えた行数分だけ下のインデックスへ移動（シフト）させる。
4. **オブジェクト座標の物理的な更新 (GridManager -> 各ブロックのSprite):**
   * 配列データのシフトに合わせて、盤面上に配置済みの各ブロックのマス（Sprite）の `Transform.position.y` を、消去された列数分だけ下に下げる（アニメーションさせる）。
5. **手持ちブロックの補充 (BlockSpawner):**
   * 消去処理の有無に関わらず、配置が確定して手持ちが空になったため、`BlockSpawner.Instance.SpawnRandomBlock()` を呼び出して次のブロックを生成する。

---

### 6.3 フローC：Undo（1手戻す）ボタン押下時の盤面・オブジェクト復元処理

子供が間違えて配置してしまった場合に、直前の状態に安全に戻すためのフローです。

1. **ボタン押下と判定 (UIManager -> GridManager):**
   * Undoボタンがタップされると、`GridManager.Instance.UndoLastMove()` が呼び出される。
   * `GridManager` 内部の履歴スタック `undoStack` が空でないかを確認する。
2. **盤面データの復元 (GridManager):**
   * `undoStack.Pop()` を実行し、直前の `GridStateSnapshot` を取り出す。
   * 現在の `gridArray` を破棄し、スナップショットに保存されていた盤面配列で上書き（復元）する。
3. **ブロックオブジェクトの復元 (GridManager -> BlockController):**
   * スナップショットに記録されていた「直前に配置したブロックのインスタンス参照」を取得する。
   * 該当の `BlockController` の状態を `Placed` から `InHand`（手持ち状態）に戻す。
   * ブロックの `Transform.position` を、手持ちエリアの初期座標へ戻す。
4. **チェンジ（手持ち補充）のキャンセル処理 (BlockSpawner):**
   * フローBの最後に補充されていた「新しい手持ちブロック」が存在する場合は、それを破棄（Destroy）し、UIと盤面の整合性を保つ。
