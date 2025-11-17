using System;
using UnityEngine;
using Objects;

namespace Utills
{
    /// <summary>
    /// 데미지 계산을 담당하는 정적 유틸리티 클래스
    /// 공격 데미지, 연산 데미지, 최대값 제한 등을 처리
    /// </summary>
    public static class DamageCalculator
    {
        #region Constants
        // 최대 데미지 제한
        private const int MAX_DAMAGE = 10;
        #endregion

        #region Attack Damage Calculation
        /// <summary>
        /// 공격 데미지 계산 (양쪽 필드에 카드가 있을 때)
        /// </summary>
        /// <param name="attackerValue">공격자 카드 값</param>
        /// <param name="defenderValue">방어자 카드 값</param>
        /// <returns>최종 데미지 (항상 0 이상)</returns>
        public static int CalculateAttackDamage(float attackerValue, float defenderValue)
        {
            // 공격자 값 - 방어자 값의 절대값
            float rawDamage = Mathf.Abs(attackerValue - defenderValue);
            int damage = Mathf.FloorToInt(rawDamage);

            // 최대 데미지 제한 적용
            damage = ApplyDamageLimit(damage);

            return damage;
        }

        /// <summary>
        /// 빈 필드 공격 데미지 계산 (상대 필드가 비어있을 때)
        /// </summary>
        /// <param name="attackerValue">공격자 카드 값</param>
        /// <returns>최종 데미지 (공격자 값 그대로, 최대 제한 적용)</returns>
        public static int CalculateEmptyFieldDamage(float attackerValue)
        {
            // 빈 필드 공격: 공격자 값을 그대로 데미지로
            int damage = Mathf.FloorToInt(attackerValue);

            // 최대 데미지 제한 적용
            damage = ApplyDamageLimit(damage);

            return damage;
        }
        #endregion

        #region Operation Damage Calculation
        /// <summary>
        /// 연산 결과에 따른 데미지 계산
        /// </summary>
        /// <param name="operationResult">연산 결과 값</param>
        /// <param name="operatorType">연산 타입</param>
        /// <returns>최종 데미지와 대상</returns>
        public static (int damage, CardZone.OwnerType target) CalculateOperationDamage(float operationResult, OperatorType operatorType)
        {
            int damage = 0;
            CardZone.OwnerType target;

            switch (operatorType)
            {
                case OperatorType.Plus:
                case OperatorType.Multiply:
                    // 더하기/곱하기: 결과가 양수일 경우에만 데미지
                    damage = Mathf.FloorToInt(Mathf.Max(0, operationResult));
                    target = CardZone.OwnerType.Opponent;
                    break;

                case OperatorType.Minus:
                case OperatorType.Divide:
                    // 빼기/나누기: 결과의 절댓값 경우에만 데미지
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

            return limitedDamage;
        }

        /// <summary>
        /// 전투 결과에 따른 승자 판정 (카드 값 비교)
        /// </summary>
        /// <param name="attackerValue">공격자 값</param>
        /// <param name="defenderValue">방어자 값</param>
        /// <returns>승리자 값 (1: 공격자, -1: 방어자, 0: 무승부)</returns>
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

            // 극단 한계값 넘어서면 큰 값 체크
            if (result < -1000f || result > 1000f)
                return false;

            return true;
        }

        /// <summary>
        /// 현재 최대 데미지 제한 값 반환
        /// </summary>
        public static int GetMaxDamageLimit() => MAX_DAMAGE;
        #endregion
    }
}
