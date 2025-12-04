# ServerLib
게임 서버를 목표로 작성한 c#기반 프레임워크입니다.

멀티스레드 Job System, 비동기 소켓 IO, Redis 연동 등

Components:

Job/JobQueue 
- 멀티스레드 환경에서의 안전한 작업 분배.

Connector/Listener
- 비동기 SoketAsyncEventArgs 기반
- 서버/클라이언트 접속 처리 컴포넌트

SendBuffer/RecvBuffer
- 가변 버퍼 + shrink 정책

Session/SessionManager
- 세션 단위 송수신 흐름 제어
- 브로드캐스트 기능 제공

RedisCmd/RedisManager
- Redis와 비동기 명령 송수신을 위한 래퍼 클래스 
