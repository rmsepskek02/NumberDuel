namespace Objects.Auth
{
    /// <summary>
    /// 소셜 로그인 결과 데이터
    /// Google, Kakao 등 모든 소셜 로그인에서 공통으로 사용
    /// </summary>
    public class SocialAuthResult
    {
        /// <summary>
        /// 로그인 성공 여부
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 에러 메시지 (실패 시)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 소셜 플랫폼 사용자 ID (Google User ID 등)
        /// </summary>
        public string ProviderUserId { get; set; }

        /// <summary>
        /// 이메일
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 표시 이름
        /// </summary>
        public string DisplayName { get; set; }

        // TODO: 프로필 사진 기능 - 추후 구현 예정
        // /// <summary>
        // /// 프로필 사진 URL
        // /// </summary>
        // public string PhotoUrl { get; set; }

        /// <summary>
        /// Firebase Credential
        /// </summary>
        public Firebase.Auth.Credential Credential { get; set; }

        /// <summary>
        /// idToken (Google OAuth)
        /// </summary>
        public string IdToken { get; set; }

        /// <summary>
        /// accessToken (Google OAuth)
        /// </summary>
        public string AccessToken { get; set; }

        public SocialAuthResult()
        {
            Success = false;
            Message = string.Empty;
            ProviderUserId = string.Empty;
            Email = string.Empty;
            DisplayName = string.Empty;
            // PhotoUrl = string.Empty; // TODO: 프로필 사진 기능 - 추후 구현 예정
            Credential = null;
            IdToken = string.Empty;
            AccessToken = string.Empty;
        }
    }
}
