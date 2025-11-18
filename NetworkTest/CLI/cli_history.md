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