# YuJanggi

YuJanggi는 C# TCP 서버와 기존 Unity 장기 게임을 연결하는 온라인 장기 프로젝트입니다.
서버가 방, 턴, 수의 유효성, 승패를 판정하고 Unity 클라이언트는 입력과 화면 표시를 담당하는 서버 권위형 구조를 목표로 합니다.

## 프로젝트 구성

| 프로젝트 | 역할 |
| --- | --- |
| `YuJanggiServer` | TCP 접속, 클라이언트 세션 및 게임 진행 관리 |
| `YuJanggiClient` | 서버 통신을 확인하기 위한 콘솔 클라이언트 |
| `YuJanggiCommon` | 서버와 클라이언트가 공유하는 메시지 타입과 패킷 프로토콜 |
| `YuJanggi` | 서버에서 사용하는 장기 코어 코드 |
| `D:\Git\YuJanggi` | Unity 6.3 기반 장기 클라이언트 별도 저장소 |

## 현재 구현 상태

- 여러 TCP 클라이언트의 비동기 접속 및 메시지 수신
- 접속 클라이언트 목록 조회와 전체 연결 종료 명령
- 4바이트 Big Endian 본문 길이 헤더와 JSON 본문을 사용하는 메시지 프로토콜
- 최대 4 KiB 메시지 크기 검증
- 장기 보드 초기화, 기물 이동 후보 계산, 궁성 규칙, 턴, 기록 및 점수 모델
- 이동 후 왕의 피격 여부와 합법 수 검사
- Unity 클라이언트의 로컬 및 AI 대전
- Unity 클라이언트의 보드 표현, 입력, 사운드, 결과 UI 및 리플레이
- `GameSession`과 `IPlayerController`를 통한 Unity 게임 흐름 분리

다음 기능은 아직 구현되지 않았습니다.

- 플레이어 참가와 식별
- 방 생성, 입장, 퇴장 및 준비 상태 관리
- 서버 게임 인스턴스와 장기 코어 연결
- 이동 요청 처리와 게임 상태 동기화
- Unity TCP 네트워크 계층과 `NetworkController`
- Unity 로비의 온라인 방 생성 및 입장 UI

세부 작업 순서와 완료 조건은 [ToDo.md](ToDo.md)를 참고합니다.

## 개발 환경

- .NET SDK 10
- C#
- Unity 6000.3.1f1

서버 저장소의 .NET 프로젝트는 `net10.0`을 대상으로 합니다. Unity 프로젝트는 별도 저장소에서 관리되며, 양쪽에 존재하는 장기 Core 코드의 단일 원본과 동기화 방법을 정해야 합니다. `net10.0` DLL을 Unity에서 직접 참조하는 방식은 사용하지 않습니다.

## 실행

서버 실행:

```powershell
dotnet run --project .\YuJanggiServer\YuJanggiServer.csproj
```

서버는 기본적으로 TCP 7777 포트에서 대기합니다.

서버 콘솔 명령:

- `clientList`: 현재 접속 목록 출력
- `clientClear`: 모든 클라이언트 연결 종료

콘솔 클라이언트 실행:

```powershell
dotnet run --project .\YuJanggiClient\YuJanggiClient.csproj
```

현재 콘솔 클라이언트는 시작 메시지만 출력하며 서버 접속은 구현되지 않았습니다.

## 통신 규약

패킷은 다음 순서로 전송합니다.

1. JSON 본문 길이를 나타내는 4바이트 Big Endian 정수
2. UTF-8 JSON으로 직렬화한 메시지 본문

현재 메시지 타입:

- 로비: `Join`, `CreateRoom`, `JoinRoom`, `Ready`
- 게임: `GameStart`, `MoveRequest`, `MoveResult`, `TurnChanged`, `GameEnd`
- 오류: `Error`

## 개발 원칙

- 서버가 게임 상태와 수의 유효성을 최종 판정합니다.
- 클라이언트 입력은 요청으로 취급하고 서버의 결과를 받은 뒤 화면에 반영합니다.
- 네트워크 DTO와 장기 도메인 모델을 분리합니다.
- 작업 단위는 작게 유지하고 기능 추가와 리팩터링을 가능한 한 분리합니다.
- 인증, 암호화, 영속 저장소는 초기 학습 범위에 포함하지 않습니다.
