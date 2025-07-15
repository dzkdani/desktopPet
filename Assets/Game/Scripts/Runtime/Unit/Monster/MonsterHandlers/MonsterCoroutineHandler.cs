using UnityEngine;
using System.Collections;
using System;

public class MonsterCoroutineHandler
{
    private MonsterController _controller;
    private Coroutine _hungerCoroutine;
    private Coroutine _happinessCoroutine;
    private Coroutine _poopCoroutine;
    private Coroutine _goldCoinCoroutine;
    private Coroutine _silverCoinCoroutine;

    public MonsterCoroutineHandler(MonsterController controller)
    {
        _controller = controller;
    }

    public void StartAllCoroutines()
    {
        float goldCoinInterval = (float)TimeSpan.FromHours(_controller.MonsterData.goldCoinDropRateStage1).TotalSeconds;
        float silverCoinInterval = (float)TimeSpan.FromHours(_controller.MonsterData.platCoinDropRateStage1).TotalSeconds;
        float poopInterval = (float)TimeSpan.FromMinutes(_controller.MonsterData.poopRate).TotalSeconds;


        _hungerCoroutine = _controller.StartCoroutine(HungerRoutine(1f));
        _happinessCoroutine = _controller.StartCoroutine(HappinessRoutine(1f));
        _poopCoroutine = _controller.StartCoroutine(PoopRoutine(poopInterval));
        _goldCoinCoroutine = _controller.StartCoroutine(CoinCoroutine(goldCoinInterval, CoinType.Platinum));
        _silverCoinCoroutine = _controller.StartCoroutine(CoinCoroutine(silverCoinInterval, CoinType.Gold));
    }

    public void StopAllCoroutines()
    {
        if (_hungerCoroutine != null) _controller.StopCoroutine(_hungerCoroutine);
        if (_happinessCoroutine != null) _controller.StopCoroutine(_happinessCoroutine);
        if (_poopCoroutine != null) _controller.StopCoroutine(_poopCoroutine);
        if (_goldCoinCoroutine != null) _controller.StopCoroutine(_goldCoinCoroutine);
        if (_silverCoinCoroutine != null) _controller.StopCoroutine(_silverCoinCoroutine);
    }

    private IEnumerator HungerRoutine(float interval)
    {
        while (true)
        {
            if (_controller.MonsterData != null)
            {
                float newHunger = Mathf.Clamp(_controller.StatsHandler.CurrentHunger - _controller.MonsterData.hungerDepleteRate, 0f, 100f);
                _controller.StatsHandler.SetHunger(newHunger);
                _controller.StatsHandler.UpdateHealth(interval);
                _controller.StatsHandler.UpdateSickStatus(interval);
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator HappinessRoutine(float interval)
    {
        while (true)
        {
            if (!_controller.StatsHandler.IsSick && _controller.MonsterData != null)
            {
                // Update happiness based on area
                _controller.StatsHandler.UpdateHappinessBasedOnArea(_controller.MonsterData, ServiceLocator.Get<MonsterManager>());

                // Reduce happiness if hungry
                if (_controller.StatsHandler.CurrentHunger < _controller.MonsterData.hungerHappinessThreshold)
                {
                    float newHappiness = Mathf.Clamp(_controller.StatsHandler.CurrentHappiness - _controller.MonsterData.hungerHappinessDrainRate, 0f, 100f);
                    _controller.StatsHandler.SetHappiness(newHappiness);
                }
            }
            else if (_controller.StatsHandler.IsSick)
            {
                // Reduce happiness when sick
                float newHappiness = Mathf.Clamp(_controller.StatsHandler.CurrentHappiness - 2f, 0f, 100f);
                _controller.StatsHandler.SetHappiness(newHappiness);
            }
            _controller.StatsHandler.UpdateHealth(interval);
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator PoopRoutine(float interval)
    {
        // interval = 10f;

        yield return new WaitForSeconds(interval);
        while (true)
        {
            var monsterData = _controller.MonsterData;
            if (monsterData != null)
            {
                if (monsterData.monType == MonsterType.Common ||
                    monsterData.monType == MonsterType.Uncommon)
                    _controller.DropPoop(PoopType.Normal);
                else if (monsterData.monType == MonsterType.Rare ||
                         monsterData.monType == MonsterType.Legend ||
                         monsterData.monType == MonsterType.Mythic)
                    _controller.DropPoop(PoopType.Sparkle);
                else
                    _controller.DropPoop(); // Default to normal poop if type is unknown
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator CoinCoroutine(float delay, CoinType type)
    {
        // delay = 20f;

        yield return new WaitForSeconds(delay);
        while (true)
        {
            _controller.DropCoin(type);
            yield return new WaitForSeconds(delay);
        }
    }
}
