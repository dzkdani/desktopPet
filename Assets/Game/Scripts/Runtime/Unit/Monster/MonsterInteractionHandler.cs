using UnityEngine;
using UnityEngine.EventSystems;

public class MonsterInteractionHandler
{
    private MonsterController _controller;
    private MonsterStateMachine _stateMachine;
    private float _pokeCooldownTimer;
    private bool _pendingSilverCoinDrop;
    
    public MonsterInteractionHandler(MonsterController controller, MonsterStateMachine stateMachine)
    {
        _controller = controller;
        _stateMachine = stateMachine;
    }
    
    public void UpdateTimers(float deltaTime)
    {
        if (_pokeCooldownTimer > 0f)
            _pokeCooldownTimer -= deltaTime;
            
        // Check if we need to drop silver coin after poke animation
        CheckForPendingSilverCoinDrop();
    }
    
    public void HandlePoke()
    {
        if (_pokeCooldownTimer > 0f) return;

        _pokeCooldownTimer = _controller.stats.pokeCooldownDuration;
        _controller.IncreaseHappiness(_controller.stats.pokeHappinessIncrease);
        
        // Mark that we should drop a silver coin after animation
        _pendingSilverCoinDrop = true;

        MonsterState pokeState = UnityEngine.Random.Range(0, 2) == 0 ?
            MonsterState.Jumping : MonsterState.Itching;

        _stateMachine?.ForceState(pokeState);
    }
    
    private void CheckForPendingSilverCoinDrop()
    {
        if (_pendingSilverCoinDrop && 
            (_stateMachine.CurrentState != MonsterState.Jumping && 
             _stateMachine.CurrentState != MonsterState.Itching))
        {
            // Animation finished, drop the silver coin
            _controller.DropCoinAfterPoke();
            _pendingSilverCoinDrop = false;
        }
    }
    
    public void OnPointerEnter(PointerEventData e) => _controller.SetHovered(true);
    public void OnPointerExit(PointerEventData e) => _controller.SetHovered(false);
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_controller.isHovered) HandlePoke();
    }
}
