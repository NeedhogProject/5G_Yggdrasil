# Memory.md

프로젝트 작업 메모리. 현재 시스템 상태, 확정 규칙, 미해결 이슈, 작업 기록(최신순)을 담는다.
마지막 정리: 2026-11-16 (multi 브랜치 기준. 미해결 이슈 재검토, 멀티 구현 현황과 세트 효과 개편 반영)

---

## 미해결 이슈 (2026-11-16 재검토, 해결되면 이 목록에서 삭제)

### 건희
- [높음] 멀티: 빌드에서 접속할 수단이 없다 (예정 작업의 멀티 구현 현황 참고).
- [중간] 멀티: 참가자가 세션을 나간 뒤 화면이 보이지 않는 경우가 있다. 재현 조건을 모른다 (호스트 나가기는 정상).
- [중간] 사망 디졸브 설정: Player 프리팹에 PlayerDeathDissolve 부착과 디졸브 머티리얼 연결이 필요하다. 상대 화면의 분신은 기존대로 바로 숨겨진다.
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
  - PlayerStats 의 비전투 15초 후 보호막 재충전과 PlayerCombat 의 세트 흡혈은 새 세트 에셋에 해당 수치가 없어 동작하지 않는다 (11.16).

### 김보민 (2026-10-19 수정 요청 전달)
- 모든 씬 InventorySystem 의 `droppedItemPrefab` 미연결 (DroppedItem.prefab 연결 필요)
- 상점/학자/창고와 같이 열린 상태에서 G 드롭 차단, 창고 G키와의 충돌
- 퀵슬롯
  - 초기화 주문서 등록 차단
  - 등급별 물약 구분 (사용과 수량을 ConsumableData 기준으로)
  - 씬 이동 시 등록 유지
  - Floor_3 에 퀵슬롯 없음
- Title 씬 KeySettingUI keySlots 에 삭제된 `Move` 액션 슬롯 4개 잔존
- [영혼] 스택 UI: SetEffectController 의 SoulStacks, SoulMaxStacks, OnSoulStacksChanged 를 쓴다 (11.15 전달 대상).
- 멀티 안내(관전, 층 이동, 전원 사망)의 정식 UI. 지금은 임시 OnGUI 표시다.
- 11.16 에 main 을 병합해 다른 창과 같이 열렸을 때 G 드롭 차단, 등급별 퀵슬롯 사용(UseConsumableByData)이 들어왔다. 위 항목 중 해결된 것은 김보민 확인 후 지운다.

### 씬 공통
- Floor_3 NavMesh: 11.04 에 재베이크했다(NavMesh 에셋 있음). 3층 맵 작업이 끝나면 다시 베이크한다.
- 11.16 에 main 을 multi 로 병합했다. multi 의 멀티/세트 효과/저장 작업은 아직 main 에 없다.

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
- 멀티: Photon Fusion 2.1.4 (multi 브랜치에만 설치됨)
- 브랜치: multi(건희 작업, 멀티 포함), main(팀원 작업), art(아트 전용)

## 담당
- 건희: 플레이어/전투, 인벤토리/아이템/장비, 유물, 세이브, 스테이지 전환, 적 AI, 보스(10.14부터 다시 담당), 멀티
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
  - 사망 연출은 PlayerDeathDissolve(몬스터 처치와 같은 디졸브)이고, 부활할 때 원래 머티리얼로 되돌린다.
- Animator 파라미터(코드 기준): Speed(Float), IsSprinting(Bool), Attack(Trigger)
  - 공격 모션은 Base Layer 의 Attack 스테이트다 (UpperBody 레이어와 아바타 마스크는 11.16 에 제거). Any State 에서 Attack 트리거로 들어가고, 끝나면 Idle 로, 캔슬되어 이동을 시작하면(Speed 0.1 초과) Walk 로 전환한다.
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
- 멀티
  - 공격 결정과 체력 계산은 호스트만 한다. 참가자의 머리는 공격을 스스로 시작하지 않는다.
  - 호스트가 공격(대상 위치와 무작위로 정한 위치), 체력, 사망을 NetworkPlayerAvatar 의 RPC 로 전달하고 각 PC 가 재생한다.
  - 피해는 각 PC 가 자기 플레이어에게 적용한다. 전투 단계는 각 PC 가 머리 사망 알림을 받아 따라간다.
  - 대상은 머리 구역 안의 살아 있는 플레이어다. 호스트 캐릭터는 구역 트리거로, 상대 분신은 BossPlayerZone.Contains 로 확인한다.

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
- 세트 효과 (2026-11-15 개편)
  - 누적 방식이다. N세트면 1~N단계 효과를 모두 적용한다(ArmorSetManager). SetEffectData 에셋의 Tier 값은 그 단계에서 새로 붙는 수치다.
  - 고정 수치는 SetEffectData 에셋 5개에서, 확률/지속/범위형 특수 효과는 SetEffectController(Player 에 부착) 인스펙터에서 조정한다. 효과 표는 두 스크립트 헤더가 정본이다.
  - 적 상태 이상(점화, 둔화, 기절)은 EnemyStatusEffect 가 처리한다(실행 중 자동 부착).
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
  - 나가기/종료: 타이틀로 나가기(GameManager.GoToTitle)와 게임 종료(OnApplicationQuit) 때 위치와 집 안 여부까지 저장한다. 죽은 상태에서는 저장하지 않는다.
  - SaveSystem 헤더에는 각인 결과도 적혀 있지만, 실제 호출하는 곳은 GameManager 와 BlacksmithSystem 뿐이다.
  - 새 게임은 설정창 저장 버튼(SaveGameButton)을 처음 누른 뒤부터 자동 저장한다. 그 전까지 기존 저장을 건드리지 않는다.
- 이어하기: 타이틀 버튼은 저장 파일이 있을 때만 보인다. 저장된 층으로 시작한다. 나가기/종료 저장이면 저장된 위치(2m 안의 NavMesh 로 보정)에서, 자동 저장이면 층 시작 지점에서 시작한다.
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

## 예정 작업
- 멀티: Photon Fusion 2 Host 모드 (2026-11-05 기획 확정, multi 브랜치에서 구현 중)
  - 구현 현황 (2026-11-16)
    - 완료: 플레이어 분신 동기화, 파티 층 이동, 몬스터 동기화(피격, 사망, 막타 보상), 일시정지 대응, 사망/관전/부활, 플레이어 겹침 방지, 아이템 줍기 우선, 세트 둔화/기절 호스트 전달, G키 드롭 공유(테스트 전), 나가기/종료 저장과 호스트 종료 시 참가자 저장 후 타이틀 복귀(테스트 전), 보스 동기화(테스트 전)
    - 미구현: 빌드용 접속 화면(지금은 에디터 전용 테스트 런처), 참가 조건(호스트가 마을일 때만)과 거절 안내, 참가자 집 안 시작, 열쇠 파티 공유, 달란 주머니(후순위), 인원별 몬스터 보정
    - 대기: 갑옷 색 구분(모델 머티리얼 분리 필요), 안내 문구 정식 UI(김보민)
    - 구현하지 않음: 자원 노드 동기화. 각 PC 에서 따로 동작하는 그대로 둔다 (2026-11-16 결정).
  - 기술
    - 분신 방식이다(2026-11-11 결정). 실제 캐릭터는 각자 PC 의 씬 Player 를 그대로 쓰고, 상대 화면용 분신(NetworkPlayerAvatar)만 네트워크로 생성한다. NetworkCharacterController 는 쓰지 않는다.
    - 혼자 플레이도 Fusion 을 거친다(Single 모드). 세션 없이 플레이하면 기존 싱글 동작 그대로다.
    - 몬스터는 호스트만 생성하고 움직인다. 피격은 공격한 플레이어 쪽에서 판정하고, 호스트에 피해량을 전달한다(공격력은 각자 PC 의 장비/강화/유물로 계산).
    - 씬에 배치된 Player 는 그대로 둔다.
    - 접속 방식(방 코드 생성 방식, 방을 여는 시점, 코드 표시 위치)은 미정.
  - 접속
    - 최대 2인. 방 코드 입력으로 접속한다. 호스트 혼자 시작하면 혼자 플레이다.
    - 참가는 호스트가 마을에 있고, 참가자 캐릭터가 마을에 저장된 상태일 때만 가능하다.
    - 호스트가 던전에 있으면 참가를 거절하고 "호스트가 마을에 있지 않음" 안내를 띄운다.
    - 참가자는 마을의 자기 집 안에서 시작한다.
    - 호스트가 게임을 종료하면 클라이언트도 자동 저장 후 타이틀로 돌아간다.
  - 진행
    - 층 이동은 파티 전원이 같이 한다.
    - 줄기는 누구나 상호작용할 수 있다(2026-11-05 투표 방식에서 변경). 상호작용한 사람에게 확인창(ConfirmDialogUI)을 1회 띄우고, 예를 누르면 파티 전원이 이동한다.
    - 2층 줄기는 위/아래 선택 후 확인창, 예를 누르면 이동한다. 혼자 플레이도 확인창을 띄운다.
    - 다른 플레이어에게는 "OO님이 다음 층으로 이동합니다" 안내를 띄우고 2~3초 뒤 이동한다(묻지 않음).
    - 열쇠는 파티 공유. 처치 순간 파티 열쇠 목록에 바로 추가한다(바닥 생성 없음).
    - 집과 창고는 각자 개인 데이터. NPC 는 동시 사용을 허용한다.
  - 보상과 경제
    - 몬스터 드롭(아이템, 자원 몬스터의 각인 재료)은 바닥에 생성하되 막타 친 플레이어에게만 보이고 그 플레이어만 주울 수 있다.
    - G키로 버린 아이템은 두 사람 모두 보이고 주울 수 있다(아이템 주고받기 방식).
    - 달란은 개인. 주고받을 때는 달란 주머니 아이템(금액 입력 후 생성, 사용 시 달란으로 전환)을 G키로 건넨다.
  - 전투
    - 아군 피격 없음. 몬스터는 가장 가까운 플레이어를 노린다.
    - 보스 패턴은 각 머리 구역에 있는 플레이어를 대상으로 한다.
    - 인원에 따른 몬스터 보정은 추후 결정(값만 바꿀 수 있게 두고 기본 1배).
  - 사망
    - 한 명이 죽으면 관전하다가 파티가 마을로 귀환할 때 같이 부활한다.
    - 사망 패널티는 싱글과 같다(아이템 전부 잃음). 전원 사망 시 각자 전부 잃고 마을로 귀환한다.
  - 저장
    - 각자 자기 PC 에 캐릭터를 저장한다. 다른 사람 방에서 얻은 아이템도 내 저장에 남는다.
    - 저장 시점에 게임 나가기/종료를 추가한다(마을 도착, 층 이동, 강화와 함께).
    - 나가기 저장 시 플레이어 위치와 집 안 여부도 저장하고, 이어하기는 그 위치에서 시작한다(NavMesh 위치 보정). 몬스터와 보스 상태는 저장하지 않는다.
    - 강제 종료(Alt+F4 등)로 종료 저장이 안 되면 마지막 자동 저장으로 돌아간다.
  - 표시
    - 다른 플레이어는 갑옷 머티리얼 색으로 구분한다. 소통 기능(채팅, 핑)은 제외한다.
- 몬스터 공격을 범위 감지 후 공격 모션, 그 다음 피해 순서로 바꾼다(모션과 타이밍은 팀원).
- 쉐이딩을 적용한다.
- VFX 연결: HitboxSystem.onHit 부터 시작한다(프리팹 전달 후).
- 세트 효과 이펙트 프리팹 4개(점화, 폭발, 회오리, 보호막) 연결: Player 프리팹의 SetEffectController 칸이 비어 있다(프리팹 전달 후).
- 플레이어 회피(구르기)는 구현하지 않기로 했다(10.14).

---

## 작업 기록 (최신순)

### 2026-11-16 UpperBody 레이어 제거, Attack 을 Base Layer 로 이동 (multi 브랜치)
- 상태: 컴파일 확인만 했다. 플레이 테스트 전.
- Player.controller: Attack 스테이트를 Base Layer 로 옮기고 UpperBody 레이어를 제거했다. 모션, 재생 속도(2.5), Any State 전이와 종료 전이 설정은 기존 값을 그대로 옮겼다.
- 추가한 전이: Attack 에서 Walk (Speed 0.1 초과, 전환 0.1초). 공격 캔슬 후 이동을 시작하면 공격 모션에서 빠져나온다. 캔슬 전에는 이동이 멈춰 Speed 가 0 이라 발동하지 않는다.
- PlayerCombat: 레이어 가중치 코드(UpdateUpperBodyLayer)와 인스펙터 필드 2개를 삭제했다. 공격 중 판정은 Base Layer 의 Attack 스테이트 기준(IsAttackStatePlaying)이다.
- NetworkPlayerAvatar: 같은 레이어 가중치 코드와 필드를 삭제했다. 분신은 같은 컨트롤러를 쓰므로 동작이 같다.
- 달라지는 점: 이전에는 캔슬해도 숨겨진 레이어에서 모션이 끝날 때까지 다음 공격을 막았다. 이제 캔슬하면 스테이트를 바로 벗어나므로, 다음 공격은 공격 쿨다운만 지나면 가능하다.
- PlayerUpperBody.mask 에셋은 쓰이지 않는 채로 남아 있다.

### 2026-11-16 보스 동기화, 아바타 마스크 제거 (multi 브랜치)
- 상태: 컴파일 확인만 했다. 보스전 플레이 테스트(싱글, 2인) 전.
- 보스 동기화
  - BossHead: 공격을 결정(AttackRoutine, 호스트 또는 싱글)과 재생(ExecuteAttack, 모든 PC)으로 나눴다. 대상은 FindTargetInZone 으로 고른다.
  - BossHead.TakeDamage: 참가자는 피해를 호스트로 보내고, 호스트는 계산한 체력을 참가자에게 알린다.
  - BossHead.Die: 세션 중이면 호스트가 막타 친 플레이어와 함께 모든 PC 에 알리고, 각 PC 가 ProcessDeath 로 처리한다.
  - BossNidhogg.GetHead, BossPlayerZone.Contains 추가. NetworkPlayerAvatar 에 보스용 RPC(RPC_BossDamage, RPC_BossHealth, RPC_BossDeath, RPC_BossAttack)를 추가했다.
  - 보스는 씬에 배치된 오브젝트라 NetworkObject 를 쓰지 않고 머리 역할(왼쪽, 오른쪽, 가운데)로 찾는다.
  - 세션 없이 플레이하면 기존 싱글 동작 그대로다 (IsPlayerInZone 은 FindTargetInZone 으로 대체).
- 아바타 마스크: Player.controller 의 UpperBody 레이어에서 PlayerUpperBody 마스크 연결을 해제했다. 공격 모션이 전신에 적용된다. 레이어와 PlayerCombat 의 가중치 코드는 그대로 두었고, PlayerUpperBody.mask 에셋은 쓰이지 않는 채로 남아 있다.
- 자원 노드 동기화는 구현하지 않기로 했다. 달란 주머니는 후순위로 미뤘다.

### 2026-11-16 main 병합, 사망 연출, 나가기 저장, G 드롭 공유 (multi 브랜치)
- 상태: 컴파일 확인만 했다. 플레이 테스트와 2인 테스트 전.
- main 병합: 팀원의 11.12~11.16 작업을 multi 로 가져왔다 (충돌 없음).
- 수정: 11.15 세트 효과 커밋에서 PlayerDeath.Die 의 멀티 분기(동료가 있으면 게임 오버 화면과 시간 정지 생략)가 빠져 있어 되살렸다.
- 드롭 위치: G 드롭은 플레이어가 서 있는 위치에 생성한다.
- 사망 연출: PlayerDeathDissolve 신규. PlayerDeath 가 사망 시 Play, 부활 시 Restore 를 호출한다. 사망 화면이 시간을 멈춰도 재생되도록 unscaled 시간을 쓴다. Player 프리팹에 부착하고 디졸브 머티리얼을 연결해야 동작한다.
- 저장
  - SaveSystem.SaveOnExit: 타이틀로 나가기와 게임 종료 때 저장한다. SaveData 에 hasPosition, position, inHouse 를 추가했다. 집 안 여부는 카메라 고정 상태(CameraFollow.IsFollowEnabled)로 판단한다.
  - GameManager.RestoreSavedPosition: 이어하기 때 저장된 위치를 2m 안의 NavMesh 로 보정해 이동한다. 실패하면 층 시작 지점이다.
  - 죽은 상태(멀티 관전 포함)에서는 저장하지 않는다.
  - 멀티: GoToTitle 이 세션에서 나간다(NetworkPlayerAvatar.LeaveSession). 호스트가 나가서 끊긴 참가자는 NetworkLauncher.OnShutdown 에서 저장 후 타이틀로 돌아간다.
- G 드롭 공유
  - 세션 중 G 로 버린 아이템은 NetworkPlayerAvatar.RPC_ShareDrop 으로 모든 PC 에 생성한다. 아이템 ID, 수량, 강화, 각인, 감정을 전달한다 (SaveSystem.ToSaved, FromSaved 를 public 으로 변경).
  - 줍기는 호스트가 먼저 온 요청만 승인한다 (RPC_RequestSharedPickup, RPC_SharedPickup). 주운 사람만 인벤토리에 넣고 다른 PC 에서는 바닥에서 제거한다.
  - 남은 점: 승인 후 인벤토리가 가득 차 못 넣으면 주운 사람 화면에만 남는다. 버린 뒤에 참가한 플레이어에게는 보이지 않는다.
- 달란 주머니는 미착수다 (생성 방법과 아이템 에셋 결정 필요).

### 2026-11-16 인벤토리 드롭/유물 버그 수정 (multi 브랜치)
- 상태: 컴파일 확인만 했다. 플레이 테스트 전.
- InventorySystem.UseItem: 유물 분기에도 PlayerEquipment 재확보 가드를 넣었다. 못 찾으면 유물을 빼지 않고 중단한다.
- InventorySystem.SpawnDroppedItem: 프리팹에 DroppedItem 이 없으면 생성하지 않고 false 를 반환한다 (인벤토리에서만 사라지던 문제).
- InventorySystem.DropHoveredItem: 판매창에 담긴 칸은 버리지 않는다.
- DroppedItem.TryPickup: 전용 인스턴스(무기, 방어구, 유물)는 인스턴스째 넣어 강화/각인/감정 정보를 유지한다. LootTable 드롭(기본 인스턴스)은 기존대로 데이터로 새로 만든다.
- InventorySlot.OnDisable: 인벤토리를 닫을 때 G키 드롭 대상 기록을 해제한다.
- 미해결 이슈 목록을 재검토했다. 빌드 실패(ItemDataRegistryAutoCollect)는 파일이 이미 Assets/Editor 에 있어 목록에서 뺐다.

### 2026-11-16 세트 효과 검토 수정 (multi 브랜치)
- 상태: 컴파일 확인만 했다. 2인 멀티의 둔화/기절 전달은 테스트 전.
- [영혼]: 스택 획득을 적 사망 알림 기준으로 바꿨다 (멀티는 막타 친 PC 만). 보스 적중은 공격 입력 1회당 1스택이다.
- 불 4세트 폭발: 공격 1회당 1번, 단검은 첫 타격에서만 판정한다.
- EnemyBase.IsDamageable 추가: 죽었거나 무적인 적(무적 상태의 보스 머리)은 피해와 적중 효과에서 제외한다.
- 땅 보호막 주기는 세트를 해제하거나 다시 착용해도 유지한다 (장비 교체로 재충전 방지).
- NetworkEnemy.RPC_Slow, RPC_Stun: 클라이언트가 건 둔화/기절을 호스트에서 적용한다.
- 남은 점
  - 이펙트 프리팹 4개(점화, 폭발, 회오리, 보호막)가 비어 있다. 폭발 이펙트는 재생 후 스스로 사라지는 프리팹이어야 한다.
  - [영혼] 스택 UI 는 김보민 담당이다.

### 2026-11-15 세트 효과 개편 (multi 브랜치)
- 기획 변경에 맞춰 세트 효과를 다시 만들었다. 효과 표는 SetEffectController 와 SetEffectData 헤더에 있다.
- 누적 방식: ArmorSetManager 가 1단계부터 현재 단계까지 효과를 모두 더한다. SetEffectData 에셋 5개의 Tier 값은 그 단계에서 새로 붙는 수치다.
- 신규: SetEffectController (Player 에 부착, 확률/지속/범위형 효과와 수치/쿨타임/이펙트 칸), EnemyStatusEffect (적 점화/둔화/기절, 실행 중 자동 부착)
- StatBonusType 에 DamageReduction 추가 (땅 2세트 받는 피해 감소)
- PlayerCombat: 적중/공격 알림, 조건부 공격력 반영, AttackPower 추가
- PlayerStats: 받는 피해 감소, 보호막 직접 설정, 마지막 전투 시각 조회 추가
- PlayerDeath: 사망 직전에 어둠 4세트 부활을 먼저 시도한다.
- 기획에 없어 정한 값(인스펙터 조정): 점화 쿨타임 3초, 물 3세트 회복 쿨타임 5초, 물 2세트 둔화 2초, 비전투 판정 5초
- SetEffectData 스크립트 파일명 대소문자를 고쳤다 (SeteffectData 에서 SetEffectData 로).

### 2026-11-11 아이템 줍기 우선 (multi 브랜치)
- 문제: 포털이나 줄기 근처에 떨어진 아이템 앞에서 상호작용 키를 누르면 이동 확인창이 떴다.
- DroppedItem: 플레이어가 줍기 범위 안에 있는 아이템 수를 세는 AnyInRange 를 추가했다 (주웠거나 씬이 바뀌면 해제).
- YggdrasilPortal, StemConnector, StemAscender: AnyInRange 가 true 면 상호작용하지 않는다. 아이템을 주운 뒤 다시 누르면 이동 확인창이 뜬다.
- 인벤토리가 가득 차 줍지 못해도 아이템이 범위 안에 있는 동안은 이동 확인창이 뜨지 않는다 (범위를 벗어나면 뜬다).
- 2인 테스트: 겹침 방지 통과 (막힘 확인, 층 이동 직후 튐 없음, 길막 불편 없음).
- 미해결: 참가자가 세션을 나간 뒤 화면이 보이지 않는 경우가 있다. 특정 상황에서만 발생하고 재현 조건을 모른다 (호스트 나가기는 정상).

### 2026-11-11 플레이어 겹침 방지 (multi 브랜치)
- PlayerAvatar.prefab: Player 원본의 CapsuleCollider 를 다시 살렸다 (Rigidbody 는 없음). 내 캐릭터의 Rigidbody 가 상대 분신에 막힌다.
- NetworkPlayerAvatar.SetModelVisible: 숨겨진 분신(내 분신, 죽은 플레이어, 다른 층)은 충돌체도 끈다.
- 분신은 태그가 Untagged 이고 EnemyBase/PlayerStats 가 없어 포털, 줄기, 드롭 줍기, 공격 판정에 걸리지 않는다.
- 주의: 좁은 길에서는 상대에게 길이 막힐 수 있다 (기획 선택, 2026-11-11).
- Esc 설정창(PauseMenuManager)은 Title 씬에만 있어 Town 에서 바로 플레이하면 동작하지 않는다. 빌드는 타이틀에서 시작하므로 그대로 두기로 했다.

### 2026-11-11 멀티 일시정지와 사망 처리 (multi 브랜치)
- 상태: 2인 테스트 통과 (2026-11-11, 3~4단계 포함).
- 일시정지
  - NetworkPlayerAvatar.Update: 세션에 동료가 있는 동안 Time.timeScale 이 0 이 되면 1 로 되돌린다. 인벤토리/창고/지도/설정 스크립트는 고치지 않았다. 혼자 있을 때(싱글, Fusion 혼자 세션)는 기존대로 멈춘다.
  - PlayerController: 우클릭을 누른 위치가 UI 위면 이동 목적지를 갱신하지 않는다 (시간이 멈추지 않아 인벤토리 우클릭 장착이 이동으로 이어지는 문제 방지).
- 사망
  - NetworkInputData.bDead, NetworkPlayerAvatar.IsDead 로 사망 여부를 공유한다. 죽은 플레이어의 분신은 상대 화면에서 숨긴다.
  - PlayerDeath.Die: 동료가 있으면 패널티(아이템 전부 잃음)까지만 적용하고, 게임 오버 화면과 시간 정지는 건너뛴다.
  - 관전: 살아 있는 동료가 있으면 카메라가 동료 분신을 따라간다 (CameraFollow.SetTarget 추가). 층 이동은 죽은 채로 함께 한다.
  - 부활: 파티가 마을로 이동하면 죽은 플레이어는 PlayerDeath.OnRetryClicked 로 집에서 부활한다 (회복, 기본 무기 지급).
  - 전원 사망: 3초 뒤 각자 OnRetryClicked 로 마을 집에서 부활한다. 동료가 세션을 나가 혼자 남은 경우도 같다.
  - 몬스터: FindNearestPlayer 가 죽은 플레이어를 제외한다. 살아 있는 플레이어가 없으면 EnemyAI 는 순찰로 돌아가고 EnemySpawner 는 생성하지 않는다.
- 남은 점
  - 죽은 내 캐릭터는 내 화면에서 그 자리에 서 있는 채로 남는다 (사망 모션 없음).
  - 관전/전원 사망 안내는 임시 OnGUI 표시다 (정식 UI 는 김보민 담당).

### 2026-11-11 멀티 2~4단계 (multi 브랜치)
- 방식 결정: 분신 방식. 실제 캐릭터는 각자 PC 의 씬 Player 를 그대로 쓰고, 상대 화면에 보일 분신만 네트워크로 생성한다. 기존 Player/UI 스크립트를 고치지 않기 위해 NetworkCharacterController 는 쓰지 않는다 (HUD, 인벤토리, 장비창 등 6개 스크립트가 씬 Player 를 직접 붙잡고 있음).
- 2단계 플레이어 분신
  - NetworkPlayerAvatar: 위치/회전/이동 상태/공격 횟수/층은 NetworkInputData 로, 장착 무기 ID 는 RPC 로 동기화한다. 내 분신은 숨기고, 3m 이상 이동은 순간이동으로 처리한다. 나와 다른 층의 분신은 숨긴다.
  - Prefabs/Multi/PlayerAvatar: Player 프리팹 변형 (게임플레이 컴포넌트 제거, NetworkObject/NetworkTransform 추가).
  - Editor/MultiTestLauncherMenu: Tools > Multi > 테스트 런처 자동 생성. 켜면 플레이 시 현재 씬에 NetworkLauncher 를 만든다 (씬 파일 수정 없음). Multiplayer Play Mode 로 2인 테스트.
  - 미적용: 갑옷 색 구분. 플레이어 모델이 머티리얼 1개를 공유해 갑옷만 분리할 수 없다 (모델링 쪽 머티리얼 분리 필요).
- 3단계 파티 층 이동
  - 줄기/포털 상호작용 시 확인창(ConfirmDialogUI) 1회 표시 후 이동. 혼자 플레이도 동일. 2층 줄기는 위/아래 선택 후 확인창 (StemConnector, StemAscender).
  - 세션 중에는 FloorManager, YggdrasilPortal 이 NetworkPlayerAvatar.RequestPartyMove 로 요청한다. 호스트가 먼저 온 요청만 승인(승인 후 5초 잠금)하고, 모든 PC 가 각자 GameManager.GoToFloor 로 이동한다. 요청한 사람은 바로, 다른 플레이어는 안내 후 2.5초 뒤 이동한다.
  - 안내 문구는 임시 표시(OnGUI). 정식 UI 연결 필요.
  - GameManager.MAX_FLOOR 를 public 으로 변경.
- 4단계 몬스터
  - NetworkEnemy 신규, 적 프리팹 9종에 NetworkObject/NetworkTransform/NetworkEnemy 추가.
  - 몬스터는 호스트만 생성하고 움직인다 (EnemySpawner 는 NetworkEnemy.CanSpawn 통과 시에만 생성, 생성은 NetworkEnemy.SpawnEnemyObject). 클라이언트에서는 EnemyAI 와 NavMeshAgent 를 끈다.
  - 피격: EnemyBase.TakeDamage 가 클라이언트에서는 피해량을 호스트로 보낸다. 체력은 호스트가 계산해 Health/MaxHealth 로 전달한다.
  - 대상: EnemyAI 와 EnemySpawner 가 NetworkPlayerAvatar.FindNearestPlayer 로 가장 가까운 플레이어(같은 층의 상대 분신 포함)를 고른다. 분신이 맞으면 RPC_TakeDamage 로 그 플레이어 PC 에서 피해를 적용한다.
  - 사망: EnemyBase.Die 를 Die 와 DieLocal 로 나눴다. 호스트가 막타 친 플레이어와 함께 모든 PC 에 알리고, 드롭/달란/각인 재료(ResourceDropEnemy)는 막타 친 PC 에서만 생성한다 (다른 플레이어에게 보이지 않고 주울 수 없음). 디졸브는 모든 PC 에서 재생.
  - 층 이동: 승인 시점과 GameManager.LoadScene 직전에 호스트가 몬스터를 모두 제거한다. 새 층에서는 모든 플레이어가 도착한 뒤에 생성한다.
  - EnemyWalkAnimator: 에이전트가 꺼져 있으면 실제 이동량으로 걷기 판정 (클라이언트용).
  - 세션 없이 플레이하면 기존 싱글 동작 그대로.
  - 미적용: 열쇠(인벤토리 연동 자체가 없음), 보스, 죽은 플레이어 관전/부활, G키 드롭 공유, 달란 주머니.
- 정리 (구조 점검 후)
  - 1단계 접속 확인용이던 TestNetworkPlayer(스크립트/프리팹), MultiTest 씬, NetworkLauncher 의 WASD 입력, NetworkInputData.vDirection 을 삭제했다. 접속 테스트는 Tools > Multi > 테스트 런처 자동 생성으로 한다.
  - EnemySpawner: 가장 가까운 플레이어를 매개변수로 넘기지 않고 기존 _player 를 갱신한다.
  - EnemyBase/NetworkEnemy: 사망 후 제거 분기를 NetworkEnemy.ScheduleDespawn 하나로 줄였다.
  - YggdrasilPortal: 마을 포털이 이동 요청 직후 층 번호를 미리 1로 바꾸던 호출을 제거했다 (요청이 거절되면 마을에서 층 번호만 바뀌는 문제). 층 번호는 GameManager.GoToFloor 가 이동 시점에 설정한다.
- 알려진 구조 문제 (다음 작업)
  - 인벤토리/창고/지도/일시정지/사망이 Time.timeScale = 0 을 쓴다. 호스트가 멈추면 몬스터가 모두 멈추고, 참가자가 멈추면 자기 캐릭터만 멈춘 채 맞는다. (11-11 일시정지와 사망 처리에서 대응)
  - 사망 시 혼자 마을로 이동한다 (관전/부활 미구현). 호스트가 죽으면 몬스터가 사라지고, 참가자가 죽으면 새 몬스터가 생성되지 않는다. (11-11 일시정지와 사망 처리에서 대응)
  - 접속 수단이 에디터 전용 테스트 런처뿐이라 빌드에서는 멀티에 접속할 수 없다. 참가 조건(호스트가 마을일 때만)도 없다.
  - 보스와 자원 노드는 동기화되지 않는다 (각 PC 에서 따로 동작).

### 2026-11-05 플레이어 신규 모델 애니메이션 연결
- MainCharacter rigging.fbx 를 Humanoid 로 설정(뼈대에 Hips 가 없어 Hips 는 mixamorig:Spine, Spine 은 Spine1, Chest 는 Spine2 로 매핑)하고 Animator Avatar 를 새 모델 Avatar 로 교체했다. 레거시 Animation 컴포넌트는 제거했다.
- WeaponHolder 를 mixamorig:RightHand 아래에 다시 만들고 PlayerEquipment.weaponHolder 에 연결했다(이전 버전은 손뼈가 아닌 모델 루트 아래에 있었음).
- 빌드 목록에서 Floor_.unity 를 비활성화했다.
- 멀티 기획을 확정했다(예정 작업 참고).

### 2026-11-04 몬스터 처치 디졸브 연출
- 신규: EnemyDeathDissolve (적 프리팹 루트에 부착, EnemyBase.OnDied 구독)
  - 사망 시 모든 메쉬 렌더러 머티리얼을 디졸브 인스턴스로 교체한다. 원본 _BaseMap, _BaseColor 를 _Base_Texture, _Albedo 로 복사하고, 텍스처가 없으면 흰색을 쓴다.
  - Cutoff Height 를 렌더러 월드 bounds 위에서 아래로 duration(1.5초) 동안 내린다. 여유값은 Noise Strength + Edge Width.
- Dissolve.shadergraph: Position 노드를 Object 에서 World 로 변경 (몬스터마다 메쉬 스케일이 달라 Object 기준 고정값이 맞지 않음)
- EnemyBase: destroyDelay 필드 추가 (기존 0.5초 고정). 적 프리팹 9종은 1.6초로 설정
- 적 프리팹 9종(Floor_1, Floor_2, Resource)에 EnemyDeathDissolve 추가, Shader Graphs_Dissolve 머티리얼 연결
- 보스 머리(BossHead)는 미적용 (사망 연출이 따로 있음)
- DissolvePreview.cs 는 이펙트 미리보기용이라 게임에서 쓰지 않는다.
- 3층 계단(stone5_s3_): 투명 경사로 콜라이더(Ground) 배치, Floor_3 NavMeshSurface 를 Physics Colliders 로 재베이크

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
