# Gemini CLI History

## 2025년 11월 16일 일요일

- `TriggerInteractor`를 마우스와 Raycast를 사용하는 방식으로 변경
- `TerminalManager`에서 `OnTriggerStay`를 사용하는 기존 상호작용 방식 삭제
- `PlayerInputs.cs`의 `TryInteract` 메서드에서 Raycast가 `interactableLayer`만 감지하도록 수정
- `TerminalManager` 리팩토링
- `NetworkPlayer` 스크립트에 `IsMine` 속성이 없는 문제 수정
- 상호작용 가능한 오브젝트를 바라볼 때 UI 텍스트 표시 기능 추가
- `SpawnManager.cs`의 `PrewarmPools` 메서드에서 발생하는 `ArgumentException` 버그 수정
- `InGameUIManager`의 싱글톤 패턴을 이벤트 기반 아키텍처로 리팩토링
- 클라이언트 디싱크 문제를 해결하기 위해 네트워크 아키텍처 리팩토링
- `NetworkManager.cs`의 컴파일 오류 수정
- `SpawnManager.cs`의 `PrewarmPools`에서 발생하는 NavMesh 경고 수정
- 멀티플레이 재접속 및 연결 종료 관련 버그 수정
- **네트워크 코드 전체 검토 및 리팩토링**:
    - **`NetworkManager.cs`**: `FixedUpdate` 로직을 작은 메소드들로 분리하고, 코드 전반에 설명 주석을 추가하여 가독성 및 유지보수성 향상.
    - **`NetworkPlayerManager.cs`**: 더 이상 사용되지 않는 `SpawnMonster` 메소드를 제거하고, 전반적인 구조를 개선하고 설명 주석을 추가.
    - **`ServerRoomManager.cs`**: `Awake`에서 메시지 핸들러를 구독하던 로직을 `Initialize` 메소드로 분리하여, `NetworkManager`가 모드를 결정한 후 호출하도록 변경. 이를 통해 잠재적인 레이스 컨디션 및 재접속 버그를 해결하고, 코드 전반에 설명 주석을 추가.
- **`NetworkPlayerManager.cs` 컴파일 오류 수정**:
    - 리팩토링 과정에서 누락된 `HandleServerJsonMessage` 메소드를 다시 추가하여 `player_action` JSON 메시지 라우팅 기능 복원.
- **네트워크 매니저 구독 버그 수정**:
    - `ServerRoomManager`와 `NetworkPlayerManager`가 재접속 시 네트워크 이벤트를 중복으로 구독하던 버그를 수정. `Initialize` 또는 `Awake` 메소드에서 구독 전에 항상 구독을 취소하도록 변경하여 안정성 향상.
- **플레이어 재접속 및 동기화 버그 최종 수정**:
    - **호스트 재접속 버그**: `ServerRoomManager.Start`에 있던 호스트 플레이어 추가 로직을 `NetworkManager.OnLobbyCreated`로 이동하여, 호스트가 방을 새로 만들 때마다 자신의 프리팹이 생성되도록 수정.
    - **클라이언트->호스트 동기화 버그**: `NetworkManager`의 호스트 `FixedUpdate` 로직에 `UpdateFromGameState` 호출을 다시 추가하여, 호스트가 클라이언트의 상태 업데이트를 자신의 씬에 올바르게 적용하도록 수정.
- **`TerminalManager` 상호작용 텍스트 잔류 버그 수정**:
    - `TerminalManager.ToggleTerminal` 메소드 내부에 터미널이 활성화될 때 `UIEvents.InteractableFocusChanged("")`를 호출하여 상호작용 텍스트를 명시적으로 숨기도록 수정.
- **채팅 시스템 리팩토링 및 오류 수정**:
    - `ChatManager.cs`를 대대적으로 리팩토링하여 Enter키로 메시지를 전송하고, 네트워크를 통해 메시지를 송수신하며, UI에 채팅 로그를 표시하는 전체 기능 구현.

## 2025년 11월 18일 화요일

- `LootOrbVisuals.cs`, `DynamicButtonEditor.cs`, `PlayerAbilityManager.cs`, `SlimeStates.cs` 파일의 깨진 한글 문자 인코딩 수정.
- `CharacterMove.cs`에 `groundNormal` 속성을 추가하고 `GroundCheck` 메서드를 수정하여 지면 법선을 저장하도록 변경.
- `MoveState.cs`의 `Tick` 메서드를 수정하여 경사로에서 캐릭터 이동을 올바르게 처리하도록 변경 (이동 방향을 지면에 투영하고 일관된 하향 힘 적용).
- `CrouchState.cs`의 `Tick` 메서드를 수정하여 경사로에서 웅크린 상태의 캐릭터 이동을 올바르게 처리하도록 변경 (이동 방향을 지면에 투영하고 일관된 하향 힘 적용, 공중에 뜨는 문제 해결).

## 2025년 11월 20일 목요일

- **무기 교체 오류 수정**: 클라이언트가 무기를 변경할 때 `activeID`가 유효하지 않은 값(0)으로 설정되어 `IndexOutOfRangeException`이 발생하는 문제를 수정했습니다. `WeaponController.cs`의 `GETCurrentSlot` 프로퍼티와 관련 메서드에 `activeID` 유효성 검사 로직을 추가하여 안정성을 높였습니다.
- **몬스터 체력 및 전투 동기화**:
    - 클라이언트의 공격이 호스트에게 전달되지 않던 문제를 해결했습니다. 이제 클라이언트가 몬스터를 공격하면 (`BulletNetwork.cs` 또는 `MeleeWeapon.cs`에서) 호스트에게 "player_dealt_damage" 메시지를 전송합니다.
    - `ServerRoomManager.cs`에 해당 메시지를 수신하여 몬스터의 체력을 권위적으로 감소시키는 로직을 추가했습니다.
    - `MonsterState` 데이터 구조에 체력 정보를 추가하고 `NetworkManager`가 이 정보를 모든 클라이언트에게 동기화하도록 수정하여, 모든 플레이어가 몬스터의 체력을 실시간으로 확인할 수 있게 했습니다.
- **아이템 드랍 동기화**:
    - 호스트에서만 보이던 아이템 드랍 문제를 해결하기 위해 `LootManager.cs`를 새로 구현했습니다.
    - 몬스터가 죽으면 호스트는 `LootManager`를 통해 "spawn_loot" 메시지를 모든 클라이언트에게 브로드캐스트합니다.
    - 모든 클라이언트는 이 메시지를 받아 아이템 오브젝트를 동일한 위치에 생성하여 드랍 아이템이 동기화됩니다.
    - 아이템 픽업 시, 클라이언트는 호스트에게 "picked_up_loot" 메시지를 보내고, 호스트는 모든 클라이언트에게 해당 아이템을 파괴하라는 "destroy_loot" 메시지를 브로드캐스트하여 아이템이 사라지는 것 또한 동기화됩니다.
- **인벤토리 입력 잠금**:
    - 인벤토리 UI가 열렸을 때 플레이어의 움직임, 공격 등 다른 입력이 가능하던 문제를 수정했습니다.
    - `InventoryManager.cs`가 UI 포커스 상태가 변경될 때 `GameManager.Instance.SetPause()`를 호출하도록 수정하여, 게임의 중앙 일시정지 시스템을 통해 입력을 효과적으로 차단하도록 개선했습니다.
- **근접 전투 시스템 설계**:
    - 근접 공격을 위한 기본 시스템을 설계하고 `CombatManager.cs`와 `MeleeWeapon.cs` 스크립트를 생성했습니다.
    - 이 설계는 애니메이션 이벤트를 사용하여 공격 판정을 활성화하고, 기존에 구현된 네트워크 데미지 처리 시스템을 재사용하여 호스트가 근접 공격 데미지를 권위적으로 처리하도록 구성되었습니다.
- **사용자 피드백 반영**:
    - `CombatManager.cs` 파일과 클래스 이름을 `CombatController.cs`로 변경했습니다.
    - `CombatController.cs`에서 `PlayerInputs` 참조를 `GameManager.Instance`에서 가져오도록 수정했습니다.

## 2025년 11월 22일 토요일

- **SinglePlayer 모드 지원 추가 및 플레이어 관리 시스템 리팩토링**:
    - `NetworkPlayerManager.cs`를 `PlayerManager.cs`로 이름을 변경하고, `NetworkPlayer`와 `SinglePlayer`를 모두 추상화하는 `IPlayerControllable` 인터페이스를 도입하여 통합 관리 시스템을 구축.
    - `TerminalManager.cs`를 리팩토링하여 특정 플레이어 구현에 의존하지 않고 `IPlayerControllable` 인터페이스를 통해 상호작용하도록 수정.
    - `NetworkPlayer.cs`에 `IPlayerControllable` 인터페이스를 구현하고, `SinglePlayer.cs` 클래스를 새로 생성하여 `IPlayerControllable`을 구현.
    - `NetworkManager.cs`에 `SinglePlayer` 모드를 추가하고, `GameManager.cs`의 `StartOffline` 메서드를 수정하여 `NetworkManager`의 모드를 `SinglePlayer`로 설정하고 게임 씬을 로드하도록 변경.
    - `GameManager.cs`의 씬 로드 이벤트(`SceneManager.sceneLoaded`) 핸들러를 수정하여, `spaceroomScene`이 로드되고 `SinglePlayer` 모드일 때만 `PlayerManager.Instance.SpawnInitialPlayer()`를 호출하여 플레이어를 생성하도록 변경.
    - `PlayerManager.cs`의 플레이어 생성 로직을 업데이트하여 `GameManager.Instance.GameSettings.spacestationSpawnPoint.position`을 사용하여 플레이어의 초기 위치를 설정하도록 수정.
- **SinglePlayer 객체 삭제 문제 해결**:
    - `PlayerManager.ClearAllNetworkEntities()` 메서드의 로직을 수정하여 `LocalPlayer` (싱글 플레이어 또는 로컬 네트워크 플레이어)의 `GameObject`가 게임 세션 종료 시 올바르게 파괴되도록 보장.
- **게임 일시정지 상태 유지 버그 수정**:
    - `GameManager.cs`에서 `PauseGame(bool)` 메서드를 `OnPauseStateChanged` 이벤트에 구독하도록 하여, `SetPause(bool)`가 호출될 때 `Time.timeScale`이 항상 올바르게 업데이트되도록 함.
    - `MainMenuUIManager.cs`의 `OnMultiplayerButtonClicked` 및 `OnSingleplayerButtonClicked` 메서드 시작 부분에 `GameManager.Instance.SetPause(false)`를 추가하여, 새 게임 세션 시작 시 게임의 일시정지 상태가 명시적으로 재설정되도록 보장.

## 2025년 11월 23일 일요일

- **플레이어 기울이기(Q/E) 네트워크 동기화**:
    - 플레이어의 기울이기 상태(`bending` 값)가 네트워크를 통해 동기화되지 않던 문제를 수정했습니다.
    - `GameStateModels.cs`의 `PlayerState` 및 `NetworkGameState` 데이터 구조에 `bending` 필드를 추가하고 직렬화/역직렬화 로직을 업데이트했습니다.
    - `NetworkManager.cs`가 로컬 플레이어의 `PlayerInputs`에서 `bending` 값을 수집하여 `PlayerState`에 포함하도록 수정했습니다.
    - `BodySlope_Handler.cs`를 수정하여 로컬 플레이어와 원격 플레이어를 구분하고(`isMine` 플래그), 네트워크로부터 받은 값으로 기울기를 직접 설정하는 `SetSlopeFromNetwork` 메서드를 추가했습니다.
    - `NetworkPlayer.cs`가 생성될 때 `BodySlope_Handler`를 올바르게 초기화하도록 수정했습니다.
    - `PlayerManager.cs`의 `UpdateFromGameState` 메서드가 수신된 `bending` 값을 원격 플레이어의 `BodySlope_Handler`에 적용하여 기울임이 모든 클라이언트에게 동일하게 보이도록 수정했습니다.
## 2025년 11월 24일 월요일

- **무기 시스템 리팩토링**:
    - `IWeapon` 인터페이스 및 `BaseWeapon` 추상 클래스 구현.
    - `Weapon.cs`를 `RangedWeapon.cs`로 이름 변경 및 `BaseWeapon` 상속하도록 리팩토링.
    - `MeleeWeapon.cs`를 `BaseWeapon` 상속하도록 리팩토링.
    - `PlayerAbilityManager.cs`, `ViewingResistance.cs`, `SlotController.cs`, `WeaponCollision.cs`, `InputHandler.cs`, `WeaponPickupOffline.cs`, `BulletOffline.cs`, `BulletNetwork.cs`, `WeaponController.cs`를 새 `IWeapon` 인터페이스 및 `RangedWeapon` 클래스를 사용하도록 업데이트.
    - `IPlayerControllable` 인터페이스에 `IsMine` 속성 추가 및 `SinglePlayer.cs`에 `IsMine` 구현.
    - `MeleeWeapon.cs`와 `UnarmedWeapon.cs`에서 `Reload()` 메서드 제거.
- **비무장 전투 로직 구현**:
    - `UnarmedWeapon.cs`를 자체 공격 및 애니메이션 로직을 내부적으로 처리하도록 리팩토링.
    - `CombatController.cs` 삭제 (기능이 `WeaponController` 및 개별 무기 클래스로 이전됨).
    - `InputHandler.cs`를 비무장 상태일 때 조준 애니메이션(`isCombat` 애니메이터 파라미터)을 관리하도록 업데이트.

## 2025년 11월 29일 토요일

- `MeleeHitbox.cs` 스크립트에서 주석 처리된 몬스터 데미지 처리 로직을 주석 해제 및 `hit` 변수를 `other`로 수정.