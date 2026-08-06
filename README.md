<div align="center">

# YuJanggi.Server

**자동 매칭과 장기 수 검증을 담당하는 .NET 10 서버 권위형 TCP 게임 서버**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-TCP_Server-239120?logo=csharp&logoColor=white)
![Protocol](https://img.shields.io/badge/protocol-Length_Prefix%20%2B%20JSON-2563eb)
![Status](https://img.shields.io/badge/status-playable_CLI-f59e0b)

[Unity Client](https://github.com/SeokJinYoo98/YuJanggi.Unity) ·
[Shared Core](https://github.com/SeokJinYoo98/YuJanggi.Core) ·
[Portfolio](https://app.notion.com/p/3b48a299d1c481019a37cf5ea2019cdd)

</div>

## 프로젝트 소개

YuJanggi.Server는 클라이언트의 입력을 요청으로 받고 서버가 참가자, 턴, 기물 소유권과 합법 수를 검증한 뒤 결과를 전파하는 C# TCP 게임 서버입니다.

두 콘솔 클라이언트로 참가, 자동 매칭, 합법 수 조회, 장기말 이동과 게임 내 채팅을 직접 실행할 수 있습니다. 장기 규칙은 submodule로 연결한 [YuJanggi.Core](https://github.com/SeokJinYoo98/YuJanggi.Core)가 담당합니다.

> **현재 상태**
>
> 콘솔 클라이언트 기준 온라인 대국 흐름은 실행 가능합니다. Unity 네트워크 계층, 재접속, 영속 저장과 운영 환경 보안은 아직 구현되지 않았습니다.

## 구현 기능

| 영역 | 구현 내용 |
| --- | --- |
| Connection | 다중 TCP 클라이언트 비동기 접속·수신, 연결별 동시 송신 제어 |
| Join | 플레이어 이름 검증, Player ID 발급, 중복 참가와 중복 이름 거부 |
| Matchmaking | 자동 매칭 시작·취소, 두 플레이어 연결, Cho·Han 진영 배정 |
| Game | 게임별 Core 인스턴스, 합법 수 조회, 서버 권위형 이동 검증 |
| Sync | 이동 결과와 전체 기물 스냅샷을 양쪽 플레이어에게 전파 |
| Chat | 같은 GameSession 참가자 사이의 게임 내 채팅 |
| Disconnect | 매칭 대기열과 게임 세션 정리, 상대에게 종료 이벤트 전송 |
| Operations | 접속 목록 조회와 전체 연결 종료 콘솔 명령 |

## 서버 흐름

~~~mermaid
flowchart LR
    A["TCP Client A"] --> CS["ClientSession"]
    B["TCP Client B"] --> CS
    CS --> D["Message Dispatch"]
    D --> J["Join"]
    D --> Q["Matchmaking Queue"]
    Q --> GS["GameSession"]
    D --> GS
    GS --> CORE["YuJanggi.Core"]
    CORE --> R["MoveResult / Board Snapshot"]
    R --> A
    R --> B
    COMMON["YuJanggiCommon DTO"] -. shared .-> A
    COMMON -. shared .-> B
    COMMON -. shared .-> D
~~~

- <code>ClientSession</code>: 연결, 참가자 정보, 게임·진영 정보와 송신 Lock을 관리합니다.
- <code>YuJanggiServer</code>: 메시지 디스패치, 참가, 매칭, 채팅과 연결 종료를 조율합니다.
- <code>GameSession</code>: 게임별 <code>MatchModel</code>과 두 플레이어를 소유하고 전용 Lock으로 요청을 직렬화합니다.
- <code>YuJanggiCommon</code>: 서버와 클라이언트가 공유하는 메시지 Envelope과 DTO입니다.
- <code>Core</code>: 턴, 기물 소유권, 합법 수와 실제 상태 변경을 판정합니다.

## 프로젝트 구성

~~~text
YuJanggiServer          # TCP 서버와 게임 세션
YuJanggiClient          # 대국 흐름을 확인하는 콘솔 클라이언트
YuJanggiCommon
└── YuJanggiCommon      # 메시지 타입, DTO와 패킷 Codec
Core                    # YuJanggi.Core Git submodule
YuJanggiServer.sln
~~~

| 프로젝트 | Target | 역할 |
| --- | --- | --- |
| <code>YuJanggiServer</code> | net10.0 | 접속, 매칭, 대국과 채팅 |
| <code>YuJanggiClient</code> | net10.0 | 콘솔 기반 프로토콜·대국 확인 |
| <code>YuJanggiCommon</code> | net10.0 | 공용 메시지 계약 |
| <code>Core</code> | net10.0 / Unity UPM | 장기 규칙과 대국 상태 |

## 시작하기

### 요구 사항

- Git
- .NET SDK 10

### Clone

Core submodule을 함께 받습니다.

~~~powershell
git clone --recurse-submodules https://github.com/SeokJinYoo98/YuJanggi.Server.git
cd YuJanggi.Server
~~~

이미 Clone한 저장소라면 다음 명령으로 Core를 초기화합니다.

~~~powershell
git submodule update --init --recursive
~~~

### 서버 실행

~~~powershell
dotnet run --project .\YuJanggiServer\YuJanggiServer.csproj
~~~

서버는 현재 모든 인터페이스의 TCP <code>7777</code> 포트에서 대기합니다.

서버 콘솔 명령:

| 명령 | 동작 |
| --- | --- |
| <code>clientList</code> | 현재 접속한 클라이언트 목록 출력 |
| <code>clientClear</code> | 모든 클라이언트 연결 종료 |

### 콘솔 대국 실행

서버와 별도로 두 개의 터미널에서 콘솔 클라이언트를 실행합니다.

~~~powershell
dotnet run --project .\YuJanggiClient\YuJanggiClient.csproj
~~~

각 클라이언트에서 플레이어 이름을 입력하고 자동 매칭을 시작합니다. 매칭 후 사용할 수 있는 명령은 다음과 같습니다.

| 입력 | 동작 |
| --- | --- |
| <code>select x z</code> | 선택한 기물의 합법 수 조회 |
| <code>move x z</code> | 선택한 기물을 목적지로 이동 요청 |
| 일반 문자열 | 상대 플레이어에게 채팅 전송 |
| <code>/quit</code> | 클라이언트 종료 |

## 통신 규약

TCP 메시지 경계를 구분하기 위해 Length-Prefixed JSON 형식을 사용합니다.

~~~text
┌────────────────────────────┬─────────────────────────────┐
│ 4-byte Big Endian length   │ UTF-8 JSON body            │
└────────────────────────────┴─────────────────────────────┘
~~~

- 헤더: JSON 본문 길이를 나타내는 4바이트 Big Endian 정수
- 본문: <code>Type</code>, <code>RequestId</code>, <code>Payload</code>를 가진 JSON 메시지
- 최대 본문 크기: 4 KiB
- 0 이하 또는 제한을 초과한 본문 길이는 연결 처리 전에 거부

### 메시지 종류

| 흐름 | 메시지 |
| --- | --- |
| 참가 | <code>Join</code> |
| 매칭 | <code>MatchmakingStart</code>, <code>MatchmakingCancel</code>, <code>MatchmakingStatus</code>, <code>MatchFound</code> |
| 대국 | <code>GameStart</code>, <code>LegalMovesRequest</code>, <code>LegalMovesResult</code>, <code>MoveRequest</code>, <code>MoveResult</code>, <code>TurnChanged</code>, <code>GameEnd</code> |
| 채팅 | <code>GameChatSend</code>, <code>GameChatReceived</code> |
| 오류 | <code>Error</code> |

## 이동 검증

<code>MoveRequest</code>는 다음 순서로 검증합니다.

1. 요청 세션이 현재 게임의 참가자인지 확인
2. 요청자의 진영과 현재 턴 비교
3. 출발지와 도착지가 보드 안인지 확인
4. 출발지 기물 존재 여부와 소유권 확인
5. <code>MatchModel.TryMove()</code>로 최종 합법성 확인
6. 성공한 이동만 양쪽 플레이어에게 <code>MoveResult</code>와 보드 스냅샷 전송

잘못된 요청은 상태를 변경하지 않고 구체적인 <code>ErrorCode</code>로 응답합니다.

## 동시성과 세션 정리

- 연결별 <code>SemaphoreSlim</code>로 여러 비동기 응답의 패킷이 섞이지 않게 합니다.
- 게임별 Lock으로 합법 수 조회와 이동 적용을 직렬화합니다.
- 연결 종료 시 접속 목록, 매칭 대기열과 게임 목록에서 세션을 제거합니다.
- 진행 중 게임의 상대에게 <code>OpponentLeft</code> 종료 이벤트를 전송합니다.

## 현재 제한 사항

- Unity 클라이언트의 TCP 계층과 <code>NetworkController</code>는 연결 전입니다.
- 상태 버전, 재동기화, 재접속은 구현되지 않았습니다.
- 인증, 암호화, 데이터베이스와 전적 저장은 현재 범위에 포함되지 않습니다.
- 포트와 서버 설정은 실행 인자가 아닌 코드에 고정되어 있습니다.
- 완전한 승패·기권·타임아웃 네트워크 흐름은 추가 구현이 필요합니다.

## 관련 저장소

- [YuJanggi.Unity](https://github.com/SeokJinYoo98/YuJanggi.Unity): Unity 입력, 세션과 화면 표현
- [YuJanggi.Core](https://github.com/SeokJinYoo98/YuJanggi.Core): Client와 Server가 공유하는 장기 규칙
