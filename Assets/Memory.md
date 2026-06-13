# Memory.md

프로젝트 작업 메모리. 더 이상 수정할 필요 없는 완성 항목 기록.

---

## 동기화본 미반영분 추가 적용 (2026-06-14)
앞서 패치로 드렸으나 동기화본에 일부만 적용돼 있던 3건을 동기화본 기준 재적용:
- InventorySystem.UseItem: Consumable 분기가 item.UseItem()+무조건 RemoveItem 이었음 (물약 회복 안 되고 소모만)
  - consumable.TryUse(PlayerStats.Instance) 성공 시에만 제거, ResetScroll 우클릭 차단으로 수정
- SpawnPoint.SelectRandomPrefab: 층 필터 미적용이었음 (자원 몬스터 1층 미소환 핵심 방어선)
  - IsAvailableOnFloor 추가, 현재 층 등장 불가 프리팹 제외 후 가중치 재계산
- EnemySpawner.SpawnEnemy: spawnCount 는 SpawnPoint 에 있었으나 스포너가 1마리만 생성했음
  - while (HasSpawned == false) 다중 스폰, maxSpawnCount 도달 시 중단, 첫 마리만 열쇠 후보
- 참고: ConsumableData 물약%, SpawnPoint.spawnCount/MarkSpawned, ResourceDropEnemy SetActive 제거, GameManager SyncFloor 는 동기화본에 이미 반영 확인됨

## 강화/정신력/가격/데스 패치 적용 완료 (2026-06-13, 동기화본 기준 전체 파일 출력)

### 강화 (WeaponData/WeaponInstance/EnhancementSystem)
- WeaponData: enhanceSuccessRates/attackMultipliers/speedMultipliers 를 SerializeField 배열로 노출 (밸런스 조정 가능) + 외부 프로퍼티
- WeaponInstance: 틀린 static 표 제거, WeaponData 배열 참조로 FinalDamage/FinalAttackSpeed/CurrentSuccessRate 재작성
  - 기존 버그: 확률 {100,80,60,40,20}, 공격력 1강당 10% 선형 — 둘 다 기획과 달랐음. 이제 WeaponData 단일 출처
- TryEnhance: 0~3강 실패 변동 없음(EnhanceResult.Fail 신규), 4강 실패만 1강 하락(Downgrade)
- EnhanceResult enum 에 Fail 추가, ResetToBase 는 미사용 잔존(죽은 코드)
- EnhancementSystem: Fail 케이스 메시지 추가, 4강 경고 문구 '초기화' -> '1강으로 하락'

### 정신력 패널티 (PlayerStats/PlayerCombat/PlayerController)
- PlayerStats: mentalAttackPenalty/mentalDefensePenalty/mentalMoveSpeedPenalty 인스펙터 필드 (0~1)
  - MentalAttackMultiplier, MentalMoveSpeedMultiplier 프로퍼티 + EffectiveDefense 계수화
  - TakeDamage 의 받는 피해 증가(2f - MentalMultiplier) 제거 (방어력 감소와 이중 적용이라)
- PlayerCombat.CalculateDamage: MentalMultiplier -> MentalAttackMultiplier
- PlayerController: CurrentSpeed/Move 에 MentalSpeedFactor(=MentalMoveSpeedMultiplier) 곱

### PlayerController 에 섬/낙하 복귀도 재반영
- 동기화본은 섬 차단/낙하 복귀가 없는 원본이었음 — 이번에 함께 재적용
- bBlockCliffEdges/edgeCheckDistance/edgeRayLength + FilterCliffDirection/HasGroundAhead
- fallYThreshold/safeRecordInterval + RecordSafePosition/CheckFallRespawn
- 주의: groundLayer 기본 ~0 이면 허공도 지면 — Floor_2 는 Ground 레이어로 좁혀 지정 필수

### 가격 통일 A방식 (ItemData/ShopItemUI)
- ItemData: _buyPrice/_sellPrice -> _price 단일. Price/BuyPrice(=_price)/SellPrice(=_price*0.3)/basePrice(=_price), SELL_RATIO=0.3
- ShopItemUI.SetupSellItem: basePrice/2 -> item.SellPrice
- ShopSystem.GetSellPrice 의 sellPriceRatio 는 0.3 확인됨 (표시/정산 일치)
- 주의: 필드명 변경으로 기존 ItemData 에셋 가격값 초기화 — 에셋마다 price 재입력 필요

### 사망 패널티 (PlayerDeath/ResourceInventory)
- PlayerDeath.ApplyDeathPenalty 활성화: 장비(UnequipAll) + 인벤(GetAllInstances 역순 RemoveItem) + 자원(ClearAll) 전부 유실
- ResourceInventory.ClearAll 신규 (키 복사 후 0 초기화)
- 인벤 칸은 유지, 내용물만 삭제. 자원 포함 전부 유실 확정
- UI: 사망 후 ResourceInventoryPanel.Refresh 호출 경로 확인 필요 (김보민)

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
- **달리기**: Shift 홀드 (InputReader.SprintHeld 경유, 리바인딩 호환)
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
- CameraFollow (Lerp 부드러운 추적, 씬 전환 시 target 자동 재탐색)
- CameraFollow.SnapToTarget (텔레포트 직후 즉시 정착 — 집 출입 페이드 연출용)

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

## 플레이어 영속 단위 — 완료 (2026-06)
- 결정: 플레이어 + 인벤토리 + 자원을 하나의 영속 단위로 (씬 전환 시 유지, 인벤토리 유지 확인 완료)
- DontDestroyOnLoad: PlayerController, InventorySystem, ResourceInventory
  - DDOL 호출 전 부모가 있으면 자동 루트 분리 + 경고 로그 (루트가 아니면 DDOL 실패하므로)
  - OnDestroy 에서 static Instance 정리 (파괴된 객체 참조 방지)
- SaveSystem 역할 구분: 런타임 씬 전환 유지는 DDOL, 세션 간 저장/불러오기는 SaveSystem 슬롯
- GameManager.DestroyPersistentPlayerObjects(): StartNewGame / ContinueGame / GoToTitle 에서 호출
  - **반드시 DestroyImmediate 사용** — 지연 Destroy 는 새 씬 싱글턴 Awake 가 살아있는 이전 인스턴스를 보고 자기 파괴함 (인벤토리 미생성 버그의 원인이었음)
- StartingEquipment: 인스턴스 등록 + 슬롯 생성(slots.Count > 0)까지 대기 후 지급
  - 인스턴스만 보고 즉시 지급하면 슬롯 0개 상태라 "가득 참" 오판 발생 (수정 완료)
- 스폰 보정: MovePlayerToSpawn(이름) 으로 일반화
  - 마을 복귀: Spawn_TownCenter / 던전 진입·층간 이동: Spawn_DungeonStart (각 던전 씬에 배치 필요)
  - 영속 플레이어는 이전 씬 위치를 들고 오므로 씬마다 스폰 지점 필수
- 주의: 각 씬에 Player/Inventory_Canvas/ResourceInventory 를 **루트 오브젝트로** 배치 유지 (중복 진입 시 싱글턴 가드가 자기 파괴)

## 세이브/로드 아이템 복원 — 완료 (리포 반영 버전, 2026-06-11 코드 기준 갱신)
- 아이템 조회: SaveSystem.itemDatabase(List<ItemData>) + FindItemData
  - 에디터 ContextMenu '아이템 데이터베이스 자동 수집' (Assets/ScriptableObjects 에서 t:ItemData 검색)
  - 새 ItemData 에셋 추가 시 재수집 필수 (안 하면 복원 시 경고 로그와 함께 누락)
  - 이전 기록의 ItemDataRegistry 싱글턴 방식은 현재 리포에 없음 (itemDatabase 방식으로 확정)
- DeserializeItem: Weapon(RestoreEnhancementLevel)/Armor(SetRune 단일 — None 이면 생략)/일반 + 스택 복원
- 복원 순서: ClearInventory 후 AddItem 순서 배치 / 장비 EquipItem / 창고 AddToStorage
- SaveData.saveName + Save(슬롯, 이름) 오버로드 + RenameSave — 저장 슬롯 UI(SaveSlotPanelUI/SaveSlotCardUI, 김보민) 연동 완료
- SLOT_COUNT = 3 (slot_0.json ~ slot_2.json)
- 한계: 슬롯 위치(slotX/Y)는 무시하고 저장 순서대로 빈 칸 자동 배치
- 한계: playerX/Y/Z 는 저장만 하고 복원은 스폰 지점(MovePlayerToSpawn) 방식
- GameManager.DestroyPersistentPlayerObjects 리포 반영 확인 (StartNewGame/ContinueGame/GoToTitle, DestroyImmediate) — 미해결 항목 해소
- 잔정리 확인 완료(2026-06-12): runeSlot2 는 이미 없음(runeSlot1 단일), 헤더 주석 '슬롯 3개', itemDataName 주석 'itemDatabase 조회 키' 로 정정됨
- ItemDataRegistry.cs 는 리포에 존재하지 않음 확인 (삭제 대상 없음)
- 참고(미정리, 요청 시): SetSignature.cs 가 Slot1/Slot2 2슬롯 구조 유지 — 각인 1슬롯 확정과 불일치 가능성, ArmorInstance 참조 여부 확인 후 판단

## Floor_2 섬 지형 이동/진입 차단 (2026-06-12)
- 맵 구조: 떠 있는 섬 + 다리, 사이는 허공 (플레이어는 Rigidbody 이동이라 NavMesh 로 못 막음)
- 섬 위 이동: 섬/다리 FBX Generate Colliders + Ground 레이어 지정 + Static + NavMesh 재굽기 (에디터 작업)
  - 적은 NavMesh 가 섬/다리 위에만 구워지므로 자동 차단
- 허공 진입 차단: PlayerController 에 레이캐스트 가장자리 차단 추가
  - blockCliffEdges / edgeCheckDistance(0.6) / edgeRayLength(3) / groundMask 필드
  - Move 에서 FilterCliffDirection: 진행 방향 앞 지면 검사, 없으면 X/Z 축별로 걸러 가장자리 슬라이드
  - 보이지 않는 벽 배치 불필요 — 모든 섬 가장자리 자동 적용, 전 씬 공통 (마을 바닥도 Ground 레이어 필요)
  - 주의: groundMask 미지정(Nothing)이면 전 방향 이동 불가
- 낭떠러지 차단/지면 판정/낙하 복귀 모두 기존 groundLayer 필드 공유 (groundMask 따로 안 둠)
  - groundLayer 기본값 ~0(Everything) 이면 허공도 지면으로 잡힘 - Floor_2 는 Ground 레이어로 좁혀 지정 필수
- 낙하 복귀: IsGrounded 일 때 0.5초마다 안전 위치 기록, Y < fallYThreshold(-10) 면 마지막 안전 위치+0.5 로 복귀
  - 넉백/콜라이더 빈틈 추락 대비 안전망 (무한 낙하 소프트락 방지)
- tree-s2.fbx 임포트 경고: Circle.002/012 자기교차 폴리곤 폐기 — 무해, 모델링 담당에게 면 정리 후 재출력 요청

## 집 출입 페이드 연출 (2026-06-12)
- ScreenFader 신규 (싱글턴, GameCore 부착): 런타임에 FadeCanvas(sortingOrder 999) + 검은 Image 자동 생성
  - FadeOutIn(액션): 어두워짐, 암전 시점에 액션 실행, 다시 밝아짐. unscaledDeltaTime 사용 (일시정지 무관)
  - IsFading 으로 중복 실행 방지 (E 연타 가드)
- CameraFollow.SnapToTarget() 추가: 텔레포트 직후 카메라를 즉시 도착 위치로 (Lerp 가로지름 방지)
- HouseDoorInteractable.Teleport: 페이드 경유로 변경, 실제 이동은 MovePlayer 로 분리
  - 플레이어 참조를 페이드 시작 시점에 캡처 (페이드 중 트리거 이탈로 _playerObj null 돼도 안전)
  - ScreenFader 없으면 기존처럼 즉시 이동 (폴백)
- 연출 방식 결정: 검은 화면으로 카메라 이동을 가리는 게 아니라, 암전 동안 카메라를 스냅해서
  페이드 인 때 이미 정착된 화면이 보이게 함

## 1층 자원 몬스터 미소환 문제 수정 (2026-06-12)
- 증상: 던전 1층에서 자원 몬스터(ResourceDropEnemy)가 안 보임
- 원인 구조: ResourceDropEnemy.ValidateFloor 가 Awake 에서 층 검사 후 SetActive(false)
  - 스폰은 되지만 생성 직후 스스로 꺼져서 미소환처럼 보임
  - 콘솔 경고의 층 숫자로 원인 구분: 0 = CurrentFloor 미동기화 / 1 = 프리팹 availableFloors 에 1 누락 / 경고 없음 = SpawnPoint 미등록
- 구조 결함 수정 (원인과 별개로 적용):
  1. GameManager.HandleSceneLoaded 첫 줄에 SyncFloor(scene.name) — 모든 진입 경로에서 층 동기화 보장
  2. SpawnPoint.SelectRandomPrefab 에 층 필터 — 스폰 전에 등장 불가 프리팹 제외 (IsAvailableOnFloor, 일반 몬스터는 제한 없음)
     기존 SelectRandomPrefab 메서드 전체를 교체 + IsAvailableOnFloor 신규 추가
  3. ResourceDropEnemy.ValidateFloor: SetActive(false) 제거, 경고 로그만
- SetActive(false) 를 없앤 이유: 스포너가 이미 AliveEnemyCount 증가 + OnDied 등록을 마친 뒤라
  비활성 몬스터는 영원히 안 죽어 잔여 카운트 영구 인플레이션, 열쇠 소유 적이면 열쇠 증발로 층 진행 불가
- 부수 효과: 한 SpawnPoint 에 여러 층용 몬스터를 같이 등록해도 층마다 자동으로 걸러짐

## 스폰 포인트당 다중 스폰 기능 (2026-06-11)
- 기존: 포인트당 1마리 고정 (의도된 설계 — 마릿수는 포인트 개수로 조절하는 구조였음)
- SpawnPoint.spawnCount 필드 추가 (기본 1, 기존 씬 무변경 동작)
  - HasSpawned 를 카운터 기반(SpawnedCount >= spawnCount)으로 변경, 이름/시그니처 유지로 참조처 무수정
  - MarkSpawned(null) 은 전부 소진 처리 (프리팹 없는 포인트 재시도 방지, 기존 동작 유지)
- EnemySpawner.SpawnEnemy: 발동 시 남은 수를 한 번에 전부 스폰 (마리마다 프리팹 가중치 재선택)
  - maxSpawnCount 도달 시 중단, 다음 체크에서 이어서 스폰 (카운터 기반이라 자동)
  - 열쇠 포인트는 첫 마리만 열쇠 소유 후보 (포인트당 열쇠 1개 규칙 유지)
- 에디터 참고: 다수 스폰 포인트는 spawnSpreadRadius 3~4 권장 (겹침 방지)
- 권장 교정: DungeonDifficultyScaler.totalSpawnCount 툴팁을 'spawnCount 합과 일치'로

## 물약 회복 방식 수정 (2026-06-11)
- 체력 물약: 절대값 -> 최대 체력 대비 % 회복 (장비로 최대치가 늘어도 비율 유지)
  - ConsumableData.effectAmount 기본값 30 유지 (최대 체력 100 기준이면 기존과 동일)
- 정신력 물약: 절대값 유지 (결정 사항 — effectAmount 가 정신력 물약에서는 포인트 의미)
- 만피 판정 하드코딩 수정: Health >= 100f 를 stats.MaxHealth 기준으로, Mental 도 MaxMental 기준으로
  - 기존엔 최대 체력이 100 초과 시 체력 100 이상이면 물약 사용 불가였음 (실질 차단 버그)
- 우클릭 사용 경로 연결(수정 완료): 기존엔 InventorySystem.UseItem 의 Consumable 분기가 미구현 ItemData.UseItem() 을 호출하고
  무조건 제거하던 것을 ConsumableData.TryUse(PlayerStats.Instance) 성공 시에만 제거하도록 수정
  - 초기화 주문서(ResetScroll)는 우클릭 사용 차단 (각인술사 NPC 전용 — 기존엔 우클릭 시 효과 없이 소모됨)
- PlayerStats.UseHealthPotion (healthPotionAmount) 도 % 해석으로 통일, UseMentalPotion 은 절대값 유지
  (현재 ContextMenu 테스트 외 호출처 미확인 — 의미만 맞춰 둠)
- 남은 한계 (이번 범위 제외): 스택 물약은 1개 사용 시 슬롯 전체 제거 (현재 AddItem 이 스택 병합을 안 해서 실사용 영향 적음),
  우클릭 사용이 ItemData 기준 제거라 동일 물약 여러 슬롯 시 다른 슬롯이 제거될 수 있음

## 인벤토리 멀티셀 오버레이 (방식 A) — 완료
- 문제였던 것: 멀티셀 아이콘이 보조 칸 배경 뒤로 깔려 내부 격자선이 비침 (GridLayoutGroup 렌더 순서)
- 구현: InventorySystem.iconOverlay (인스펙터 연결) — 아이콘을 오버레이 레이어로 옮겨 모든 칸 위에 렌더
  - ResizeItemIcon: 오버레이로 이동 + 주인 칸 월드 좌상단 정렬 + raycastTarget=false
  - RestoreItemIcon: 제거/이동 시 원래 슬롯으로 복귀
  - RefreshIconPositions: 탭 활성화 시점에 전체 아이콘 위치 재계산 (InventoryUI.SwitchTab 에서 호출)
- 씬 셋업: IconOverlay 는 ItemInventoryPanel 자식, SlotContainer 다음 형제, 레이아웃 컴포넌트 금지
- 슬롯 배경 Image 에 Raycast Target 필수 (아이콘이 입력을 안 받으므로)

---

## 가격 통일 / 데스 패널티 / 물약 (2026-06-13)

### 물약 % 회복 — 결정 확정
- 기획상 체력 최대 100 이었으나 방어구로 최대 체력이 늘어 절대값 회복은 밸런스 붕괴
- 체력 물약은 최대 체력 대비 % 회복으로 확정 (이미 코드 반영, ConsumableData.TryUseHealthPotion)
- 정신력 물약은 절대값 유지 (앞서 확정)

### 아이템 가격 단일화 (A방식)
- ItemData: _buyPrice/_sellPrice 두 필드 -> _price 단일 필드
  - Price 프로퍼티, BuyPrice(=_price 호환), SellPrice(=_price*0.3 계산), basePrice(=_price)
  - SELL_RATIO 상수 0.3 (기획: 재판매 = 구매가 x 0.3)
- ShopItemUI.SetupSellItem: basePrice/2 하드코딩 -> item.SellPrice (0.3배 표시)
- 상점 구매가(ShopItemData.buyPrice)는 유지 — 상점이 마진 붙여 파는 구조 (A방식 범위)
- ShopSystem.GetSellPrice 의 sellPriceRatio 는 0.3 인지 확인 필요 (표시/정산 일치)
- 주의: 필드명 변경으로 기존 ItemData 에셋 가격값 초기화됨 — 에셋마다 price 재입력 필요 (엑셀 정리표 활용)

### 사망 패널티 — 보유 아이템 전부 유실 확정
- PlayerDeath.ApplyDeathPenalty 활성화 (기존 인벤 삭제가 ClearAll 미존재로 주석 처리돼 있었음)
- 장착 장비: PlayerEquipment.UnequipAll (반환분 미사용 = 소실)
- 인벤토리: GetAllInstances 역순 RemoveItem (칸은 유지, 내용물만 비움)
- 자원(각인 재화): ResourceInventory.ClearAll 신규 추가 후 호출 (자원도 유실 확정)
- UI: 사망 후 ResourceInventoryPanel.Refresh 호출 경로 확인 필요 (김보민 UI 담당)

## 기획 확정/충돌 정리 및 강화 점검 (2026-06-13)

### 세트 효과 — 확정 (작업 종료)
- 원소 데미지 추가 폐기 확정. 속성별로 오르는 스탯이 다른 방식으로 확정
- SetEffectData.cs 헤더 확정표가 이 방향이라 세트 효과 코드 추가 작업 없음
- 주의: 세트_효과.docx 와 게임_구조.txt 는 원소 데미지/중첩 기준 옛 서술 — 코드(확정표)가 정본

### 정신력 패널티 — 확정
- 적용 대상: 공격력, 방어력, 이동속도 감소만 (받는 피해 직접 증가는 제외)
- 감소 강도는 PlayerStats 인스펙터 필드로 노출 (mentalAttackPenalty/mentalDefensePenalty/mentalMoveSpeedPenalty)
- 기존 TakeDamage 의 (2f - MentalMultiplier) 받는 피해 증가는 제거 예정 (방어력 감소와 이중 적용이라)
- 이동속도 패널티는 신규 연결 (PlayerController.CurrentSpeed/Move 에 MentalMoveSpeedMultiplier 곱)

### 방어구 강화 — 확정
- 방어구 강화 시스템 없음. 강화는 무기 전용, 방어구는 각인 전용

### 무기 강화 — 이미 구현됨 (누락 아님), 단 수치 버그 3건
- 구현 위치: WeaponInstance.TryEnhance + EnhancementSystem.cs(대장장이 UI, 김보민) + CoinFlipUI 완성
- 버그1(확률): WeaponInstance 가 {100,80,60,40,20} 으로 기획({90,75,45,25,10})과 다름 - 실사용 경로라 잘못된 확률 적용중
- 버그2(페널티): 4->5 실패가 0강 초기화로 되어있음 - 확정값은 '1~4강 실패 하락 없음, 4->5 실패만 1강 하락'
- 버그3(배율): WeaponInstance.FinalDamage 가 1강당 10% 선형 - 확정값은 {2,4,7,9,15}% 유지하되 인스펙터 조정 가능
- 근본 원인: 강화 로직이 WeaponData/WeaponInstance 양쪽 중복, 실사용은 WeaponInstance 인데 수치가 다 틀림

### 강화 수정 — 적용 완료 (위 2026-06-13 패치 블록 참조)
- WeaponData: enhanceSuccessRates/attackMultipliers/speedMultipliers 를 SerializeField 배열로 노출 + 외부 프로퍼티
- WeaponInstance: FinalDamage/FinalAttackSpeed/CurrentSuccessRate 를 WeaponData 배열 참조로 교체, 틀린 static 표 제거
- WeaponInstance.TryEnhance: 0~3강 실패 변동 없음(EnhanceResult.Fail 신규), 4강 실패만 1강 하락
- EnhanceResult enum 에 Fail 추가 (ResetToBase 는 미사용 죽은 코드로 잔존)
- EnhancementSystem.cs(김보민 파일): switch 에 Fail 케이스 추가 필요 - 직접 수정 전 공유 권장
- PlayerStats: 정신력 패널티 3필드 + MentalAttackMultiplier/MentalMoveSpeedMultiplier, EffectiveDefense 계수화, TakeDamage 이중적용 제거
- PlayerCombat.CalculateDamage: MentalMultiplier -> MentalAttackMultiplier
- PlayerController: CurrentSpeed/Move 에 MentalMoveSpeedMultiplier 곱

### 그 외 발견 (미해결, 충돌 — 팀 확정 필요)
- 물약 회복량 수치: 장비_기획서(체력15%/정신력10) vs 코드 기본값(체력30%/정신력25) 불일치
- 강화 초기화 단계 문서 충돌은 위 확정으로 해소(4->5 실패 1강 하락)
- 죽으면 인벤토리 전부 소실, 재판매 차감률 0.3 - 구현 여부 미확인 (PlayerDeath/상점 로직 확인 필요)

## 해야 하는 작업 (6월 18일까지)

### 1순위: 각인(세트) 효과 완성 — 코드 종료, 에디터+검증만 남음 (2026-06-13 갱신)
코드 작업은 리포에 대부분 반영 완료 확인:
- 완료: 공격력/공격속도/흡혈 연결 (ApplyAllStatBonuses 가 PlayerCombat 세터 호출, CalculateDamage/AttackInterval 반영)
- 완료: % 해석 교정 (MaxHealth = (100+장비)x(1+세트%), 방어 동일 — 기본, 장비, 세트 순서)
- 완료: 이동속도 (PlayerController.SetMoveSpeedBonus, 속도 배율 적용)
- 완료: 특수 효과 재설계 반영 (흡혈 = 공격 적중 시 데미지 비례 회복 / 보호막 = MaxHealth 비례 선흡수 + 비전투 15초 재충전, 하드코딩 100 없음)

실제 남은 작업:
1. **SetEffectData 에셋 5개 생성 + ArmorSetManager.setEffectDatabase 등록** (에디터)
   - 수치는 SetEffectData.cs 헤더의 '기획 확정 세트 효과' 표 그대로
2. **발동 검증 플레이** — 동일 원소 2개 착용 시 '[ArmorSetManager] X 세트 Tier2 활성화' 로그 확인, 어둠은 1개부터
   - 로그 미출력 시 PlayerEquipment 의 OnArmorEquipped/OnArmorUnequipped 호출 여부 추적
3. 원소 데미지: 폐기 확정 (속성별 스탯 보너스 방식). 추가 작업 없음 — 위 '기획 확정' 섹션 참조

### 2순위: 플레이 가능 범위 완성
- **Floor_2 씬** — Floor_1 복제 + DungeonCore 수치 변경 (2층 줄기는 UpOnly)
- **2층 적 프리팹**
- **세이브/로드 실플레이 검증** (복원 구현 완료 상태, 플레이 테스트 필요)

### UI 작업 (별도 담당 - 개발 외)
- 장비 장착 UI, NPC 창들, 미니맵, 타이틀, 세이브 슬롯 등

---

## 6월 18일까지 하지 않는 작업 (보류)

- **Floor_3 / Floor_4 씬** — 맵은 2층까지만 구현
- **3~4층 적 프리팹**
- **보스 (니드호그)** — 다음 학기 구현 예정
- 그래픽 후반 작업: 캐릭터 모델링(현재 Capsule 임시), 오디오 클립 연결, 툰 셰이더 검토

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
