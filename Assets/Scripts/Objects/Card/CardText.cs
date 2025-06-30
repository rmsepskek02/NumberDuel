using Objects;
using TMPro;
using UnityEngine;

public class CardText : MonoBehaviour
{
    public TextMeshPro textMesh; // TextMeshPro 참조
    private string _textValue = "1";  // 기본값 (초기 숫자)
    private float _rawValue = 1; // 원래 숫자 값도 보관

    public float RawValue => _rawValue; // 외부에서 원본 접근 가능

    // 프로퍼티 (숫자 또는 기호를 저장)
    public string TextValue
    {
        get { return _textValue; }
        set
        {
            if (IsValidInput(value))
            {
                if (float.TryParse(value, out var parsed))
                {
                    _rawValue = parsed; // 원본 저장
                    _textValue = FormatNumber(value); // 표시용 저장
                }
                else
                {
                    _textValue = value; // 기호일 경우
                }
                UpdateText();
            }
        }
    }

    void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }
        UpdateText(); // 초기 텍스트 표시
    }

    /// <summary>
    /// RawValue를 외부에서 변경할 수 있도록 하는 메서드 - 나누기 연산 대응 수정
    /// 내부적으로 표시용 텍스트도 자동으로 갱신됩니다.
    /// </summary>
    public void SetRawValue(float newValue)
    {
        // 나누기 연산 결과는 정수 부분만 저장 (몫만)
        _rawValue = Mathf.Floor(newValue);
        _textValue = FormatNumber(_rawValue.ToString());
        UpdateText();
    }

    /// <summary>
    /// 정수 값으로 직접 설정 (성능 최적화)
    /// </summary>
    public void SetRawValue(int newValue)
    {
        _rawValue = newValue;
        _textValue = FormatNumber(newValue.ToString());
        UpdateText();
    }

    public void SetOperatorText(OperatorType type)
    {
        switch (type)
        {
            case OperatorType.Plus:
                _textValue = "+";
                break;
            case OperatorType.Minus:
                _textValue = "-";
                break;
            case OperatorType.Multiply:
                _textValue = "×";
                break;
            case OperatorType.Divide:
                _textValue = "÷";
                break;
        }
        _rawValue = 0; // 연산자 카드는 수치 연산에 직접 관여하지 않음
        UpdateText();
    }

    public void SetJokerText()
    {
        _textValue = "Joker";
        _rawValue = 0; // 조커는 수치 연산에 관여하지 않음 (기본 0 처리)

        if (textMesh != null)
        {
            textMesh.fontSize = 16; // 폰트 사이즈 줄이기
        }
        UpdateText();
    }

    void UpdateText()
    {
        if (textMesh != null)
        {
            textMesh.text = _textValue; // TextMeshPro에 적용
        }
    }

    // 숫자 입력 검증: 기호를 허용하고, 숫자는 변환 후 적용
    private bool IsValidInput(string value)
    {
        // 기호인지 확인
        if (System.Array.Exists(Global.AllowedSymbols, symbol => symbol == value))
        {
            return true;
        }
        // 숫자일 경우만 변환 허용 (최대값 제한은 FormatNumber에서 처리)
        return float.TryParse(value, out _);
    }

    // 숫자를 변환 (1000 이상이면 '1.2k' 형식 적용, 최대 999k) - 정수 처리 강화
    private string FormatNumber(string value)
    {
        if (float.TryParse(value, out float number))
        {
            // 정수 부분만 사용 (나누기 몫 처리)
            int intNumber = Mathf.FloorToInt(number);

            if (intNumber >= 1_000_000)
                return "999k"; // 최대값 제한
            if (intNumber >= 10_000)
                return (intNumber / 1000).ToString() + "k"; // 정수 k 형식 (소수점 없음)
            if (intNumber >= 1000)
                return (intNumber / 1000f).ToString("0.0") + "k"; // 1.2k 형식 (소수점 1자리)

            return intNumber.ToString(); // 1000 미만은 정수로 표시
        }
        return value; // 숫자가 아니면 원본 그대로
    }

    // TODO :: TEST 함수
    public string GenerateRandomNumberString()
    {
        int randomNumber = Random.Range(1, 6); // 1-5 랜덤
        return randomNumber.ToString();
    }
}