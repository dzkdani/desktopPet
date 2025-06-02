using UnityEngine;
using Spine.Unity;

public class MonsterVisualHandler
{
    private MonsterController _controller;
    private SkeletonGraphic _skeletonGraphic;
    
    public MonsterVisualHandler(MonsterController controller, SkeletonGraphic skeletonGraphic)
    {
        _controller = controller;
        _skeletonGraphic = skeletonGraphic;
    }
    
    public void SetSpineDataBasedOnEvolution()
    {
        if (_controller.MonsterData == null || _skeletonGraphic == null) return;
        
        SkeletonDataAsset targetSkeletonData = GetCurrentSkeletonData();
        
        if (targetSkeletonData != null)
        {
            // Only change if it's actually different
            if (_skeletonGraphic.skeletonDataAsset != targetSkeletonData)
            {
                _skeletonGraphic.skeletonDataAsset = targetSkeletonData;
                _skeletonGraphic.Initialize(true);
                
                // Wait a frame before setting animation
                _controller.StartCoroutine(SetAnimationAfterFrame());
            }
        }
    }
    
    public void ApplyMonsterVisuals()
    {
        if (_controller.MonsterData == null || _skeletonGraphic == null) return;
        
        SetSpineDataBasedOnEvolution();
        
        // Ensure animation starts after spine data is set
        if (_skeletonGraphic.skeletonDataAsset != null && _skeletonGraphic.AnimationState != null)
        {
            _skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
        }
    }
    
    private System.Collections.IEnumerator SetAnimationAfterFrame()
    {
        yield return null; // Wait one frame for spine to initialize
        
        if (_skeletonGraphic.AnimationState != null)
        {
            _skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
        }
    }

    public SkeletonDataAsset GetCurrentSkeletonData()
    {
        if (_controller.MonsterData == null || 
            _controller.MonsterData.monsterSpine == null || 
            _controller.MonsterData.monsterSpine.Length == 0) 
        {
            Debug.LogWarning($"[Visual] No spine data available for monster {_controller.monsterID}");
            return null;
        }

        // Clamp to valid array bounds
        int arrayIndex = Mathf.Clamp(_controller.evolutionLevel, 0, _controller.MonsterData.monsterSpine.Length - 1);
        
        // Log if we're clamping (indicates a configuration issue)
        if (arrayIndex != _controller.evolutionLevel)
        {
            Debug.LogWarning($"[Visual] Evolution level {_controller.evolutionLevel} out of bounds for monster {_controller.monsterID}. Using index {arrayIndex} instead.");
        }
        
        var spineAsset = _controller.MonsterData.monsterSpine[arrayIndex];
        if (spineAsset == null)
        {
            Debug.LogError($"[Visual] Spine asset at index {arrayIndex} is null for monster {_controller.monsterID}");
        }
        
        return spineAsset;
    }
    
    public void UpdateMonsterVisuals()
    {
        ApplyMonsterVisuals();
    }
}
