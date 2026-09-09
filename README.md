# YuJanggi.Server

.NET 10 기반 장기 게임 서버입니다.

클라이언트의 이동 요청을 서버에서 검증하고, 두 플레이어에게 같은 보드 상태를 전달하는 구조를 구현했습니다.

[Unity](https://github.com/SeokJinYoo98/YuJanggi.Unity) / [Core](https://github.com/SeokJinYoo98/YuJanggi.Core) / [포트폴리오](https://app.notion.com/p/3b48a299d1c481019a37cf5ea2019cdd)

## 주요 기능

- **접속:** 여러 TCP 클라이언트의 비동기 접속과 송수신 처리

- **매칭:** 참가, 매칭 시작과 취소, 초와 한 진영 무작위 배정

  먼저 대기한 두 플레이어를 매칭한 뒤 초와 한을 무작위로 정합니다. 두 클라이언트에 같은 배정 안내를 전달합니다.

- **대국:** 합법 수 조회, 이동 검증, 전체 보드 상태 전송

- **채팅:** 같은 대국에 참가한 플레이어 간 메시지 전송

- **종료:** 연결 종료 시 대기열과 게임 세션 정리

## 설계에서 집중한 점

- **서버에서 최종 검증:** 참가자, 턴, 좌표와 소유권을 확인하고 Core로 합법 수를 판정합니다. 성공한 이동만 양쪽에 전달합니다.

- **게임 상태 분리:** 대국마다 GameSession과 Core 인스턴스를 두고, 게임별 Lock으로 조회와 이동 요청을 순차 처리합니다.

- **TCP 메시지 경계 처리:** 길이 헤더로 패킷을 구분하고 연결별 송신 제어로 메시지 바이트가 섞이지 않도록 했습니다.

- **공용 메시지 계약:** 요청과 응답을 YuJanggiCommon으로 분리해 서버와 클라이언트가 같은 DTO를 사용하도록 구성했습니다.

## 실행

Git과 .NET SDK 10이 필요합니다.

저장소를 받을 때 Core submodule을 함께 내려받습니다.

```powershell
git clone --recurse-submodules https://github.com/SeokJinYoo98/YuJanggi.Server.git
cd YuJanggi.Server
```

서버를 실행합니다.

```powershell
dotnet run --project .\YuJanggiServer\YuJanggiServer.csproj
```

별도 터미널 두 개에서 콘솔 클라이언트를 실행하면 대국 흐름을 확인할 수 있습니다.

```powershell
dotnet run --project .\YuJanggiClient\YuJanggiClient.csproj
```

서버는 TCP 7777 포트를 사용합니다.

매칭 안내와 기존 메시지 호환성 테스트를 실행합니다.

```powershell
dotnet test .\Tests\YuJanggi.Server.Tests.csproj
```

## 통신 방식

패킷은 4바이트 Big Endian 길이와 UTF-8 JSON 본문으로 구성됩니다.

JSON 본문의 최대 크기는 4 KiB입니다.

이동 결과에는 전체 보드 상태를 포함합니다. 상태 버전과 재접속 시 재동기화는 아직 지원하지 않습니다.

## 콘솔 명령

- **서버:** clientList, clientClear

- **클라이언트:** select x z, move x z, 일반 채팅, /quit

## 현재 상태

- 콘솔 클라이언트 기준 참가, 매칭, 이동, 채팅 흐름을 실행할 수 있습니다.

- Unity 클라이언트의 온라인 대국 연결은 진행 중입니다.

- 재접속, 상태 재동기화, 인증, 암호화, 전적 저장은 구현되지 않았습니다.

- 정상 종료, 기권, 시간패의 전체 네트워크 흐름은 추가 구현이 필요합니다.

- Core는 Git submodule로 관리하므로 Unity와 같은 검증된 커밋을 사용해야 합니다.
