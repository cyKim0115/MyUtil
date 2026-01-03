using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProbabilityDictionary : Dictionary<object, float>
{
    private System.Random random = new System.Random();

    // 모든 값의 합을 캐싱하여 성능 최적화
    private float totalWeight = 0f;
    private bool isDirty = true;

    // 새로운 요소 추가/삭제/수정 시 totalWeight 갱신 플래그 설정
    public new void Add(object key, float value)
    {
        if (value < 0f)
        {
            Debug.LogWarning("ProbabilityDictionary: Weight cannot be negative.");
            return;
        }
        base.Add(key, value);
        isDirty = true;
    }

    public new void Remove(object key)
    {
        base.Remove(key);
        isDirty = true;
    }

    public new void Clear()
    {
        base.Clear();
        isDirty = true;
    }

    public new float this[object key]
    {
        get => base[key];
        set
        {
            if (value < 0f)
            {
                Debug.LogWarning("ProbabilityDictionary: Weight cannot be negative.");
                return;
            }
            base[key] = value;
            isDirty = true;
        }
    }

    // 모든 값의 합을 계산 (필요할 때만 갱신)
    private void UpdateTotalWeight()
    {
        if (!isDirty) return;
        totalWeight = Values.Sum();
        isDirty = false;
    }

    // 확률 기반으로 키를 뽑는 함수
    public object GetRandomKey()
    {
        if (Count == 0)
        {
            Debug.LogWarning("ProbabilityDictionary: Dictionary is empty.");
            return null;
        }

        UpdateTotalWeight();
        if (totalWeight <= 0f)
        {
            Debug.LogWarning("ProbabilityDictionary: Total weight is zero or negative.");
            return null;
        }

        float randomValue = (float)random.NextDouble() * totalWeight;
        float currentSum = 0f;

        foreach (var pair in this)
        {
            currentSum += pair.Value;
            if (randomValue <= currentSum)
            {
                return pair.Key;
            }
        }

        // 부동소수점 오차로 인해 마지막 키 반환
        return Keys.Last();
    }

    // 특정 키의 확률을 백분율로 반환
    public float GetProbabilityPercentage(object key)
    {
        if (!ContainsKey(key))
        {
            Debug.LogWarning($"ProbabilityDictionary: Key '{key}' not found.");
            return 0f;
        }

        UpdateTotalWeight();
        if (totalWeight <= 0f)
        {
            Debug.LogWarning("ProbabilityDictionary: Total weight is zero or negative.");
            return 0f;
        }

        return (this[key] / totalWeight) * 100f;
    }
}