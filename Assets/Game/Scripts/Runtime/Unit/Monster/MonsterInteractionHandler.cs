using UnityEngine;
using UnityEngine.EventSystems;

public class MonsterInteractionHandler
{
    private MonsterController _controller;
    private MonsterStateMachine _stateMachine;
    private float _pokeCooldownTimer;
    
    public MonsterInteractionHandler(MonsterController controller, MonsterStateMachine stateMachine)
    {
        _controller = controller;
        _stateMachine = stateMachine;
    }
    
    public void UpdateTimers(float deltaTime)
    {
        if (_pokeCooldownTimer > 0f)
            _pokeCooldownTimer -= deltaTime;
    }
    
    public void HandlePoke()
    {
        if (_pokeCooldownTimer > 0f) return;

        _pokeCooldownTimer = _controller.stats.pokeCooldownDuration;
        _controller.IncreaseHappiness(_controller.stats.pokeHappinessIncrease);
        _controller.SetShouldDropCoinAfterPoke(true);

        MonsterState pokeState = UnityEngine.Random.Range(0, 2) == 0 ?
            MonsterState.Jumping : MonsterState.Itching;

        _stateMachine?.ForceState(pokeState);
    }
    
    public void OnPointerEnter(PointerEventData e) => _controller.SetHovered(true);
    public void OnPointerExit(PointerEventData e) => _controller.SetHovered(false);
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_controller.isHovered) HandlePoke();
    }
}
