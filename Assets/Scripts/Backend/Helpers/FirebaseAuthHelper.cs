using System.Collections.Generic;
using System.Linq;
using Firebase;

/// <summary>
/// Firebase Authentication 관련 유틸리티 헬퍼
/// - Firebase 에러 메시지 변환
/// - Provider 확인 헬퍼
/// </summary>
public static class FirebaseAuthHelper
{
    #region Provider Checks
    /// <summary>
    /// Google Provider 확인 (google.com 또는 playgames.google.com)
    /// </summary>
    public static bool HasGoogleProvider(List<string> providers)
    {
        if (providers == null)
            return false;

        return providers.Contains("google.com") || providers.Contains("playgames.google.com");
    }

    /// <summary>
    /// Password Provider 확인
    /// </summary>
    public static bool HasPasswordProvider(List<string> providers)
    {
        if (providers == null)
            return false;

        return providers.Contains("password");
    }

    /// <summary>
    /// Anonymous Provider 확인
    /// </summary>
    public static bool HasAnonymousProvider(List<string> providers)
    {
        if (providers == null)
            return false;

        return providers.Contains("anonymous");
    }
    #endregion

    #region Error Message Conversion
    /// <summary>
    /// Firebase 에러 메시지를 한글로 변환
    /// </summary>
    public static string GetFirebaseErrorMessage(FirebaseException ex)
    {
        string errorCode = ex.Message;

        // 계정 이미 존재 (다른 Provider로 가입됨)
        if (errorCode.Contains("account-exists-with-different-credential"))
            return "ACCOUNT_EXISTS"; // 특별한 코드 반환

        if (errorCode.Contains("EMAIL_EXISTS") || errorCode.Contains("already in use"))
            return "이미 사용 중인 이메일입니다.";

        if (errorCode.Contains("INVALID_EMAIL") || errorCode.Contains("badly formatted"))
            return "올바르지 않은 이메일 형식입니다.";

        if (errorCode.Contains("WEAK_PASSWORD"))
            return "비밀번호가 너무 약합니다. 6자 이상 입력해주세요.";

        if (errorCode.Contains("USER_NOT_FOUND") || errorCode.Contains("no user record"))
            return "존재하지 않는 계정입니다.";

        if (errorCode.Contains("WRONG_PASSWORD") || errorCode.Contains("password is invalid"))
            return "비밀번호가 올바르지 않습니다.";

        if (errorCode.Contains("TOO_MANY_ATTEMPTS") || errorCode.Contains("too-many-requests"))
            return "너무 많은 시도가 있었습니다. 잠시 후 다시 시도해주세요.";

        if (errorCode.Contains("NETWORK_ERROR") || errorCode.Contains("network"))
            return "네트워크 연결을 확인해주세요.";

        // 유효하지 않은 Credential
        if (errorCode.Contains("invalid-credential"))
            return "인증 정보가 유효하지 않습니다. 다시 시도해주세요.";

        // 팝업 차단
        if (errorCode.Contains("popup-blocked"))
            return "팝업이 차단되었습니다. 브라우저 설정을 확인해주세요.";

        return $"오류가 발생했습니다: {errorCode}";
    }
    #endregion
}
