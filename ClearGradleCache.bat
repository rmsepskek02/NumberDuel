@echo off
echo ========================================
echo Gradle Cache 초기화 스크립트
echo ========================================
echo.

echo [1/3] Unity Library 폴더 삭제 중...
if exist "%~dp0Library" (
    rd /s /q "%~dp0Library"
    echo ✅ Library 폴더 삭제 완료
) else (
    echo ℹ️ Library 폴더 없음
)

echo.
echo [2/3] Gradle 로컬 캐시 삭제 중...
if exist "%~dp0Builds\Android\NumberDuel_Dev\.gradle" (
    rd /s /q "%~dp0Builds\Android\NumberDuel_Dev\.gradle"
    echo ✅ 로컬 Gradle 캐시 삭제 완료
) else (
    echo ℹ️ 로컬 Gradle 캐시 없음
)

echo.
echo [3/3] 사용자 Gradle 캐시 삭제 중...
if exist "%USERPROFILE%\.gradle\caches" (
    rd /s /q "%USERPROFILE%\.gradle\caches"
    echo ✅ 사용자 Gradle 캐시 삭제 완료
) else (
    echo ℹ️ 사용자 Gradle 캐시 없음
)

echo.
echo ========================================
echo ✅ Gradle Cache 초기화 완료!
echo ========================================
echo.
echo 다음 빌드는 Clean Build가 됩니다.
echo.
pause
