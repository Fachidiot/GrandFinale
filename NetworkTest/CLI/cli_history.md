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