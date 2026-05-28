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

## 씬 구성 완료
- Town 씬
  - GameCore (Main Camera 자식으로 포함, DontDestroyOnLoad)
  - Player (프리팹)
  - YggdrasilPortal
  - Inventory_Canvas (인벤토리 UI 전체)
  - HUD_Canvas (체력/정신력 바)
  - GameOverCanvas
  - 임시 Ground (Plane)
  - 받은 배경 모델링 적용 중

- Floor_1 씬 (완성)
  - 바닥(Plane) + NavMesh 굽기 완료
  - 임시 Main Camera (CameraFollow + DevTestCamera)
  - Player 배치
  - DungeonCore (프리팹화) — DungeonDifficultyScaler + EnemySpawner
  - StemManager + 줄기 4방향 (North/South/East/West)
  - SpawnPoint 배치 + EnemySpawner 연결
  - ResourceNode 배치
  - 진입/스폰/열쇠/층이동 테스트 완료

---

## ScriptableObject 에셋 생성 완료
- WeaponData (단검/장검/창)
- ResourceData (5종: 불/물/바람/땅/어둠)
- ConsumableData (체력물약)

---

## 아직 미완성 (다음 작업)

### 다음 우선순위 작업
1. **Floor_2 ~ Floor_4_Boss 씬** — Floor_1 복제 후 수치 변경
2. **적 프리팹** — 2~4층 적 (현재 1층만 완성)
3. **보스 (니드호그)** — 4층
4. **NPC 시스템 연동** — 코드 완성, UI는 별도 담당
5. **미니맵 / 마을맵** — 코드 완성, UI는 별도 담당
6. **세이브/로드 검증**

### UI 작업 (별도 담당 - 개발 외)
- 장비 장착 UI, NPC 창들, 미니맵, 타이틀, 세이브 슬롯 등

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
- **카메라 구조**: Main Camera는 GameCore 자식으로 DontDestroyOnLoad 유지. 던전 씬에는 별도 카메라 두지 않음. 단독 테스트용으로 DevTestCamera 부착 시 GameCore 카메라 진입하면 자동 제거됨
- **CameraFollow**: 씬 전환 시 target 자동 재탐색 (PlayerController.Instance → Tag "Player" 순)
- **DungeonCore 프리팹화 완료**: Floor_2~4에서 재사용, DifficultyScaler 수치만 변경
- FloorKey 4개 ScriptableObject 완성 (North/South/East/West)
- 1층 적 프리팹 전부 완성
