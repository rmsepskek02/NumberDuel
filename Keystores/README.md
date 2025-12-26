# NumberDuel Keystores

## Debug Keystore (팀 공유용)

이 디렉토리의 `debug.keystore`는 팀 전체가 공유하는 디버그 키스토어입니다.

### SHA-1 지문
```
SHA1: 92:FD:44:A8:0F:25:2C:2E:45:62:3F:87:D8:D9:BA:E4:6F:A6:3A:03
SHA256: E0:EE:2E:16:11:64:50:88:01:9C:82:08:78:D0:C2:0F:DA:4A:9A:BE:F0:53:F0:74:95:46:C5:B7:41:65:CB:B5
```

### 키스토어 정보
- **파일명**: debug.keystore
- **Alias**: androiddebugkey
- **Store Password**: android
- **Key Password**: android
- **발급일**: 2022-02-22
- **만료일**: 2052-02-15
- **알고리즘**: RSA 2048-bit

### 사용 목적
- 모든 개발자가 동일한 SHA-1 지문을 사용하여 Firebase, Google Play Games 등의 서비스 테스트
- 디버그 빌드 간 일관성 유지
- 개발 환경 간 서명 불일치 문제 방지

### SHA-1 확인 방법
```bash
keytool -list -v -keystore Keystores/debug.keystore -alias androiddebugkey -storepass android -keypass android
```

### Unity 설정 방법

1. Unity Editor 열기
2. `Edit > Project Settings > Player`
3. Android 탭 선택 (안드로이드 아이콘)
4. `Publishing Settings` 섹션 펼치기
5. 다음과 같이 설정:
   - ✅ `Custom Keystore` 체크
   - `Browse Keystore` 클릭 → `Keystores/debug.keystore` 선택
   - Keystore password: `android`
   - Keystore 선택 후 Alias dropdown에서 `androiddebugkey` 선택
   - Alias password: `android`

### 새로운 팀원 온보딩

새 팀원이 프로젝트를 클론한 후:

1. Git에서 프로젝트 클론
2. Unity에서 프로젝트 열기
3. 위의 "Unity 설정 방법" 단계를 따라 키스토어 설정
4. 빌드 테스트 (APK가 정상적으로 서명되는지 확인)

### Firebase / Google Play Games 설정

이 디버그 키스토어의 SHA-1이 다음 서비스에 이미 등록되어 있습니다:
- ✅ Firebase Console
- ✅ Google Play Console (Google Play Games Services)

새로운 Firebase 프로젝트나 서비스를 추가할 때는 위의 SHA-1을 등록하세요.

## ⚠️ 보안 주의사항

### Git에 커밋해도 되는 것
- ✅ `debug.keystore` (디버그 전용, 보안 위험 낮음)
- ✅ 이 README.md

### 절대 Git에 커밋하지 말 것
- ❌ `release.keystore` (릴리즈용 키스토어)
- ❌ 실제 프로덕션 서명 키
- ❌ 키스토어 비밀번호를 담은 별도 파일

### Release Keystore 관리

릴리즈 키스토어는:
1. 안전한 곳에 별도 보관
2. 환경변수나 CI/CD 시크릿으로 관리
3. 팀 리드나 지정된 담당자만 접근 가능하도록 설정

---

**마지막 업데이트**: 2025-12-26
**관리자**: GoroCompany
