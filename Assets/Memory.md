# Memory.md

프로젝트 작업 메모리. 더 이상 수정할 필요 없는 완성 항목 기록.

---

## 프로젝트 환경
- 엔진: Unity 6000.3.10f1
- 렌더 파이프라인: Built-in
- 입력: Unity New Input System
- 언어: C#

---

## 완성된 시스템

### 1. 핵심 인프라
- **GameCore 오브젝트**: `GameManager`, `AudioManager`, `SaveSystem`, `FloorManager`
- **DontDestroyOnLoad** 적용: GameManager, HUDManager
- **싱글턴 패턴**: GameManager, AudioManager, PlayerStats, PlayerController, InventorySystem, HUDManager, UIItemTooltip, FloorManager, ResourceInventory

### 2. 플레이어 시스템
- **이동**: WASD (월드 축 기준)
- **회전**: 마우스 커서 방향
- **달리기**: Shift 홀드 (`Keyboard.current.leftShiftKey.isPressed`)
- **공격**: 마우스 좌클릭, UI 위에서는 차단됨
- **사망/리트라이**: GameOverPanel UI 연동 완료

### 3. HUD
- 체력 슬라이더 (위험 시 빨간색)
- 정신력 슬라이더 (위험 시 마젠타)
- PlayerStats 이벤트로 자동 갱신

### 4. 인벤토리
- 80칸 격자형 슬롯
- 장비/자원 탭 전환
- **드래그앤드롭**으로 아이템 이동/스왑
- 우클릭으로 장착/사용
- I키 열기/닫기, ESC 닫기, 바깥 클릭 닫기
- 게임 일시정지 연동 (timeScale)

### 5. 아이템 시스템
- ItemData (camelCase 별칭 포함)
- WeaponData, ArmorData, EquipmentData, RelicData, ResourceData, ConsumableData
- ItemInstance, WeaponInstance, ArmorInstance
- DroppedItem (E키 줍기, testItemData 슬롯)
- LootTable (DroppedItem 연동)
- ResourceDropEnemy (원소 몬스터만 자원 드랍)

### 6. 툴팁
- UIItemTooltip (마우스 따라다님)
- Raycast Target 비활성화로 깜빡임 해결
- New Input System 적용

### 7. 카메라
- CameraFollow (Lerp 부드러운 추적)

---

## 코드 스타일 규칙 적용 완료 파일

CLAUDE.md 기준 `var 금지`, `if (!변수)` 금지 적용 완료:

### 플레이어 시스템
- PlayerStats.cs
- PlayerController.cs
- PlayerCombat.cs
- PlayerEquipment.cs
- PlayerDeath.cs

### 인벤토리/UI
- InventoryUI.cs
- InventorySystem.cs
- InventorySlot.cs
- UIItemTooltip.cs
- HitboxSystem.cs
- HUDManager.cs

### 던전 시스템
- StemManager.cs
- StemConnector.cs
- StemAscender.cs
- YggdrasilPortal.cs
- EnemySpawner.cs
- EnemyAI.cs
- EnemyBase.cs
- DungeonDifficultyScaler.cs

### 방어구/세트 시스템
- ArmorSetManager.cs
- ArmorInstance.cs
- ArmorData.cs

### 아이템 시스템
- LootTable.cs
- DroppedItem.cs
- ResourceNode.cs
- ResourceDropEnemy.cs
- ItemData.cs
- ItemInstance.cs
- ResourceInventory.cs

### 시스템/UI
- SaveSystem.cs
- AudioManager.cs
- HouseSystem.cs
- TownMapUI.cs
- InscriptionMasterSystem.cs
- EquipmentSlotUI.cs

**프로젝트 전체 코드 스타일 정리 완료 (0건 잔여)**

---

## 미정의 클래스/enum 정의 완료
- GameEnums.cs (ItemType, ItemRarity, InscriptionType, EquipmentType)
- SetSignature.cs
- FloorManager.cs
- EquipmentData.cs
- RelicData.cs
- ResourceInventory.cs
- ShopItemUI.cs

---

## 일괄 작업 완료
- AudioManager 호출: `PlaySFX(string)` → `PlaySFX(SFXClip.XXX)` enum 방식
- 입력: 구버전 `Input.GetKeyDown` → New Input System (`Keyboard.current`)
- `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()`
- NPC 시스템 Open 메서드에 `isOpen` 가드 추가

---

## 버그 수정 기록
- HUDManager.StartSanityPulse: `vignetteImage` 미할당 시 NullReferenceException 폭증 문제 수정 (가드 추가, HUDManager.cs)
- InitBase.cs: `MonoBehaviour` 상속 누락으로 Awake 가 호출되지 않던 문제 수정 (베이스 클래스 동작 복구)
- 싱글턴 초기화 순서 경합: `PlayerStats` 에 `[DefaultExecutionOrder(-100)]` 적용으로 HUDManager 보다 먼저 Awake 보장
- 매니저 간 직접 결합 해소: `PlayerStats` 가 `HUDManager.Instance` 를 직접 호출하던 구조를 이벤트 구독 기반으로 역전 (PlayerStats 는 UI 를 모름)
- HUDManager 에 `OnDestroy` 추가: PlayerStats 이벤트 구독을 안전 해제하고 `Instance` 를 정리 (씬 전환 시 데드 참조 방지)
- `PlayerStats.MAX_STAT` public const 화: HUDManager 가 동일 최대값을 사용하도록 일원화
- `ItemInventoryUI.cs` 삭제: Unity 기본 템플릿 빈 스텁, 코드/씬/프리팹 어디에서도 참조되지 않은 고아 파일
- `ResourceInventory.GetResourceAmount` 제거: `GetResourceCount` 단순 래퍼였으며 호출자(`ResourceInventoryPanel`)를 `GetResourceCount` 로 통일

---

## 추가 점검 필요 (요청 시 처리)
- `Assets/Scripts/Debug/TestWeaponEquip.cs`: 시작 시 무기 자동 장착 테스트 스캐폴드. 출시 전 제거 검토.
- `StemManager.RerollKeys()`: `DistributeKeys()` 의 public 래퍼. C# 호출자 0건이라 사용처 확인 필요 (UI Button OnClick 바인딩일 가능성).

---

## 폴더 구조 재편 (Assets/Scripts)

64개 평면 배치를 10개 카테고리 서브폴더로 분류. `.cs` 와 `.cs.meta` 를 함께 이동해 GUID 보존 → 씬/프리팹/인스펙터 참조 무변경.

```
Assets/Scripts/
├── Core/        GameManager, AudioManager, SaveSystem, InitBase, GameEnums                                    (5)
├── Player/      PlayerController, PlayerCombat, PlayerEquipment, PlayerStats, PlayerDeath, HitboxSystem        (6)
├── Data/        ItemData, WeaponData, ArmorData, EquipmentData, RelicData, ResourceData,
│                ConsumableData, FloorKeyData, SeteffectData, SetSignature                                     (10)
├── Items/       ItemInstance, WeaponInstance, ArmorInstance, ConsumableResourceInstance,
│                DroppedItem, LootTable, ResourceNode                                                           (7)
├── Inventory/   InventorySystem, InventorySlot, InventoryUI, ItemInventoryPanel,
│                ResourceInventory, ResourceInventoryPanel                                                      (6)
├── Combat/      EnemyAI, EnemyBase, EnemySpawner, ResourceDropEnemy                                            (4)
├── Dungeon/     FloorManager, DungeonDifficultyScaler, StemManager, StemConnector,
│                StemAscender, YggdrasilPortal                                                                  (6)
├── NPCSystems/  BlacksmithSystem, EnhancementSystem, HouseSystem, ShopSystem, ScholarSystem,
│                RuneInscriptionSystem, InscriptionMasterSystem, ArmorSetManager, NPCDialogue,
│                CoinFlipUI, ShopItemUI                                                                        (11)
├── UI/          HUDManager, MinimapUI, TownMapUI, EndingUI, UIItemTooltip,
│                EquipmentSlotUI, InscriptionColorHelper                                                        (7)
├── Camera/      CameraFollow                                                                                   (1)
└── Debug/       TestWeaponEquip                                                                                (1)
```

분류 결정 사유:
- `Data` 와 `Items` 분리: `*Data.cs` 는 ScriptableObject 정의(에디터 에셋 생성용), `*Instance.cs` 는 런타임 상태 객체
- `NPCSystems` 에 `ShopItemUI`, `CoinFlipUI`, `NPCDialogue` 포함: 단순 UI 가 아니라 NPC 기능과 강결합
- `Combat` 에 `EnemyAI`, `EnemyBase`, `EnemySpawner`, `ResourceDropEnemy`: 적 행동/스폰만 분리. 플레이어 전투 로직은 `Player` 안에 둠 (`PlayerCombat`, `HitboxSystem`)
- `Camera`, `Debug` 단일 파일이지만 의도(테스트/카메라 전용)가 다르므로 별도 폴더로 두어 향후 추가 시 자연스럽게 누적

---

## 씬 구성 완료
- Town 씬
  - GameCore
  - Player (프리팹)
  - YggdrasilPortal
  - Inventory_Canvas (인벤토리 UI 전체)
  - HUD_Canvas (체력/정신력 바)
  - GameOverCanvas
  - 임시 Ground (Plane)
  - 받은 배경 모델링 적용 중

---

## ScriptableObject 에셋 생성 완료
- WeaponData (단검/장검/창)
- ResourceData (5종: 불/물/바람/땅/어둠)
- ConsumableData (체력물약)

---

## 아직 미완성 (다음 작업)

### 다음 우선순위 작업
1. **장비 장착 UI** — EquipmentSlotUI Canvas 배치
2. **던전 씬** — Floor_1 ~ Floor_4_Boss
3. **적 AI / 스폰** — EnemySpawner, EnemyBase 씬 배치
4. **NPC 시스템** — Blacksmith, Scholar, InscriptionMaster, Shop UI Canvas
5. **줄기/열쇠 시스템** — 던전 층 이동
6. **미니맵**

### 그래픽 작업 (개발 후반)
- 캐릭터 모델링 (현재 Capsule 임시)
- 오디오 클립 연결 (AudioManager의 SFX/BGM)
- 툰 셰이더 적용 검토

---

## 주의사항

- `PlayerCombat`에서 `RequireComponent(typeof(HitboxSystem))` 제거됨 → HitboxSystem은 HitboxChild 자식 오브젝트에 부착
- `TooltipPanel`의 Image와 자식 모두 **Raycast Target 체크 해제** 필수 (깜빡임 방지)
- `OutsideCloseButton`은 Hierarchy에서 `InventoryPanel`보다 **위에 있어야** 함
- 받은 씬 사용 시 중복 Audio Listener / Camera 제거 필요
