using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// Общая громкость игры. Сохраняется между запусками (PlayerPrefs)
    /// и применяется автоматически при старте игры, в любой сцене.
    /// </summary>
    public static class GameAudio
    {
        const string PrefKey = "MasterVolume";
        const float DefaultVolume = 0.8f;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(PrefKey, DefaultVolume);
            set
            {
                float v = Mathf.Clamp01(value);
                AudioListener.volume = v;
                PlayerPrefs.SetFloat(PrefKey, v);
                PlayerPrefs.Save();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplySavedVolume()
        {
            AudioListener.volume = Volume;
        }
    }
}
