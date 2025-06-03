#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonsterDataSO))]
public class MonsterDataSOEditor : Editor
{
    private bool showBasicInfo = true;
    private bool showClassification = true;
    private bool showStats = true;
    private bool showHappinessSystem = true;
    private bool showPoopBehavior = true;
    private bool showEvolution = true;
    private bool showSpineData = true;
    private bool showImages = true;
    private bool showSoundEffects = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "Basic Info", true);
        if (showBasicInfo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("monsterName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("monPrice"));
            EditorGUI.indentLevel--;
        }

        showClassification = EditorGUILayout.Foldout(showClassification, "Classification", true);
        if (showClassification)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("monType"));
            EditorGUI.indentLevel--;
        }

        showStats = EditorGUILayout.Foldout(showStats, "Stats", true);
        if (showStats)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpd"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hungerDepleteRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("poopRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseHunger"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseHappiness"));
            EditorGUI.indentLevel--;
        }

        showHappinessSystem = EditorGUILayout.Foldout(showHappinessSystem, "Happiness System", true);
        if (showHappinessSystem)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pokeHappinessValue"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("areaHappinessRate"));
            EditorGUI.indentLevel--;
        }

        showPoopBehavior = EditorGUILayout.Foldout(showPoopBehavior, "Poop Behavior", true);
        if (showPoopBehavior)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clickToCollectPoop"));
            EditorGUI.indentLevel--;
        }

        showEvolution = EditorGUILayout.Foldout(showEvolution, "Evolution", true);
        if (showEvolution)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canEvolve"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isEvolved"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isFinalEvol"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("evolutionLevel"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startingEvolutionLevel"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("evolutionRequirements"));
            EditorGUI.indentLevel--;
        }

        showSpineData = EditorGUILayout.Foldout(showSpineData, "Spine Data", true);
        if (showSpineData)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("monsterSpine"), true);
            EditorGUI.indentLevel--;
        }

        showImages = EditorGUILayout.Foldout(showImages, "Images", true);
        if (showImages)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("monsImgs"), true);
            EditorGUI.indentLevel--;
        }

        showSoundEffects = EditorGUILayout.Foldout(showSoundEffects, "Sound Effects", true);
        if (showSoundEffects)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleSounds"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("happySound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("eatSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hurtSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("evolveSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deathSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("interactionSound"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif