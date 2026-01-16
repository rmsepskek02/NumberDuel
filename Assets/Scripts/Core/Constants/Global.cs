using UnityEngine;

/// <summary>
/// 전역 상수 값들 및 함수를 모아둔 클래스
/// </summary>
public static class Global
{
    #region Color
    // 주요 색상 (RGB 값) - HEX 코드 자동 변환
    public static readonly Color Red = HexToColor("#BE373B");
    public static readonly Color Green = HexToColor("#18AD83");
    public static readonly Color Purple = HexToColor("#6855B2");
    public static readonly Color Yellow = HexToColor("#DB8E3D");
    public static readonly Color GlowGreen = HexToColor("#05FF00"); // 공격 Glow
    public static readonly Color GlowRed = HexToColor("#FF000A");   // 피격 Glow

    // UI 상태 색상
    public static readonly Color Processing = HexToColor("#808080"); // 처리 중
    public static readonly Color White = HexToColor("#FFFFFF");      // 기본 (밝은색)
    public static readonly Color Black = HexToColor("#000000");      // 기본 (어두운색)

    // 주요 색상 (HEX 값)
    public static readonly string RedHex = "#BE373B";
    public static readonly string GreenHex = "#18AD83";
    public static readonly string PurpleHex = "#6855B2";
    public static readonly string YellowHex = "#DB8E3D";
    public static readonly string GlowGreenHex = "#05FF00";
    public static readonly string GlowRedHex = "#FF000A";

    // 색상명 문자열로 색상얻기 함수
    public static Color GetColorByName(string colorName)
    {
        string lower = colorName.ToLower();

        if (lower.Contains("red")) return Red;
        if (lower.Contains("green")) return Green;
        if (lower.Contains("yellow")) return Yellow;
        if (lower.Contains("purple")) return Purple;

        return Color.white;
    }

    // HEX 값 RGB 변환 함수
    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        return Color.white; // 기본값 (변환 실패 시 흰색)
    }

    #endregion

    #region Symbols
    // 수식에서 사용할 수 있는 연산 기호
    public static readonly string Plus = "+";
    public static readonly string Minus = "-";
    public static readonly string Multiply = "×";
    public static readonly string Divide = "÷";
    public static readonly string Equal = "=";

    public static readonly string[] AllowedSymbols = { Plus, Minus, Multiply, Divide, Equal };
    #endregion

    #region Path
    public static readonly string Card = "Card";
    public static readonly string Joker = "Joker";
    #endregion

    #region Resources - Material Names
    public static readonly string MatRedCardEmpty = "Red_Card_Empty";
    public static readonly string MatYellowCardEmpty = "Yellow_Card_Empty";
    public static readonly string MatPurpleCardEmpty = "Purple_Card_Empty";
    public static readonly string MatGreenCardEmpty = "Green_Card_Empty";
    public static readonly string MatBackCard = "Back_Card";
    #endregion

    #region Resources - Image Names
    public static readonly string ImgColorBack = "color_back";
    public static readonly string ImgColorGreenEmpty = "color_green_empty";
    public static readonly string ImgColorPurpleEmpty = "color_purple_empty";
    public static readonly string ImgColorYellowEmpty = "color_yellow_empty";
    public static readonly string ImgColorRedEmpty = "color_red_empty";
    #endregion

    #region Resources - Sprite Names
    public static readonly string SpriteColorBlack = "color_back 1_0";
    #endregion

    #region Validation
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
        if (!System.Text.RegularExpressions.Regex.IsMatch(email, pattern))
            return (false, "올바른 이메일 형식이 아닙니다.");

        return (true, string.Empty);
    }

    /// <summary>
    /// 비밀번호 검증
    /// </summary>
    /// <param name="password">검증할 비밀번호</param>
    /// <returns>검증 결과와 에러 메시지</returns>
    public static (bool isValid, string errorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "비밀번호를 입력해주세요.");

        if (password.Length < 6)
            return (false, "비밀번호는 최소 6자 이상이어야 합니다.");

        return (true, string.Empty);
    }

    /// <summary>
    /// 닉네임 검증
    /// </summary>
    /// <param name="nickname">검증할 닉네임</param>
    /// <returns>검증 결과와 에러 메시지</returns>
    public static (bool isValid, string errorMessage) ValidateNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return (false, "닉네임을 입력해주세요.");

        if (nickname.Length < 2)
            return (false, "닉네임은 최소 2자 이상이어야 합니다.");

        if (nickname.Length > 12)
            return (false, "닉네임은 최대 12자까지 가능합니다.");

        return (true, string.Empty);
    }

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

    // 사용 Sample
    // Color selectedColor = Global.GetColorByName("red"); // 색상명 변환
    // textMeshPro.color = Global.Purple; // 보라색 적용
    // cardRenderer.material.color = Global.Green; // 초록색 적용
    // string plusSymbol = Global.Plus; // "+" 기호 사용
    // string redMatName = Global.RedCardEmptyMat; // 머티리얼 이름 사용
    // string redImgName = Global.ColorRedEmptyImg; // 이미지 이름 사용
    // var (isValid, errorMessage) = Global.ValidateEmail("user@example.com"); // 이메일 검증
}
