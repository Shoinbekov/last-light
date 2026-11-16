using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Vampire
{
    public class LocaleDropdown : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;
    
        IEnumerator Start()
        {
            // Wait for the localization system to initialize
            yield return LocalizationSettings.InitializationOperation;
    
            // Восстанавливаем язык, выбранный в прошлый раз
            string saved = PlayerPrefs.GetString(PrefKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                var savedLocale = LocalizationSettings.AvailableLocales.GetLocale(saved);
                if (savedLocale != null)
                    LocalizationSettings.SelectedLocale = savedLocale;
            }

            // Generate list of available Locales
            var options = new List<TMP_Dropdown.OptionData>();
            int selected = 0;
            for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; ++i)
            {
                var locale = LocalizationSettings.AvailableLocales.Locales[i];
                if (LocalizationSettings.SelectedLocale == locale)
                    selected = i;
                // Показываем красивое имя языка ("Русский"), а не имя файла
                string label = string.IsNullOrEmpty(locale.LocaleName) ? locale.name : locale.LocaleName;
                options.Add(new TMP_Dropdown.OptionData(label));
            }
            dropdown.options = options;
    
            dropdown.value = selected;
            dropdown.onValueChanged.AddListener(LocaleSelected);
        }
    
        const string PrefKey = "SelectedLocale";

        static void LocaleSelected(int index)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[index];
            LocalizationSettings.SelectedLocale = locale;
            PlayerPrefs.SetString(PrefKey, locale.Identifier.Code);
            PlayerPrefs.Save();
        }
    }
}