# NumberDuel

Unity 기반의 멀티플레이어 숫자 카드 대결 게임입니다. Photon PUN2를 사용하여 실시간 네트워크 게임을 구현했습니다.

## 게임 소개

**NumberDuel**은 숫자 카드와 연산자 카드(+, -, ×, ÷)를 사용하여 상대방과 대결하는 턴제 카드 게임입니다. Secret 카드(뒷면), 특수 효과를 가진 Joker 카드 등을 전략적으로 활용하여 상대방의 HP를 0으로 만들면 승리합니다.

- **제작사**: GoroCompany
- **플랫폼**: PC, Mobile (반응형 디자인)
- **엔진**: Unity 2021.3 이상

## 시작하기

### 필수 요구사항

1. **Unity 2021.3 이상**
2. **Photon PUN2** (Photon Unity Networking 2)
3. **Firebase Unity SDK**
4. **DOTween** (애니메이션용)

### 프로젝트 설정

#### 1. 저장소 클론
```bash
git clone [repository-url]
cd NumberDuel
```

#### 2. Unity에서 프로젝트 열기
- Unity Hub에서 "Open" 클릭
- 클론한 프로젝트 폴더 선택

#### 3. Firebase 설정

Firebase 관련 대용량 바이너리 파일은 Git에서 제외되어 있으므로 직접 설치가 필요합니다.

##### Firebase Unity SDK 설치
1. [Firebase Unity SDK](https://firebase.google.com/download/unity) 다운로드
2. 다음 패키지를 Unity 프로젝트에 임포트:
   - `FirebaseAuth.unitypackage` (인증)
   - `FirebaseDatabase.unitypackage` (실시간 데이터베이스)
   - 필요한 다른 Firebase 서비스

##### google-services.json 설정
1. [Firebase Console](https://console.firebase.google.com/)에서 프로젝트 생성
2. Android/iOS 앱 추가
3. `google-services.json` 파일 다운로드
4. **중요**: 다운로드한 `google-services.json` 파일을 `Assets/` 폴더에 배치
   ```
   NumberDuel/
   └── Assets/
       └── google-services.json
   ```

##### Firebase 데이터베이스 규칙 설정
Firebase Console에서 Realtime Database 또는 Firestore 규칙을 프로젝트에 맞게 설정하세요.

#### 4. Photon PUN2 설정

1. [Photon Engine](https://www.photonengine.com/)에서 계정 생성
2. PUN2 App ID 발급
3. Unity에서 `Window > Photon Unity Networking > PUN Wizard`
4. App ID 입력

### 프로젝트 실행

#### 씬 순서
게임을 테스트하려면 다음 순서로 씬을 실행하세요:

1. `SplashScene` - 초기 로딩 및 설정
2. `JoinScene` - Photon 네트워크 연결 및 닉네임 설정
3. `LobbyScene` - 방 생성/참가
4. `GameScene` - 메인 게임 플레이

#### 멀티플레이어 테스트
- 프로젝트를 빌드하여 여러 클라이언트 인스턴스 생성
- 각 클라이언트는 `ClientSettings.txt`에 설정 저장 (창 해상도 등)
- Photon Cloud 또는 로컬 서버 사용

### 빌드 설정

- **기본 해상도**: 1280x720
- **화면 방향**: 자동 회전 활성화
- **Company Name**: GoroCompany
- **Product Name**: NumberDuel

## 아키텍처 개요

### 게임 흐름
- 싱글톤 패턴 기반의 매니저 시스템
- Photon PUN2를 통한 네트워크 동기화
- 커스텀 RPC 시스템으로 턴 관리

### 주요 매니저
- **NetworkGameManager**: 모든 네트워크 동기화 처리
- **TurnManager**: 턴 흐름 제어
- **InGameManager**: 게임 루프 관리
- **PhotonManager**: Photon 콜백 처리

자세한 아키텍처 정보는 `CLAUDE.md` 파일을 참조하세요.

## 개발 가이드

### 디렉토리 구조
```
NumberDuel/
├── Assets/
│   ├── Scenes/           # 게임 씬
│   ├── Scripts/
│   │   ├── Managers/     # 싱글톤 매니저
│   │   ├── Objects/      # 게임 오브젝트 (Card, Zone 등)
│   │   └── Utills/       # 유틸리티 클래스
│   ├── Model/            # 3D 모델, 폰트
│   └── Resources/        # 런타임 로드 리소스
└── ProjectSettings/
```

### 코드 컨벤션
- 모든 네트워크 동기화는 `NetworkGameManager`를 통해 처리
- `[PunRPC]` 속성을 사용한 RPC 메서드
- 싱글톤 매니저는 `Singleton<T>` 또는 `SingletonDontDestroy<T>` 상속

## 문제 해결

### Firebase 관련 오류
- `google-services.json` 파일이 `Assets/` 폴더에 있는지 확인
- Firebase Unity SDK 버전 호환성 확인
- Unity Editor를 재시작

### Photon 연결 오류
- App ID가 올바르게 설정되었는지 확인
- 인터넷 연결 상태 확인
- Photon Dashboard에서 CCU 제한 확인

### 빌드 오류
- Platform별 필요한 패키지 설치 확인
- Build Settings에서 씬이 포함되었는지 확인

## 라이선스

[라이선스 정보 추가]

## 문의

[문의처 정보 추가]
