using System.Text.RegularExpressions;

namespace Utills
{
    /// <summary>
    /// 유효성 검사를 담당하는 헬퍼 클래스
    /// 이메일, 비밀번호, 닉네임 등의 검증 로직을 통합 관리
    /// </summary>
    public static class ValidationHelper
    {
        #region Constants

        /// <summary>
        /// 닉네임 최대 픽셀 길이 (한글 2px, 영문/숫자 1px)
        /// </summary>
        public const int MAX_NICKNAME_PIXEL_LENGTH = 24;

        /// <summary>
        /// 비밀번호 최소 길이
        /// </summary>
        public const int MIN_PASSWORD_LENGTH = 6;

        #endregion

        #region Email Validation

        /// <summary>
        /// 이메일 검증
        /// </summary>
        /// <param name="email">검증할 이메일</param>
        /// <returns>검증 결과와 에러 메시지</returns>
        public static (bool isValid, string errorMessage) ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "이메일을 입력해주세요.");

            // 정규표현식: 기본적인 이메일 형식 검증
            // - 로컬 파트: 영문/숫자/특수문자(._%+-)
            // - @ 필수
            // - 도메인: 영문/숫자/하이픈/점
            // - TLD: 최소 2글자 이상
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!Regex.IsMatch(email, pattern))
                return (false, "올바른 이메일 형식이 아닙니다.");

            return (true, string.Empty);
        }

        #endregion

        #region Password Validation

        /// <summary>
        /// 비밀번호 검증
        /// </summary>
        /// <param name="password">검증할 비밀번호</param>
        /// <returns>검증 결과와 에러 메시지</returns>
        public static (bool isValid, string errorMessage) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "비밀번호를 입력해주세요.");

            if (password.Length < MIN_PASSWORD_LENGTH)
                return (false, $"비밀번호는 최소 {MIN_PASSWORD_LENGTH}자 이상이어야 합니다.");

            return (true, string.Empty);
        }

        #endregion

        #region Nickname Validation

        /// <summary>
        /// 닉네임 검증 (픽셀 기반)
        /// 한글/영문만 허용, 한글 2px, 영문 1px 기준 최대 24px
        /// </summary>
        /// <param name="nickname">검증할 닉네임</param>
        /// <returns>검증 결과와 에러 메시지</returns>
        public static (bool isValid, string errorMessage) ValidateNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return (false, "닉네임을 입력해주세요.");

            // 허용 문자 검증 (한글, 영문만)
            foreach (char c in nickname)
            {
                if (!IsValidNicknameCharacter(c))
                    return (false, "한글, 영문만 사용 가능합니다.");
            }

            // 최소 1자 이상
            if (nickname.Length < 1)
                return (false, "최소 1자 이상 입력해주세요.");

            // 픽셀 길이 검증
            int pixelLength = CalculatePixelLength(nickname);
            if (pixelLength > MAX_NICKNAME_PIXEL_LENGTH)
                return (false, $"닉네임이 너무 깁니다. ({pixelLength}/{MAX_NICKNAME_PIXEL_LENGTH})");

            return (true, string.Empty);
        }

        /// <summary>
        /// 닉네임 픽셀 길이 계산
        /// 한글: 2px, 영문: 1px
        /// </summary>
        /// <param name="input">계산할 문자열</param>
        /// <returns>픽셀 길이 (-1: 허용되지 않는 문자 포함)</returns>
        public static int CalculatePixelLength(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            int totalPixels = 0;

            foreach (char c in input)
            {
                if (!IsValidNicknameCharacter(c))
                    return -1; // 허용되지 않는 문자

                totalPixels += IsKoreanCharacter(c) ? 2 : 1;
            }

            return totalPixels;
        }

        /// <summary>
        /// 닉네임에 허용되는 문자인지 확인 (한글, 영문만)
        /// </summary>
        /// <param name="c">검사할 문자</param>
        /// <returns>허용 여부</returns>
        public static bool IsValidNicknameCharacter(char c)
        {
            return IsKoreanCharacter(c) || IsEnglishCharacter(c);
        }

        /// <summary>
        /// 한글 문자 여부 확인 (완성형 + 자모음)
        /// </summary>
        /// <param name="c">검사할 문자</param>
        /// <returns>한글 여부</returns>
        public static bool IsKoreanCharacter(char c)
        {
            // 완성형 한글: 가(0xAC00) ~ 힣(0xD7A3)
            // 자음: ㄱ(0x3131) ~ ㅎ(0x314E)
            // 모음: ㅏ(0x314F) ~ ㅣ(0x3163)
            return (c >= '가' && c <= '힣') ||
                   (c >= 'ㄱ' && c <= 'ㅎ') ||
                   (c >= 'ㅏ' && c <= 'ㅣ');
        }

        /// <summary>
        /// 영문 문자 여부 확인
        /// </summary>
        /// <param name="c">검사할 문자</param>
        /// <returns>영문 여부</returns>
        public static bool IsEnglishCharacter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        #endregion

        #region General Validation

        /// <summary>
        /// 문자열이 비어있는지 검증
        /// </summary>
        /// <param name="value">검증할 문자열</param>
        /// <param name="fieldName">필드 이름 (에러 메시지용)</param>
        /// <returns>검증 결과와 에러 메시지</returns>
        public static (bool isValid, string errorMessage) ValidateNotEmpty(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (false, $"{fieldName}을(를) 입력해주세요.");

            return (true, string.Empty);
        }

        #endregion
    }
}
