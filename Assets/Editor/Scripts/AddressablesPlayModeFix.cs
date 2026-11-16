using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// Чтобы в редакторе всегда были актуальные языки (а не старая сборка с китайским),
    /// переключаем Addressables в режим "Use Asset Database" при открытии проекта.
    /// Меню Tools → Vampire → Rebuild Addressables — пересобрать перед сборкой .exe.
    /// </summary>
    [InitializeOnLoad]
    static class AddressablesPlayModeFix
    {
        static AddressablesPlayModeFix()
        {
            EditorApplication.delayCall += Apply;
        }

        static void Apply()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            for (int i = 0; i < settings.DataBuilders.Count; i++)
            {
                if (settings.DataBuilders[i] is BuildScriptFastMode)
                {
                    if (settings.ActivePlayModeDataBuilderIndex != i)
                    {
                        settings.ActivePlayModeDataBuilderIndex = i;
                        Debug.Log("Addressables: Play Mode Script → Use Asset Database (актуальные языки)");
                    }
                    return;
                }
            }
        }

        [MenuItem("Tools/Vampire/Rebuild Addressables")]
        static void Rebuild()
        {
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("Addressables пересобраны. Теперь можно делать File → Build.");
        }
    }
}
