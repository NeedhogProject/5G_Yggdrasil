# Memory.md

프로젝트 작업 메모리. 현재 시스템 상태, 확정 규칙, 미해결 이슈, 작업 기록(최신순)을 담는다.
마지막 정리: 2026-10-19 (10.18 커밋 e86b540 기준. 대체된 항목과 낡은 요약 삭제, 7~10월 작업 반영)

---

## 미해결 이슈 (2026-10-18 검토, 해결되면 이 목록에서 삭제)

### 건희
- [높음] 빌드 실패
  - ItemDataRegistryAutoCollect.cs 는 Editor 폴더가 아닌 Scripts/Items 에 있고, `using UnityEditor` 를 가드 없이 사용한다.
  - 에디터에서는 문제없지만 플레이어 빌드에서 컴파일 에러가 난다.
  - 조치: Assets/Editor 폴더로 옮기거나 `#if UNITY_EDITOR` 로 감싼다.
- [높음] 유물 우클릭 장착 시 유물 증발
  - InventorySystem.UseItem 의 유물 분기에는 PlayerEquipment 재확보 가드가 없다(장비 분기에만 있음).
  - PlayerEquipment 가 null 이면 RemoveItem 이 먼저 실행된 뒤 장착이 생략되어 유물이 사라진다.
- [높음] G 드롭 시 아이템 삭제
  - SpawnDroppedItem 은 생성한 프리팹에 DroppedItem 이 없어도 true 를 반환한다.
  - 그래서 프리팹이 없으면 모델 프리팹만 생성되고 인벤토리에서는 삭제된다.
  - 조치: DroppedItem 컴포넌트가 없으면 Destroy 하고 false 를 반환한다.
- [높음] 판매 마킹 칸 드롭
  - DropHoveredItem 에 IsSaleMarked 검사가 없다.
  - 김보민 쪽 InventoryUI 차단과 별개로 방어 코드를 둔다.
- [중간] 버렸다 주운 아이템의 정보 초기화
  - DroppedItem.TryPickup 의 비중첩 경로가 `AddItem(Data)` 라서, 버렸다 주우면 강화/각인/감정 정보가 초기화된다.
  - LootTable 은 기본 ItemInstance 를 넘기므로, 전용 인스턴스일 때만 `AddItem(instance)` 를 쓴다.
- [중간] 닫힌 인벤토리의 hover 기록 잔존
  - 인벤토리를 닫을 때 `_hoveredSlot` 이 남는다. 비활성 오브젝트는 OnPointerExit 를 받지 않기 때문이다.
  - 조치: InventorySlot.OnDisable 에서 기록을 해제한다.
- [중간] 드롭 위치
  - 플레이어 앞 2m 에 생성되므로 벽 안이나 허공 위에 생길 수 있다(DroppedItem 은 중력 없음).
  - 발밑 생성이나 NavMesh.SamplePosition 보정을 검토한다.
- [낮음] G 드롭 제외 대상
  - Prefab3D 가 없는 아이템이 13종 있다: 열쇠 8종, 초기화 주문서, 기본 단검 W-001-1, 기본 창 W-003-1 등.
  - 이 아이템들과 퀘스트 아이템(열쇠)을 G 드롭에서 어떻게 처리할지 정한다.
- [낮음] 각인 결과 자동 저장 누락
  - SaveSystem 헤더에는 "강화/각인 결과" 라고 적혀 있지만, 각인 쪽(InscriptionMasterSystem, RuneInscriptionSystem)에서는 Save 를 호출하지 않는다.
  - InscriptionMasterSystem 은 김보민 파일이므로 전달이 필요하다.
- [낮음] 열쇠 인벤토리 연동 미구현
  - EnemyBase.DropKey 는 로그만 찍는다.
  - StemManager.TryInsertKey 는 열쇠 확인 없이 성공한다.
- [정리] 불필요한 코드
  - InventorySystem.DropItem(ItemData) 는 호출하는 곳이 없다.
  - UseConsumableByType / GetConsumableCount 는 퀵슬롯 수정 후 쓰이지 않는다.
  - DropHoveredItem 의 제거 분기는 `RemoveItemAtSlot(owner)` 한 줄로 충분하다.
  - UseItem 의 `playerEquipment?.` 는 가드 뒤라 `?.` 가 필요 없다.

### 김보민 (2026-10-19 수정 요청 전달)
- 모든 씬 InventorySystem 의 `droppedItemPrefab` 미연결 (DroppedItem.prefab 연결 필요)
- 상점/학자/창고와 같이 열린 상태에서 G 드롭 차단, 창고 G키와의 충돌
- 퀵슬롯
  - 초기화 주문서 등록 차단
  - 등급별 물약 구분 (사용과 수량을 ConsumableData 기준으로)
  - 씬 이동 시 등록 유지
  - Floor_3 에 퀵슬롯 없음
- Title 씬 KeySettingUI keySlots 에 삭제된 `Move` 액션 슬롯 4개 잔존

### 씬 공통
- Floor_3 NavMesh 미베이크: 플레이어 우클릭 이동과 적 AI 가 동작하지 않는다.

---

## 프로젝트 환경
- 엔진: Unity 6000.3.10f1
- 렌더 파이프라인: URP 17.3 (GraphicsSettings 에 URP 에셋 지정)
  - 보스 연출 코드도 `_BaseColor` (URP Lit) 기준이다.
- 입력: New Input System (PlayerInputActions.inputactions, Assets/_Source/Town)
- 길찾기: AI Navigation (NavMeshSurface). 베이크된 씬: Town, Floor_1, Floor_2, BossTest
- 빌드 씬 순서: Title, Town, Floor_1, Floor_2, Floor_3
  - BossTest 는 보스 단독 테스트 씬 (Floor_4_Boss 에서 이름 변경)
- 레포: github.com/NeedhogProject/5G_Yggdrasil (옛 이름 Programming)
- 멀티(Photon Fusion 2) 패키지는 아직 미설치

## 담당
- 건희: 플레이어/전투, 인벤토리/아이템/장비, 유물, 세이브, 스테이지 전환, 적 AI, 보스(10.14부터 다시 담당), 멀티(예정)
- 김보민: UI/Canvas, 상점, 집/창고, 3스테이지 씬(임포트, 콜라이더), 퀵슬롯, 대장장이/강화 UI, 툴팁, 키 설정 UI
- 김보민 파일 수정은 직접 고치지 않고 전달한다. 대상: BlacksmithSystem, EnhancementSystem, UIItemTooltip, KeySettingUI 씬 오브젝트
- 사운드는 별도 팀원 담당
- Memory.md 는 건희만 갱신한다.

---

## 현재 시스템 요약 (2026-10-18 코드 기준)

### 1. 핵심 인프라와 영속 단위
- DontDestroyOnLoad 대상
  - 상시 유지: GameManager(GameCore), AudioManager, InputReader, KeyBindingManager, PauseMenuManager, PersistentCanvas, ItemDataRegistry
  - 플레이어 단위: PlayerController(Player), InventorySystem(Inventory_Canvas), ResourceInventory
- HUD_Canvas(HUDManager, QuickSlotSystem)는 씬마다 따로 있다. HUDManager 는 PlayerStats 가 바뀌면 자동으로 다시 연결한다.
- 플레이어 단위 오브젝트는 씬마다 배치되어 있다.
  - 첫 씬의 것이 영속되고, 이후 씬의 것은 싱글턴 가드가 스스로 파괴한다.
  - 부모가 있으면 Awake 에서 루트로 분리한다(DDOL 은 루트에만 동작).
- GameManager.DestroyPersistentPlayerObjects (새 게임, 이어하기, 타이틀 복귀)는 반드시 DestroyImmediate 를 쓴다.
- GameManager 씬 로드 규칙
  - 로드 중에는 중복 로드 요청을 차단한다(마을 씬 중복 로드 시 NPC 싱글턴 자기 파괴 방지).
  - 빌드에 없는 씬은 먼저 확인한다.
  - EventSystem 이 없는 씬에는 자동으로 생성한다.
- 게임 흐름: 타이틀, 마을, 던전 1~3층(3층은 최하층 + 보스 구역), 엔딩. 4층은 제거됐다.
- 스폰/카메라 지점 오브젝트 이름: Spawn_TownCenter, Spawn_DungeonStart, Spawn_House, HouseCameraPoint
  - 영속 플레이어는 이전 씬 위치를 들고 오므로 씬마다 스폰 지점이 필요하다.

### 2. 플레이어
- 이동: 우클릭 탭이면 클릭 지점, 홀드면 커서 지점을 계속 갱신하며 NavMesh 경로로 이동한다.
  - NavMeshAgent 대신 NavMesh.CalculatePath 로 경로만 계산하고, 기존 Rigidbody 속도로 코너를 따라간다.
  - 텔레포트 감지: 한 프레임에 3m 이상 이동하면 경로를 폐기한다.
  - 막힘 감지: 거의 못 움직이면 경로를 폐기하고 같은 목적지를 재요청하지 않는다(제자리 걷기 방지).
  - 커서 근처에 NavMesh 가 없으면 커서 방향으로 갈 수 있는 끝 지점을 목적지로 쓴다.
- 회전: 이동 중에는 진행 방향, 공격 시작 순간에만 커서 방향, 공격 중에는 고정한다.
- 달리기: Shift 홀드, 정신력 소모
- 공격
  - 시작하면 이동을 정지한다.
  - 경직(WeaponData.AttackLockTime)이 끝난 뒤 이동 입력이 들어오면 공격 모션을 캔슬한다.
  - 단검은 다단히트가 끝나기 전에 캔슬되지 않게 최소 경직을 보장한다.
- 판정(HitboxSystem, Player/HitboxChild 자식 오브젝트)
  - 리치/너비는 장착 무기 WeaponData(Reach, AttackWidth) 값을 쓴다.
  - 장검은 Reach 를 부채꼴 반경으로 쓰고, 각도는 인스펙터 swordAngle 값이다.
  - 판정 대상은 적과 자원 노드만이다.
  - 높이 판정은 콜라이더에서 플레이어 높이에 가장 가까운 점을 기준으로 한다(큰 적 누락 방지).
- 공격속도: WeaponInstance.FinalAttackSpeed (무기 에셋 값에 강화 배율 반영)
- 외부 배율: PlayerController/PlayerCombat 의 외부 이동속도/공격속도 배율 (보스 장판에서 설정)
- 낭떠러지 차단과 낙하 복귀
  - 진행 방향 앞에 지면이 없으면 축별로 걸러 가장자리를 따라 미끄러진다.
  - Y < fallYThreshold 이면 마지막 안전 위치로 복귀한다.
  - groundLayer 를 Ground 로 좁혀야 한다. 기본값 Everything 이면 허공도 지면으로 잡힌다.
- 정신력 패널티
  - 공격력, 방어력, 이동속도만 감소한다(받는 피해 직접 증가는 없음).
  - 강도는 PlayerStats 인스펙터 3필드로 조정한다.
- 사망
  - 장착 장비, 인벤토리, 각인 재료를 전부 잃는다.
  - 다시하기를 누르면 집(Spawn_House)에서 부활하고, 기본 무기를 재지급하며, 카메라는 바로 추적한다.
- Animator 파라미터(코드 기준): Speed(Float), IsSprinting(Bool), Attack(Trigger)
  - 상체 레이어 보간(upperBodyLayerIndex) 코드가 PlayerCombat 에 남아 있다.
- UI 가 열린 동안 이동 잠금: PlayerInteractionLock(김보민)이 인스펙터 패널 목록을 보고 PlayerController.enabled 를 끈다.

### 3. 적과 던전
- EnemyAI
  - NavMeshAgent 로 이동한다. Rigidbody 는 실행 시 Kinematic 으로 바꿔 플레이어를 밀지 않는다.
  - 공격 범위 안쪽에서 정지하고, 공격 상태에 들어가면 velocity 를 0 으로 만든다.
- 적 공격: 공격 범위 안에서 쿨다운마다 즉시 피해를 준다. 공격 모션과 타이밍은 팀원 담당으로 미완이다.
- 방어 공식(플레이어와 적 동일): 방어력 1당 피해 0.05% 감소. 100 이면 5%, 상한 95%. 기본 방어력 100.
- 달란: 처치 즉시 지급(EnemyBase.GrantGold, 프리팹별 확률/범위)에 유물 달란 비율을 반영한다. 상한 999999.
- 스폰
  - SpawnPoint.spawnCount 로 포인트당 여러 마리를 스폰한다.
  - 층 필터(IsAvailableOnFloor)를 적용한다.
  - GameManager.HandleSceneLoaded 첫 줄에서 SyncFloor 를 호출한다.
- 열쇠 배정
  - 층에 들어오면 열쇠를 섞어 대기열에 넣는다.
  - 열쇠 스폰 포인트에서 처음 나온 1마리에게 AssignKey 로 배정한다.
  - 한 번 배정한 열쇠는 재배정하지 않는다.
- 줄기
  - 1층은 열쇠를 삽입해 2층으로 하강만 한다.
  - 2층은 E키로 위/아래 선택창을 열고, 아래를 선택할 때만 열쇠를 확인한다.
  - 3층은 UpOnly 이며 열쇠 없이 2층으로 올라간다.
- DungeonDifficultyScaler: 1~3층 일반 몬스터 표와 3층 보스 배율(bossHealthMult, bossAttackMult)을 따로 둔다.
- BossZoneBGM: 3층 보스 구역 트리거에 들어가면 보스 BGM, 나오면 층 BGM 으로 바꾼다.

### 4. 보스 (3층 보스 구역, 머리 3개)
- BossNidhogg 는 전투 단계와 모든 보스전 수치를 관리한다.
  - 단계: 전투 시작, 한쪽 머리 파괴, 그로기, 중앙 최종전, 사망, 엔딩
- BossHead 는 머리마다 BossPlayerZone(트리거)에 플레이어가 있을 때 공격한다.
  - 왼쪽: 투사체(BossProjectile)를 시전 순간 위치와 주변 랜덤 위치로 발사하고, 착탄 자리에 위험 지대를 남긴다.
  - 오른쪽: 이동속도 감소 장판과 공격속도 감소 장판(BossGroundField)을 설치한다. 종류별 최대 개수를 넘으면 오래된 것부터 지운다.
  - 가운데: 예고 존(노란색에서 빨간색) 후 근접 공격을 한다. 좌우 머리가 모두 죽기 전까지 무적이다.
- 좌우 머리가 죽으면 회색으로 남고, 가운데 머리는 Die 애니메이션으로 사라진다.
- 보스가 죽으면 모든 공격과 장판을 즉시 제거한다.

### 5. 아이템과 인벤토리
- 그리드: 10x4 멀티셀(디아블로식). 씬 인스펙터 값 기준이며, 코드 기본값은 10x8 이다.
  - 보조 칸은 ownerSlot 으로 주인 칸을 가리킨다.
  - 드래그, 우클릭, 툴팁은 모두 주인 칸 기준으로 처리한다.
- 아이콘 오버레이(iconOverlay)
  - 아이콘을 오버레이 레이어로 옮겨 격자선 위에 그린다.
  - 인벤토리를 열 때마다 RefreshIconPositions 로 위치를 보정한다.
- 중첩
  - 관련 함수: AddItemStack, MergeIntoStacks, GetItemCount(ItemData), RemoveItemCount
  - 판매 마킹 칸은 집계에서 제외한다. 0개가 된 묶음은 칸까지 비운다.
- 제거와 이동은 아이템 종류가 아닌 칸의 인스턴스 기준이다(같은 아이템이 여러 칸일 수 있음).
- 우클릭 동작
  - 장비 장착, 물약 사용(초기화 주문서는 차단), 유물 장착
  - 상점 판매 모드면 판매창에 담기, 학자 감정창이 열려 있으면 감정할 유물 선택
- 창고와 인벤토리 사이 드래그: 빈자리면 배치, 아이템 1개면 교환, 둘 다 아니면 빈 공간, 그것도 없으면 변경 없음. 창고 총 용량을 먼저 확인한다.
- 창고 데이터는 HouseSystem 의 static 목록이다(마을 씬이 파괴돼도 유지).
- CreateInstanceFromData 는 데이터 타입별 전용 인스턴스를 만든다(Weapon/Armor/Consumable/Resource/Relic).
- 바닥 아이템
  - LootTable 의 드롭 횟수는 최소~최대 랜덤이고, 중첩 아이템만 수량을 적용한다.
  - DroppedItem 은 E키로 줍고, 중첩 아이템은 수량째로 획득한다.
- 10.18 (김보민): Ctrl+좌클릭으로 소모품을 퀵슬롯(1, 2키)에 등록, G키로 가리키는 칸의 아이템 드롭
- 가격: ItemData._price 단일 필드. 판매가 = 가격 x 0.3
- 물약
  - 체력: 최대 체력 대비 % 회복 (일반 15, 고급 30, 영웅 55)
  - 정신력: 절대값 회복 (일반 10, 희귀 20, 영웅 30)

### 6. 장비, 강화, 각인, 유물
- 무기 강화(WeaponData 표 단일 출처): 성공 확률, 공격 배율, 속도 배율 배열을 인스펙터에서 조정한다.
  - 0~3강 실패는 변동이 없고, 4강에서 5강 실패 시에만 1강으로 하락한다.
- 강화 결과가 나오면 자동 저장한다(BlacksmithSystem). 각인 결과 자동 저장은 호출이 없다(미해결 이슈 참고).
- 방어구 강화는 없다. 강화는 무기 전용, 방어구는 각인 전용이다(각인 1슬롯).
- 세트 효과는 SetEffectData.cs 헤더의 확정표가 정본이다(속성별 스탯 보너스, 원소 데미지 폐기).
  - 세트_효과.docx 와 게임_구조.txt 는 옛 서술이다.
- 장비 교체 시 반환되는 기존 장비는 `AddItem(replaced)` 로 인스턴스째 반환한다(강화/각인 보존).
- 최대 체력 = (100 + 장비 + 유물 HP) x (1 + 세트%). 유물 HP 는 장비 최대 체력에 합산된다.
  - 마을에서 착용하면 회복되고, 던전에서는 만피일 때만 만피를 유지한다.
- 유물
  - RelicData 옵션: MaxHealth, MaxMental, Attack(고정값), DropGold(비율). None 이면 판매용이다.
  - RelicInstance 가 감정 여부를 유물마다 따로 보관한다.
  - 미감정 유물은 장착과 판매가 안 되고, 툴팁에서 효과와 희귀도가 숨겨진다.
  - 학자(ScholarSystem)에서 달란(기본 50)을 내고 감정한다. 감정창과 인벤토리를 같이 열고 유물을 우클릭으로 선택한다.
  - 유물 칸은 2개다(PlayerEquipment). 빈 칸에 먼저 넣고, 둘 다 차 있으면 0번 칸을 교체한다. 같은 유물 2개는 효과가 중첩된다.
- 대장장이 무기 선택: 대장간 목록 카드(BlacksmithItemUI)에서 고른다.

### 7. 저장
- 슬롯 없이 파일 1개: persistentDataPath/save.json
- 저장 시점
  - 자동: 마을 도착, 층 이동, 강화 결과
  - SaveSystem 헤더에는 각인 결과도 적혀 있지만, 실제 호출하는 곳은 GameManager 와 BlacksmithSystem 뿐이다.
  - 새 게임은 설정창 저장 버튼(SaveGameButton)을 처음 누른 뒤부터 자동 저장한다. 그 전까지 기존 저장을 건드리지 않는다.
- 이어하기: 타이틀 버튼은 저장 파일이 있을 때만 보인다. 저장된 층으로 시작한다.
- 저장 항목: 인벤토리(칸 위치 포함), 장착 장비, 유물 칸, 창고, 달란, 체력/정신력, 각인 재료, 현재 층
  - 퀵슬롯 등록과 바닥 아이템은 저장하지 않는다.
- 아이템은 에셋 GUID 로 저장하고 ItemDataRegistry(GameCore 프리팹)에서 찾는다.
  - ItemData 에셋을 추가/삭제/이동하면 ItemDataRegistryAutoCollect 가 목록을 자동 갱신한다.
- 복원 순서
  - 창고는 씬 로드 전에 복원한다.
  - 플레이어는 씬 로드 후 인벤토리 칸이 생성될 때까지(최대 10프레임) 기다렸다가 복원한다. 장비를 먼저 복원해야 최대치가 맞는다.
- 사망 화면(체력 0)이나 레지스트리가 없을 때는 저장하지 않는다(기존 저장 덮어쓰기 방지).

### 8. 카메라와 연출
- CameraFollow
  - 씬 전환 시 target 을 재탐색한다.
  - SnapToTarget 은 텔레포트 직후 즉시 정착시킨다.
  - MoveToFixedPoint 와 SetFollowEnabled 로 고정/추적을 전환한다.
- Main Camera 는 GameCore 자식으로 영속된다.
  - 던전 씬에는 카메라를 두지 않는다.
  - 단독 테스트용 DevTestCamera 는 GameCore 카메라가 들어오면 자동으로 제거된다.
- 새 게임은 집 안에서 시작하고, 한 프레임 뒤 HouseCameraPoint 로 카메라를 고정한다.
- 집 출입: HouseDoorInteractable(bEntersHouse)이 ScreenFader 페이드를 거쳐 같은 씬 안에서 텔레포트한다. 들어가면 HouseCameraPoint 로 고정, 나오면 추적을 재개한다.

### 9. UI/NPC 연동 지점 (김보민 담당, 코드 연동만 기록)
- NPCInteractable 은 상인, 대장장이, 각인술사, 학자를 타입별로 라우팅한다. E 프롬프트는 InteractionHint 공용이다.
- InteractableHighlight: 상호작용 물체에 붙이면 범위 안에서 E 프롬프트와 외곽선을 표시한다(기존 트리거 공유).
- ConfirmDialogUI: 예/아니오 공용 팝업. 던전 입장 확인에 쓴다.
- 상점, 학자, 창고는 인벤토리를 옆에 같이 연다(InventoryUI._openedBeside).
- PlayerStatsPanelUI: 인벤토리 우측 상단 스탯 표시
- NPC 대사는 WaitForSecondsRealtime 기반 타이핑이다.

---

## 확정 규칙과 주의사항
- 플레이어에 NavMeshAgent 를 쓰지 않는다.
  - GameManager 스폰, 집 문 텔레포트, 낙하 복귀가 transform.position 을 직접 대입하기 때문이다.
- `.inputactions` 는 기존 파일에 덮어써서 GUID 를 유지한다. 새 파일을 만들면 PlayerInput 참조가 끊긴다.
- 프로젝트를 OneDrive 동기화 폴더 밖에 둔다(Burst 경로 잠금으로 빌드 실패).
- 받은 씬을 쓸 때는 중복 Audio Listener 와 Camera 를 제거한다.
- UI 설정
  - 툴팁(TooltipPanel)의 Image 와 자식은 Raycast Target 을 해제한다.
  - OutsideCloseButton 은 Hierarchy 에서 InventoryPanel 보다 위에 둔다.
  - 인벤토리 슬롯 배경 Image 는 Raycast Target 이 필요하다(아이콘이 입력을 받지 않음).
  - IconOverlay 는 ItemInventoryPanel 의 자식, SlotContainer 다음 형제로 두고 레이아웃 컴포넌트를 붙이지 않는다.
- 새 ItemData 에셋은 ItemDataRegistry 목록에 들어가야 불러오기가 된다(자동 갱신, 수동은 컴포넌트 우클릭).
- 유물과 장비는 인벤토리에서 빼기 전에 장착 가능 여부를 먼저 확인한다(빼고 실패하면 아이템이 사라짐).
- 기획 엑셀은 A열이 빈칸이고 데이터는 B열부터다.

## 예정 작업 (기획 확정, 코드 미착수)
- 멀티: Photon Fusion 2 Host 모드
  - NetworkCharacterController 를 사용하고, NavMesh 경로는 클라이언트에서 계산해 이동 방향만 입력으로 보낸다.
  - 골드와 드롭은 막타를 친 플레이어에게 준다.
- 몬스터 공격을 범위 감지 후 공격 모션, 그 다음 피해 순서로 바꾼다(모션과 타이밍은 팀원).
- 플레이어 아바타 마스크를 제거한다.
- 쉐이딩을 적용한다.
- VFX 연결: HitboxSystem.onHit 부터 시작한다(프리팹 전달 후).
- 플레이어 회피(구르기)는 구현하지 않기로 했다(10.14).

---

## 작업 기록 (최신순)

### 2026-10-18 (김보민) 인벤토리 G 드롭과 퀵슬롯 등록
- 퀵슬롯: 인벤토리 소모품을 Ctrl+좌클릭으로 빈 칸에 선착순 등록하고 1, 2키로 사용한다.
- G키: 인벤토리를 연 상태에서 가리키는 칸의 아이템을 DroppedItem 으로 바닥에 생성한다.
- 인벤토리를 열 때 멀티셀 아이콘 위치를 보정한다. 빈 칸은 아이콘이 꺼진 채로 시작한다.
- 우클릭 장착 직전에 PlayerEquipment 를 다시 찾는다(장비 분기만 적용).
- 남은 작업(김보민): 슬롯 수량 텍스트 프리팹 추가, 학자 감정창 패널, 보스 체력바
- 검토 결과는 위 미해결 이슈를 참고한다.

### 2026-10-14 보스 패턴 구현과 히트박스 높이 판정
- 신규: BossGroundField, BossPlayerZone, BossProjectile
- BossHead 와 BossNidhogg 를 재작성했다(위 시스템 요약 4 참고).
- PlayerController 와 PlayerCombat 에 외부 이동속도/공격속도 배율을 추가했다(장판 감속).
- HitboxSystem: 높이 판정을 콜라이더 최근접점 기준으로 바꿨다.
- Floor_4_Boss 씬을 BossTest 로 이름 변경하고 테스트 프리팹을 추가했다.

### 2026-10-13 세이브 재작성, 유물 시스템, 인벤토리 중첩
- SaveSystem 재작성(파일 1개, 자동 저장, GUID), SaveGameButton, ItemDataRegistry 와 AutoCollect, 타이틀 이어하기
- 유물: RelicOptionType, RelicData 옵션, RelicInstance 감정, ScholarSystem 감정, PlayerEquipment 유물 효과, PlayerStats 유물 보너스, 툴팁 미감정 처리, 미감정 판매 차단
- 인벤토리: 중첩 추가/병합/수량 제거, 인스턴스 기준 제거, 창고와 인벤토리 교환 배치
- LootTable: 드롭 횟수 최소~최대, 중첩 아이템 수량. DroppedItem: 중첩 수량째 획득
- 이동: 막힘 감지와 경로 폐기, NavMesh 밖 커서 처리
- (김보민) 퀵슬롯 1차(인스펙터 지정 방식), PlayerStatsPanelUI, 각인 방어구 목록 정렬(10.12)

### 2026-10-12 (김보민) 유물 칸과 상호작용 표시
- PlayerEquipment 에 유물 칸 2개를 추가하고, 우클릭으로 장착한다.
- InteractableHighlight, InteractionHint(E 프롬프트와 외곽선)를 추가했다.

### 2026-10-09 4층 제거 반영, 열쇠 배정 변경, 적 밀림 수정
- 4층을 제거하고 3층을 최하층으로 반영했다. 대상: GameManager, AudioManager, DungeonDifficultyScaler(보스 배율), StemConnector/StemManager, EnemySpawner. BossZoneBGM 을 추가했다.
- 열쇠: 층 입장 시 1회 셔플, 스폰 시 AssignKey, 재배정 없음. 2층은 아래로 갈 때만 열쇠, 3층은 열쇠 불필요.
- EnemyAI: Rigidbody Kinematic, 공격 범위 안쪽 정지, 공격 진입 시 velocity 0 (플레이어 밀림 해결)
- HitboxSystem: 적과 자원 노드만 판정한다.
- (김보민) 대장장이 X/ESC 시 NPC 메뉴로 복귀, 상점 개선, 부활 시 카메라 즉시 추적

### 2026-10-08 공격 경직 후 이동 캔슬
- WeaponData.attackLockTime 을 추가했다. 경직 이후 이동 입력으로 공격 모션을 캔슬한다(상체 레이어도 끔).
- 공격 중 회전을 고정한다(시작 순간에만 커서 방향).

### 2026-10-07 무기 공격범위/공격속도 적용, 씬 로드 안정화
- 공격범위 미적용 수정: HitboxSystem 이 무기 에셋 리치/너비를 쓴다.
- 공격속도도 무기 에셋 값에 강화 배율을 반영한다(PlayerCombat 하드코딩 표 삭제).
- GameManager: 씬 로드 중복 차단, EventSystem 자동 생성
- 부활 시 기본 무기 재지급(StartingEquipment.GrantOnRespawn)
- (김보민) 보스 초안, HUD 재연결, NPC 대사 타이핑

### 2026-09-03 (김보민) 던전 입장 확인창
- ConfirmDialogUI 를 추가했다. YggdrasilPortal 은 E키를 누르면 확인 후 입장한다.
- 상점 대사에 타이핑 효과를 넣었다.

### 2026-08-08 (김보민) UI 열림 시 이동 잠금
- PlayerInteractionLock: PlayerController 를 수정하지 않고 외부에서 enabled 와 애니메이터를 제어한다.
- 디버그용 스크립트 3개를 삭제했다.

### 2026-07-11 (김보민) 기존 슬롯 세이브 제거
- SaveSystem(3슬롯), SaveSlotPanelUI, SaveSlotCardUI 를 삭제했다.
- 10.13 에 파일 1개 방식으로 재작성했다.

### 2026-07-07 이동 방식 B 확정 (우클릭 NavMesh 경로 이동)
- WASD 를 제거하고 MoveHold(Button, 우클릭)를 추가했다. InputReader.MoveHeld 를 쓴다.
- Rigidbody 와 CalculatePath 방식을 선택한 이유는 확정 규칙 첫 항목에 있다.
- 공격 시작 순간 커서 방향 스냅(SnapRotationToCursor)
  - 장검/창 판정은 PerformAttack 호출 프레임의 transform.forward 기준이다.
  - 스냅이 없으면 이동 중 옆의 적을 클릭했을 때 빗나간다.

### 2026-06 이전 (요약)
- 플레이어/인벤토리/자원 영속 단위와 DestroyImmediate 규칙
- 멀티셀 인벤토리와 아이콘 오버레이
- 방어 공식 통일, 달란 처치 지급
- 강화 표를 WeaponData 단일 출처로 정리
- 정신력 패널티, 사망 패널티
- 가격 단일화, 물약 % 회복
- Floor_2 섬 지형의 낭떠러지 차단과 낙하 복귀
- 집 출입 페이드, 새 게임 집 시작
- 자원 몬스터 층 필터
- 세부 내용은 위 시스템 요약으로 통합했다.
