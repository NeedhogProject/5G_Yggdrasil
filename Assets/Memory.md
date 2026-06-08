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
- 입력: 구버전 `Input.GetKeyDown` → New Input System
- `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()`
- NPC 시스템 Open 메서드에 `isOpen` 가드 추가
- **전체 키 입력 → InputReader 싱글턴 중앙화 (키 리바인딩 호환)**
  - 이동/달리기/공격/상호작용(E)/인벤토리(I)/맵(M)/취소(ESC) 모두 InputReader 경유
  - 하드코딩 키 전부 제거 (마우스 위치값만 직접 사용 — 리바인딩 무관)
  - UIBlocking 플래그로 UI 열림 시 게임플레이 입력 차단

## 다중 칸 인벤토리 (디아블로식) — 5단계 전부 완료
- 아이템 크기(InventoryWidth/Height)만큼 칸 차지
- InventorySlot: ownerSlot 참조, IsOwner 프로퍼티
- InventorySystem:
  - OccupyArea/OccupyAreaOnly: 크기만큼 칸 점유 + 아이콘 확장(ResizeItemIcon)
  - ReleaseArea/RestoreItemIcon: 제거 시 점유 해제 + 아이콘 1칸 복원
  - MoveItem/CanPlaceAt: 드래그 이동 시 다중 칸 충돌 검사, 실패 시 원위치 복원
  - GetSlot, FindInstanceBySlot, ReleaseAreaSilent 헬퍼
- InventorySlot 드래그/우클릭/툴팁 모두 보조 칸이면 주인 슬롯(ownerSlot) 기준 처리
- 우클릭 착용 복사 버그 수정 (착용 전 RemoveItem, 교체 반환은 PlayerEquipment 담당)
- OnDrop null 참조 순서 버그 수정 (CleanupDrag로 통합)

## 등급/방어력 시스템 재정비 (신규)
- 아이템 등급 4단계로 축소: Common / Rare / Epic / Legendary (Uncommon 제거)
  - ItemRarity, WeaponRarity 양쪽에서 제거
  - UIItemTooltip 색상 처리에서 Uncommon 제거
- 기본 방어력 100 고정 (방어구 없어도) — 정신력 패널티 계산 기준
- 방어력 공식 선형화: 방어력 1당 데미지 0.05% 감소
  - 방어력 100 = 5% 감소, 500 = 25%, 1000 = 50%
  - 감소율 상한 95%
- 방어력 상한 제거: 기본 100 + 장비 방어력 (100 초과 가능)
- baseDefense Range/Clamp 제한 제거

## 장비 최대체력 옵션 (신규)
- ArmorData에 maxHealthBonus 필드 추가
- PlayerStats: MaxHealth = 100 + equipmentMaxHealth (동적 최대체력)
- AddEquipmentMaxHealth / RemoveEquipmentMaxHealth 메서드
- 마을(IsInTown)에서 방어구 착용: 최대치 증가 + 현재 체력도 회복
- 던전에서 방어구 착용: 최대치만 증가, 현재 체력 유지
- 방어구 해제 시 최대치 감소 → 현재 체력 상한 보정
- HUD 체력바 max를 MaxHealth로 연동 (고정 100 아님)
- ModifyHealth, UseHealthPotion 상한도 MaxHealth 기준
- 던전에서 만피 상태로 체력 방어구 착용 시 → 새 최대치로 만피 유지 (bWasFull 판정)
  - 만피 아니면 현재 체력 유지 (악용 방지)

## 던전 범위 조정 (2층까지만 구현)
- 모델링 일정상 1~2층만 구현, 3~4층 보류
- StemConnector에 UpOnly 모드 추가 (최하층=2층 전용, 상승만 가능)
- 2층 줄기는 UpOnly로 설정 → 하강 옵션 없음, 상승만
- 단, UpOnly도 열쇠 필요 (무한 파밍 방지)
- 2층 줄기 흐름: 열쇠 삽입 → 상승 연출 → 1층 복귀

## 새게임/이어하기 시스템 (신규)
- 다중 슬롯 (SLOT_COUNT = 3) + 수동 저장 방식
- GameManager.IsNewGame 플래그, CurrentSlot 추적
- StartNewGame(slot): 슬롯 비우고 마을부터 + IsNewGame=true
- ContinueGame(slot): 저장된 층 씬 로드 후 한 프레임 뒤 데이터 복원
- SaveCurrentGame(): 현재 슬롯에 수동 저장 (집/NPC에서 호출)
- StartingEquipment: IsNewGame일 때만 무기 3종 지급 + 장검 장착

## StartingEquipment (신규)
- Player에 부착, 새 게임 시 단검/장검/창 지급, 장검 자동 장착
- 이어하기 시 건너뜀 (세이브가 장비 복원)

## InputReader 싱글턴 (신규)
- GameCore 같은 상시 오브젝트에 PlayerInput과 함께 부착
- DontDestroyOnLoad
- PlayerInputActions 에셋에 액션 필요: Move, Sprint, Attack, Interact, Inventory, Map, Cancel
- PlayerController/PlayerCombat은 InputReader에서 입력 읽음 (자체 콜백 제거됨)
- 이동은 카메라 기준 방향 (camForward/camRight 투영)

---

## 마을/UI/인벤토리 추가 작업 완료 (이번 세션)
- **NPCInteractable**: 마을 NPC 상호작용 단일 컴포넌트 (상인/대장장이/각인술사 타입 라우팅 + 힌트 표시). 기존 ShopNPCInteractable 대체 (삭제 가능)
- **BlacksmithSystem / InscriptionMasterSystem**: ShopSystem 스타일 시작 메뉴 이식
  - 대장장이: 강화하기 / 대화하기
  - 각인술사: 각인하기 / 각인 초기화 / 대화하기
  - 공통: talkLines 순환 대사 + ESC 단계 처리(작업창→메뉴→닫기)
- **HouseDoorInteractable**: 같은 씬 내 집 입구↔실내 텔레포트 (targetPoint 로 플레이어 위치 이동, 씬 전환 아님)
- **인벤토리 드래그 버그 수정 (InventorySlot/InventorySystem)**
  - 드래그 아이콘 앵커를 중앙(0.5,0.5)으로 → 마우스 위치 일치
  - 드롭 시 잡은 위치 보정: _grabOffset 기록 후 (드롭칸 - offset)으로 주인 칸 역산 (GetSlotAt 헬퍼 추가)
- **ShopSystem 컴파일 에러 수정**: InventorySystem 에 RemoveItemAtSlot(InventorySlot) 추가 (슬롯 단위 정확 제거)
- **UIItemTooltip**
  - 깜빡임 해결: Awake 에서 CanvasGroup.blocksRaycasts=false 강제 (자식 텍스트/아이콘까지 한 번에)
  - 위치를 커서 우측 하단으로: 피벗 좌상단(0,1) 고정 + ClampToScreen 그에 맞춤 + offset (15,-15)
- **EquipmentSlotUI**
  - 빈 슬롯 실루엣 플레이스홀더(emptySlotIcon, emptySlotAlpha) / 장착 시 풀컬러 아이콘
  - PlayerEquipment.OnEquipmentChanged 구독 → 장착/해제 시 슬롯 UI 자동 갱신 (장착 표시 + 호버 툴팁 정상 동작)
- **인벤토리 아이콘 칸 채움**: ResizeItemIcon 에서 preserveAspect=false (여백/격자처럼 보이던 현상 제거, 멀티셀 격자선과는 별개)

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

### 인벤토리 멀티셀 표시 — 방식 A 확정 (추후 구현)
- 문제: 멀티셀 아이템 아이콘이 보조 칸 배경/테두리 뒤로 깔려 내부 격자선이 비침 (GridLayoutGroup 렌더 순서)
- **결정: 오버레이 레이어 방식(A)** 으로 진행
  - 그리드 위에 별도 IconOverlay RectTransform 배치 (그리드 다음 형제, GridLayoutGroup 없음)
  - 아이템 아이콘을 칸의 자식이 아니라 오버레이에서 그림 → 항상 모든 칸 위에 렌더
  - 위치: 주인 칸 그리드 좌표 → 셀 크기/간격으로 환산 (ResizeItemIcon 계산 재활용)
  - InventorySystem 배치/이동/제거 경로 + InventorySlot 일부 수정 필요 (중간 규모)
- 임시 조치만 적용됨: preserveAspect=false (단일 칸 채움). 격자선 자체는 오버레이 구현 시 해결 예정

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
