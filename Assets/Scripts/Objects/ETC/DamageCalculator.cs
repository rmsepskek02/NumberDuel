using UnityEngine;
using Objects;

namespace Utills
{
    /// <summary>
    /// 데미지 계산을 담당하는 정적 유틸리티 클래스
    /// 공격 데미지, 연산 데미지, 데미지 제한 등을 처리
    /// </summary>
    public static class DamageCalculator
    {
        // 최대 데미지 제한
        private const int MAX_DAMAGE = 10;

        #region Attack Damage Calculation
        /// <summary>
        /// 공격 데미지 계산 (상대 필드에 카드가 있는 경우)
        /// </summary>
        /// <param name="attackerValue">공격자 카드 값</param>
        /// <param name="defenderValue">방어자 카드 값</param>
        /// <returns>계산된 데미지 (항상 0 이상)</returns>
        public static int CalculateAttackDamage(float attackerValue, float defenderValue)
        {
            // 공격자 값 - 방어자 값의 절댓값
            float rawDamage = Mathf.Abs(attackerValue - defenderValue);
            int damage = Mathf.FloorToInt(rawDamage);

            // 최대 데미지 제한 적용
            damage = ApplyDamageLimit(damage);

            Debug.Log($"[DamageCalculator] 공격 데미지 계산: |{attackerValue} - {defenderValue}| = {rawDamage} → {damage} (제한 적용)");

            return damage;
        }

        /// <summary>
        /// 빈 필드 공격 데미지 계산 (상대 필드가 비어있는 경우)
        /// </summary>
        /// <param name="attackerValue">공격자 카드 값</param>
        /// <returns>계산된 데미지 (공격자 값 그대로, 최대 제한 적용)</returns>
        public static int CalculateEmptyFieldDamage(float attackerValue)
        {
            // 빈 필드 공격: 공격자 값이 그대로 데미지
            int damage = Mathf.FloorToInt(attackerValue);

            // 최대 데미지 제한 적용
            damage = ApplyDamageLimit(damage);

            Debug.Log($"[DamageCalculator] 빈 필드 공격 데미지: {attackerValue} → {damage} (제한 적용)");

            return damage;
        }
        #endregion

        #region Operation Damage Calculation
        /// <summary>
        /// 연산 결과에 따른 데미지 계산
        /// </summary>
        /// <param name="operationResult">연산 결과 값</param>
        /// <param name="operatorType">사용된 연산자</param>
        /// <returns>계산된 데미지와 대상</returns>
        public static (int damage, CardZone.OwnerType target) CalculateOperationDamage(float operationResult, OperatorType operatorType)
        {
            int damage = 0;
            CardZone.OwnerType target;

            switch (operatorType)
            {
                case OperatorType.Plus:
                case OperatorType.Multiply:
                    // 더하기/곱하기: 결과가 양수면 상대에게 데미지
                    damage = Mathf.FloorToInt(Mathf.Max(0, operationResult));
                    target = CardZone.OwnerType.Opponent;
                    break;

                case OperatorType.Minus:
                case OperatorType.Divide:
                    // 빼기/나누기: 결과의 절댓값을 상대에게 데미지
                    damage = Mathf.FloorToInt(Mathf.Abs(operationResult));
                    target = CardZone.OwnerType.Opponent;
                    break;

                default:
                    // 알 수 없는 연산자는 데미지 없음
                    damage = 0;
                    target = CardZone.OwnerType.Opponent;
                    break;
            }

            // 최대 데미지 제한 적용
            damage = ApplyDamageLimit(damage);

            Debug.Log($"[DamageCalculator] 연산 데미지 계산: {operatorType} 결과 {operationResult} → {target}에게 {damage} 데미지");

            return (damage, target);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 최대 데미지 제한 적용
        /// </summary>
        /// <param name="damage">원본 데미지</param>
        /// <returns>제한이 적용된 데미지 (최대 10)</returns>
        public static int ApplyDamageLimit(int damage)
        {
            int limitedDamage = Mathf.Min(damage, MAX_DAMAGE);

            if (damage > MAX_DAMAGE)
            {
                Debug.Log($"[DamageCalculator] 데미지 제한 적용: {damage} → {limitedDamage}");
            }

            return limitedDamage;
        }

        /// <summary>
        /// 공격 결과에 따른 승자 결정 (카드 간 전투용)
        /// </summary>
        /// <param name="attackerValue">공격자 값</param>
        /// <param name="defenderValue">방어자 값</param>
        /// <returns>승리한 쪽 (1: 공격자, -1: 방어자, 0: 무승부)</returns>
        public static int DetermineBattleWinner(float attackerValue, float defenderValue)
        {
            float difference = attackerValue - defenderValue;

            if (difference > 0) return 1;      // 공격자 승리
            if (difference < 0) return -1;     // 방어자 승리
            return 0;                          // 무승부
        }

        /// <summary>
        /// 연산 결과가 유효한 데미지인지 확인
        /// </summary>
        /// <param name="result">연산 결과</param>
        /// <returns>유효한 데미지면 true</returns>
        public static bool IsValidDamageResult(float result)
        {
            // NaN, Infinity 체크
            if (float.IsNaN(result) || float.IsInfinity(result))
                return false;

            // 음의 무한대나 극단적으로 큰 값 체크
            if (result < -1000f || result > 1000f)
                return false;

            return true;
        }

        /// <summary>
        /// 현재 최대 데미지 제한 값 반환
        /// </summary>
        public static int GetMaxDamageLimit() => MAX_DAMAGE;
        #endregion

        #region Debug Methods
        /// <summary>
        /// 데미지 계산 과정을 상세히 출력 (디버그용)
        /// </summary>
        /// <param name="description">계산 설명</param>
        /// <param name="value1">첫 번째 값</param>
        /// <param name="value2">두 번째 값</param>
        /// <param name="result">최종 결과</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DebugLogCalculation(string description, float value1, float value2, int result)
        {
            Debug.Log($"[DamageCalculator] {description}: {value1} vs {value2} → {result} 데미지");
        }
        #endregion
    }
}