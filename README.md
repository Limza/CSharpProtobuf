0. https://github.com/protocolbuffers/protobuf/releases/tag/v3.13.0 구글 프로토버프 c# 다운

1. 구글 프로토버프 파일 복사

2. 구글 프로트버프에 필요한 라이브러리 설치를 위해
    https://github.com/GlitchEnzo/NuGetForUnity 임포트
    Assets에 있는 Nuget, package 의 Any Platform, Load on startup 체크

3. nuget에서 필요한 라이브러리 설치
    System.Buffers System.Memory

4. 프로토버프를 사용하기 위해 define을 유니티 프로젝트에 추가
    플레이어 설정 -> Other Settings -> Scripting Define Symbols 에 GOOGLE_PROTOBUF_SUPPORT_SYSTEM_MEMORY 추가

5. 프로토버프를 사용하기 위해 unsafe 코드 허용
    플레이어 설정 -> Other Settings -> Allow 'unsafe' Code 체크

6. .proto 파일을 cs파일로 변환하기 위해 
    https://github.com/protocolbuffers/protobuf/releases/tag/v3.13.0 에서
    protoc win64 설치

7. protoc로 proto 파일을 cs 형태로 변환, 
    protoc --csharp_out=./ protocol.proto
    protoc --csharp_out=./ session.proto
    protoc --csharp_out=./ game.proto