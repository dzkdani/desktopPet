using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class NPCPetCaretakerHandler
{
    private MonsterManager _monsterManager;
    private MonsterController _monsterController;
    private List<MonsterController> _petMonsterList = new List<MonsterController>();
    private List<CoinController> _coinList = new List<CoinController>();
    private List<PoopController> _poopList = new List<PoopController>();
    private Vector2 NPCPosition;
    private CoinController TargetCoin;
    private PoopController TargetPoop;
    private MonsterController TargetPet;
    private bool PoopCollectorEnabled = true;
    private bool OnCollectingPoop = false;
    private bool CoinCollectorEnabled = true;
    private bool OnCollectingCoin = false;
    private bool PetInteractionEnabled = true;
    private bool OnPetInteraction = false;
    private bool PetFeederEnabled = true;
    private bool OnFeedingPet = false;
    private float minDistanceFromTarget = 100f;

    private enum ActionType { CoinCollection, PoopCollection, PetInteraction, PetFeeding , Idling}
    private ActionType _currentAction = ActionType.Idling;
    private bool _isPerformingAction = false;

    public bool OnAction => OnDoingAction();

    public NPCPetCaretakerHandler(MonsterController monsterController)
    {
        _monsterController = monsterController;
        _monsterManager = ServiceLocator.Get<MonsterManager>();

        if (_monsterManager == null || _monsterController == null)
        {
            Debug.LogError("NPCPetCaretakerHandler: MonsterManager or MonsterController is null.");
            return;
        }
    }

    public void Initialize()
    {
        InitList(_monsterManager);
        InitMode();
        InitState();
        _monsterController.StartCoroutine(ManageActions());
    }

    private IEnumerator ManageActions()
    {
        while (true)
        {
            if (!_isPerformingAction)
            {
                yield return StartNextAction();
            }
            yield return new WaitForSeconds(3f); // Check every 3 seconds
        }
    }

    // Update the StartNextAction method to use test sequence
    private IEnumerator StartNextAction()
    {
        _isPerformingAction = true;

        switch (_currentAction)
        {
            case ActionType.CoinCollection when CoinCollectorEnabled:
                yield return DoCoinCollection();
                Debug.Log("NPC is collecting coins.");
                break;
            case ActionType.PoopCollection when PoopCollectorEnabled:
                yield return DoPoopCollection();
                Debug.Log("NPC is collecting poop.");
                break;
            case ActionType.PetInteraction when PetInteractionEnabled:
                yield return DoPetInteraction();
                Debug.Log("NPC is interacting with a pet.");
                break;
            case ActionType.PetFeeding when PetFeederEnabled:
                yield return DoPetFeeding();
                Debug.Log("NPC is feeding a pet.");
                break;
            case ActionType.Idling:
                Debug.Log("NPC is idling.");
                break;
            default: 
                Debug.LogWarning($"NPCPetCaretakerHandler: Action {_currentAction} is not enabled or not implemented.");
                _isPerformingAction = false;
                yield break; 
        }
        _isPerformingAction = false;
        _currentAction = GetAction();
    }

    private ActionType GetAction()
    {
        // Priority 1: Check if any monster has hunger under 40%
        if (PetFeederEnabled)
        {
            foreach (var pet in _petMonsterList)
            {
                if (pet != null && pet.IsTargetable)
                {
                    // Assuming pets have a hunger property - adjust property name as needed
                    if (pet.StatsHandler.CurrentHunger < 100f * 0.4f)
                    {
                        return ActionType.PetFeeding;
                    }
                }
            }
        }

        // Priority 2: Check if any monster has happiness under 50%
        if (PetInteractionEnabled)
        {
            foreach (var pet in _petMonsterList)
            {
                if (pet != null && pet.IsTargetable)
                {
                    // Assuming pets have a happiness property - adjust property name as needed
                    if (pet.StatsHandler.CurrentHappiness < 100f * 0.5f)
                    {
                        return ActionType.PetInteraction;
                    }
                }
            }
        }

        // Priority 3: Collect coins if available
        if (CoinCollectorEnabled && _coinList.Any(coin => coin != null && coin.IsTargetable))
        {
            return ActionType.CoinCollection;
        }

        // Priority 4: Collect poop if available
        if (PoopCollectorEnabled && _poopList.Any(poop => poop != null && poop.IsTargetable))
        {
            return ActionType.PoopCollection;
        }

        // Default: Idle if nothing to do
        return ActionType.Idling;
    }

    public void InitMode(bool poopCollectorEnabled = true, bool coinCollectorEnabled = true, bool petInteractionEnabled = true, bool petFeederEnabled = true)
    {
        PoopCollectorEnabled = poopCollectorEnabled;
        CoinCollectorEnabled = coinCollectorEnabled;
        PetInteractionEnabled = petInteractionEnabled;
        PetFeederEnabled = petFeederEnabled;
    }

    public void InitState(bool onCollectingCoin = false, bool onCollectingPoop = false, bool onPetInteraction = false, bool onFeedingPet = false)
    {
        OnCollectingCoin = onCollectingCoin;
        OnCollectingPoop = onCollectingPoop;
        OnPetInteraction = onPetInteraction;
        OnFeedingPet = onFeedingPet;
    }

    public void InitList(MonsterManager manager)
    {
        _petMonsterList.Clear();
        _coinList.Clear();
        _poopList.Clear();
        _petMonsterList = manager.activeMonsters;
        _coinList = manager.activeCoins;
        _poopList = manager.activePoops;

        if (_petMonsterList.Count <= 0 || _coinList.Count <= 0 || _poopList.Count <= 0)
        {
            Debug.LogWarning($"NPCPetCaretakerHandler: No pets {_petMonsterList.Count}, coins {_coinList.Count}, or poops {_poopList.Count} found.");
            return;
        }
    }

    private IEnumerator MoveTo(RectTransform target)
    {
        // Set the target position in MonsterController - this will use the existing movement system
        _monsterController.SetTargetPosition(target.anchoredPosition);
        
        // Wait until the monster reaches the target position
        while (OnDoingAction())
        {
            // Check if coin still exists
            if (target == null || !target.gameObject.activeInHierarchy) yield break;

            // Check if we're close enough to collect
            NPCPosition = GetAnchoredPos(_monsterController.transform);
            float distance = Vector2.Distance(NPCPosition, target.anchoredPosition);

            if (distance <= minDistanceFromTarget)
            {
                OnNearTarget(target.gameObject);
                yield break;
            }
            yield return null;
        }
        yield return null; 
    }

    private void OnNearTarget(GameObject target)
    {
        if (target.TryGetComponent<CoinController>(out var coin))
        {
            CollectCoin(coin);
            return;
        }
        if (target.TryGetComponent<PoopController>(out var poop))
        {
            CollectPoop(poop);
            return;
        }
        if (target.TryGetComponent<MonsterController>(out var pet))
        {
            _monsterController.StateMachine.ChangeState(MonsterState.Jumping);
            if (_currentAction == ActionType.PetInteraction) pet.InteractionHandler.HandlePoke();
            if (_currentAction == ActionType.PetFeeding)
            {
                var itemInventoryUI = ServiceLocator.Get<ItemInventoryUI>();
                var itemID = SaveSystem.PlayerConfig.ownedItems.FirstOrDefault(i => i.type == ItemType.Food)?.itemID; // Get the first food item ID
                var itemData = itemInventoryUI.ItemDatabase.GetItem(itemID);
                SaveSystem.TryBuyItem(itemData);
                if (SaveSystem.PlayerConfig.ownedItems.FirstOrDefault(i => i.itemID == itemID).amount >= 1)
                {
                    SaveSystem.PlayerConfig.ownedItems.FirstOrDefault(i => i.itemID == itemID).amount -= 1; // Decrease food amount
                    _monsterManager.SpawnItem(itemData, pet.GetComponent<RectTransform>().anchoredPosition); // Spawn food 
                    Debug.Log("Feeding pet...");
                }
                else
                {
                    Debug.LogWarning("Not enough food to feed the pet.");
                }
            }
            return;
        }
    }

    private void CollectCoin(CoinController coin)
    {
        coin.OnCollected();
        // Remove from active coins list
        if (_coinList.Contains(coin))
        {
            _coinList.Remove(coin);
        }
    }

    private void CollectPoop(PoopController poop)
    {
        poop.OnCollected();
        // Remove from active poops list
        if (_poopList.Contains(poop))
        {
            _poopList.Remove(poop);
        }
    }

    public IEnumerator DoCoinCollection()
    {   
        while (CoinCollectorEnabled) // Add continuous loop
        {
            if (OnDoingAction())
            {
                yield return new WaitUntil(() => !OnDoingAction()); // Wait before checking again
                continue;
            }
            OnCollectingCoin = true;
            TargetCoin = FindNearestTarget(_coinList);
            if (TargetCoin != null) yield return _monsterController.StartCoroutine(MoveTo(TargetCoin.transform as RectTransform));
            OnCollectingCoin = false;
            TargetCoin = null; // Reset target after collection
            // Wait before looking for next coin
            yield break;
        }
    }

    public IEnumerator DoPoopCollection()
    {
        while (PoopCollectorEnabled) // Add continuous loop
        {
            if (OnDoingAction())
            {
                yield return new WaitUntil(() => !OnDoingAction()); // Wait before checking again
                continue;
            }
            OnCollectingPoop = true;
            TargetPoop = FindNearestTarget(_poopList);
            if (TargetPoop != null) yield return _monsterController.StartCoroutine(MoveTo(TargetPoop.transform as RectTransform));
            OnCollectingPoop = false;
            TargetPoop = null; // Reset target after collection
            // Wait before looking for next poop
            yield break;
        }
    }

    public IEnumerator DoPetInteraction()
    {
        while (PetInteractionEnabled) // Add continuous loop
        {
            if (OnDoingAction())
            {
                yield return new WaitUntil(() => !OnDoingAction()); // Wait before checking again
                continue;
            }
            OnPetInteraction = true;
            TargetPet = FindNearestTarget(_petMonsterList);
            if (TargetPet != null) yield return _monsterController.StartCoroutine(MoveTo(TargetPet.transform as RectTransform));
            OnPetInteraction = false;
            TargetPet = null; // Reset target after interaction
            // Wait before looking for next pet
            yield break;
        }
    }

    public IEnumerator DoPetFeeding()
    {
        while (PetFeederEnabled) // Add continuous loop
        {
            if (OnDoingAction())
            {
                yield return new WaitUntil(() => !OnDoingAction()); // Wait before checking again
                continue;
            }
            OnFeedingPet = true;
            TargetPet = FindNearestTarget(_petMonsterList);
            if (TargetPet != null) yield return _monsterController.StartCoroutine(MoveTo(TargetPet.transform as RectTransform));
            OnFeedingPet = false;
            TargetPet = null; // Reset target after feeding
            // Wait before looking for next pet
            yield break;
        }
    }

    public T FindNearestTarget<T>(IEnumerable<T> list) where T : MonoBehaviour, ITargetable
    {
        T nearest = null;
        float shortestDistance = float.MaxValue;
        Vector2 npcPos = GetAnchoredPos(_monsterController.transform);
        
        foreach (var t in list)
        {
            if (t == null || !t.IsTargetable) continue;
            float d = Vector2.Distance(npcPos, t.Position);
            if (d < shortestDistance) 
            { 
                shortestDistance = d; 
                nearest = t; 
            }
        }
        return nearest;
    }

    private bool OnDoingAction()
    {
        // Handle other actions here
        if (OnCollectingCoin || OnCollectingPoop || OnPetInteraction || OnFeedingPet)
        {
            return true;
        }
        return false;
    }
    
    private Vector2 GetAnchoredPos(Transform target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            // Use anchoredPosition for UI elements
            return rectTransform.anchoredPosition;
        }
        else
        {
            // Fallback to world position for non-UI objects
            return new Vector2(target.position.x, target.position.y);
        }
    }

}
