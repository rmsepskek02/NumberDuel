# 오디오 리소스 폴더 구조

이 폴더는 NumberDuel의 모든 오디오 파일을 저장하는 곳입니다.

## 폴더 구조

```
Audio/
├── BGM/                    # 배경음악 (5개)
│   ├── BGM_Splash.ogg      # 로딩 화면
│   ├── BGM_Lobby.ogg       # 메뉴/대기실
│   ├── BGM_Battle.ogg      # 인게임 배틀
│   ├── BGM_Victory.ogg     # 승리
│   └── BGM_Defeat.ogg      # 패배
│
└── SFX/                    # 효과음 (22개)
    ├── UI/                 # UI 효과음
    │   ├── UI_ButtonClick.wav
    │   ├── UI_MatchFound.wav
    │   ├── UI_MessageInfo.wav
    │   ├── UI_MessageWarning.wav
    │   ├── UI_MessageError.wav
    │   └── UI_TurnStart.wav
    │
    ├── Card/               # 카드 효과음
    │   ├── Card_Draw.wav
    │   ├── Card_PlaceNormal.wav
    │   ├── Card_PlaceSecret.wav
    │   ├── Card_Attack.wav
    │   ├── Card_Destroy.wav
    │   └── Card_Hover.wav
    │
    ├── Combat/             # 전투 효과음
    │   ├── Combat_Plus.wav
    │   ├── Combat_Minus.wav
    │   ├── Combat_Multiply.wav
    │   ├── Combat_Divide.wav
    │   ├── Combat_Damage.wav
    │   └── Combat_SecretReveal.wav
    │
    ├── Joker/              # 조커 효과음
    │   ├── Joker_Draw.wav
    │   ├── Joker_Delete.wav
    │   └── Joker_Swap.wav
    │
    └── Game/               # 게임 이벤트
        ├── Game_Victory.wav
        └── Game_Defeat.wav
```

## 파일 명명 규칙

**중요**: 파일 이름은 반드시 `SoundType` enum과 **정확히 일치**해야 합니다.

### BGM 파일
- 형식: `.ogg` (Vorbis 압축 권장)
- 예시: `BGM_Battle.ogg`

### SFX 파일
- 형식: `.wav` (ADPCM 압축 권장)
- 예시: `UI_ButtonClick.wav`

## Unity 임포트 설정 권장사항

### BGM 설정
1. 파일 선택 → Inspector
2. **Load Type**: `Streaming`
3. **Compression Format**: `Vorbis`
4. **Quality**: 70% (용량 최적화)

### SFX 설정
1. 파일 선택 → Inspector
2. **Load Type**: `Decompress On Load` (짧은 효과음)
3. **Compression Format**: `ADPCM`
4. **Quality**: 100% (음질 우선)

## 리소스 파일 필수

SoundManager는 실제 오디오 파일을 로드하여 사용합니다:
- 파일이 없으면 Console에 오류 메시지가 출력됩니다
- 해당 사운드는 재생되지 않습니다
- 필요한 모든 오디오 파일을 적절한 폴더에 배치해야 합니다

## 오디오 리소스 구하기

### 무료 리소스 사이트
1. **Freesound.org** - CC 라이선스 효과음
2. **Kenney.nl** - CC0 게임 사운드 팩
3. **Mixkit.co** - 상업적 사용 가능
4. **Unity Asset Store** - Free 섹션

### 주의사항
- 라이선스 확인 필수
- 상업적 사용 가능 여부 체크
- 저작자 표기 조건 확인

## 파일 추가 방법

1. 오디오 파일을 해당 폴더에 복사
2. Unity에서 자동 임포트
3. 파일 이름이 `SoundType` enum과 일치하는지 확인
4. 코드 수정 없이 자동으로 작동

## 문제 해결

### "AudioClip을 찾을 수 없습니다" 오류
- 파일 이름 확인 (대소문자 구분)
- 파일 위치 확인 (올바른 하위 폴더)
- enum 이름과 정확히 일치하는지 확인

### 사운드가 재생되지 않음
- SoundManager가 씬에 존재하는지 확인
- 볼륨 설정 확인 (F5 테스트 UI 사용)
- Console 창에서 에러 메시지 확인
