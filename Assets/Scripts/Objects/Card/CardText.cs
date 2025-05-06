using TMPro;
using UnityEngine;

public class CardText : MonoBehaviour
{
    public TextMeshPro textMesh; // TextMeshPro 참조
    private string _textValue = "1";  // 기본값 (초기 숫자)
    private long _rawValue = 1; // 원래 숫자 값도 보관
    public long RawValue => _rawValue; // 외부에서 원본 접근 가능

    // 프로퍼티 (숫자 또는 기호를 저장)
    public string TextValue
    {
        get { return _textValue; }
        set
        {
            if (IsValidInput(value))
            {
                if (long.TryParse(value, out var parsed))
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

    void Start()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }
        //_textValue = Global.Divide;
        TextValue = GenerateRandomNumberString();

        UpdateText(); // 초기 텍스트 표시
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
        return long.TryParse(value, out _);
    }

    // 숫자를 변환 (1000 이상이면 '1.2k' 형식 적용, 최대 999k)
    private string FormatNumber(string value)
    {
        if (long.TryParse(value, out long number))
        {
            if (number >= 1_000_000)
                return "999k"; // 최대값 제한
            if (number >= 10_000)
                return (number / 1000).ToString() + "k"; // 정수 k 형식 (소수점 없음)
            if (number >= 1000)
                return (number / 1000f).ToString("0.0") + "k"; // 1.2k 형식 (소수점 1자리)
        }
        return value; // 숫자가 아니면 원본 그대로
    }

    // TODO :: TEST 함수
    public string GenerateRandomNumberString()
    {
        //int randomNumber = Random.Range(1, 1_000_003); // 최대값은 1_000_000_000 포함되게
        int randomNumber = Random.Range(1, 6); // 최대값은 1_000_000_000 포함되게
        return randomNumber.ToString();
    }
}
