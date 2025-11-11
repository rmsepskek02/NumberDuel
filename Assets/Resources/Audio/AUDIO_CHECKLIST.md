# 오디오 파일 체크리스트

NumberDuel에 필요한 모든 오디오 파일 목록입니다.

## BGM (배경음악) - 5개

폴더: `Assets/Resources/Audio/BGM/`

- [ ] `BGM_Splash.ogg` - 로딩 화면 BGM
- [ ] `BGM_Lobby.ogg` - 메뉴/대기실 BGM
- [ ] `BGM_Battle.ogg` - 인게임 배틀 BGM (루프)
- [ ] `BGM_Victory.ogg` - 승리 BGM (짧은 팡파레)
- [ ] `BGM_Defeat.ogg` - 패배 BGM (짧은 음악)

**권장 설정:**
- 파일 형식: `.ogg` (Vorbis 압축)
- Load Type: `Streaming`
- Quality: 70%
- 길이:
  - Splash/Lobby: 1~2분 (루프 가능)
  - Battle: 2~3분 (루프)
  - Victory/Defeat: 5~10초

---

## SFX - UI (6개)

폴더: `Assets/Resources/Audio/SFX/UI/`

- [ ] `UI_ButtonClick.wav` - 버튼 클릭음 (짧고 경쾌한)
- [ ] `UI_MatchFound.wav` - 매칭 성공 알림 (긍정적인 소리)
- [ ] `UI_MessageInfo.wav` - 일반 메시지 알림 (중립적)
- [ ] `UI_MessageWarning.wav` - 경고 메시지 (조심스러운)
- [ ] `UI_MessageError.wav` - 에러 메시지 (부정적인)
- [ ] `UI_TurnStart.wav` - 턴 시작 신호음 (주목을 끄는)

**권장 설정:**
- 파일 형식: `.wav`
- Load Type: `Decompress On Load`
- Compression: `ADPCM`
- 길이: 0.1~0.5초

---

## SFX - Card (6개)

폴더: `Assets/Resources/Audio/SFX/Card/`

- [ ] `Card_Draw.wav` - 카드 드로우 (휙 하는 소리)
- [ ] `Card_PlaceNormal.wav` - 일반 카드 배치 (톡 하는 소리)
- [ ] `Card_PlaceSecret.wav` - 시크릿 카드 배치 (살짝 다른 톤)
- [ ] `Card_Attack.wav` - 카드 공격 (휘두르는 소리)
- [ ] `Card_Destroy.wav` - 카드 파괴 (찢어지는/터지는 소리)
- [ ] `Card_Hover.wav` - 마우스 호버 (미묘한 소리, 볼륨 낮게)

**권장 설정:**
- 파일 형식: `.wav`
- Load Type: `Decompress On Load`
- Compression: `ADPCM`
- 길이: 0.1~0.3초

---

## SFX - Combat (6개)

폴더: `Assets/Resources/Audio/SFX/Combat/`

- [ ] `Combat_Plus.wav` - 덧셈 연산 (+)
- [ ] `Combat_Minus.wav` - 뺄셈 연산 (-)
- [ ] `Combat_Multiply.wav` - 곱셈 연산 (×)
- [ ] `Combat_Divide.wav` - 나눗셈 연산 (÷)
- [ ] `Combat_Damage.wav` - 데미지 적용 (타격음)
- [ ] `Combat_SecretReveal.wav` - 시크릿 카드 공개 (뒤집는 소리)

**권장 설정:**
- 파일 형식: `.wav`
- Load Type: `Decompress On Load`
- Compression: `ADPCM`
- 길이: 0.2~0.5초

**팁:**
- 4가지 연산자 각각 다른 음높이로 구분하면 좋음
- Plus: 높은 음
- Minus: 낮은 음
- Multiply: 강한 음
- Divide: 부드러운 음

---

## SFX - Joker (3개)

폴더: `Assets/Resources/Audio/SFX/Joker/`

- [ ] `Joker_Draw.wav` - 조커 드로우 효과 (카드 뽑는 소리)
- [ ] `Joker_Delete.wav` - 조커 삭제 효과 (파괴음)
- [ ] `Joker_Swap.wav` - 조커 교환 효과 (교환/이동 소리)

**권장 설정:**
- 파일 형식: `.wav`
- Load Type: `Decompress On Load`
- Compression: `ADPCM`
- 길이: 0.3~0.6초

**팁:**
- 조커는 특별한 효과이므로 "마법같은" 또는 "특별한" 사운드 사용

---

## SFX - Game Event (2개)

폴더: `Assets/Resources/Audio/SFX/Game/`

- [ ] `Game_Victory.wav` - 승리 팡파레 (짧은 축하 소리)
- [ ] `Game_Defeat.wav` - 패배 사운드 (낙담하는 소리)

**권장 설정:**
- 파일 형식: `.wav`
- Load Type: `Decompress On Load`
- Compression: `ADPCM`
- 길이: 1~3초

---

## 총계

- **BGM**: 5개
- **SFX**: 22개
- **총합**: 27개 오디오 파일

---

## 파일 확인 방법

Unity 에디터에서:
1. `Assets/Resources/Audio/` 폴더 열기
2. 각 하위 폴더에 파일이 있는지 확인
3. 파일 이름이 위 목록과 **정확히 일치**하는지 확인 (대소문자 구분)
4. Console 창에서 `[SoundManager] AudioClip 로드 성공` 메시지 확인

---

## 테스트 방법

1. Play 모드 진입
2. F5 키로 사운드 테스트 UI 열기
3. "BGM 재생" 버튼 클릭 → `BGM_Battle` 재생 확인
4. "SFX 테스트" 버튼 클릭 → `UI_ButtonClick` 재생 확인
5. Console 창에서 오류 메시지 없는지 확인

---

## 문제 해결

### "AudioClip을 찾을 수 없습니다" 오류
1. 파일 이름 확인 (정확히 일치해야 함)
2. 파일 위치 확인 (올바른 폴더)
3. 파일 확장자 확인 (.ogg 또는 .wav)
4. Unity에서 리임포트 (우클릭 → Reimport)

### 소리가 나지 않음
1. F5로 테스트 UI 열기
2. 마스터 볼륨 확인 (0보다 큰지)
3. 해당 카테고리 볼륨 확인
4. PC 시스템 볼륨 확인
