using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vampire
{
    /// <summary>
    /// Фоновая музыка. Создаётся автоматически при запуске игры — ничего
    /// добавлять в сцены не нужно. В главном меню играет MenuTheme,
    /// на уровнях — BattleTheme. Треки лежат в Assets/Resources/Music.
    /// Громкость зависит от общего ползунка (GameAudio / VolumeSlider).
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        const string MenuScene = "Main Menu";
        const string MenuTrack = "Music/MenuTheme";
        const string BattleTrack = "Music/BattleTheme";
        const float MusicVolume = 0.5f;   // музыка тише будущих звуковых эффектов
        const float FadeTime = 1.0f;

        static MusicPlayer instance;
        AudioSource source;
        Coroutine fade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("Music Player");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MusicPlayer>();
        }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.ignoreListenerPause = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Play(scene.name == MenuScene ? MenuTrack : BattleTrack);
        }

        void Play(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning("MusicPlayer: не найден трек Resources/" + path);
                return;
            }
            if (source.clip == clip && source.isPlaying) return;   // тот же трек — не перезапускаем
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(SwitchTo(clip));
        }

        IEnumerator SwitchTo(AudioClip clip)
        {
            // плавно убираем старый трек (unscaled — работает и на паузе)
            float start = source.volume;
            for (float t = 0; t < FadeTime && source.isPlaying; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / FadeTime);
                yield return null;
            }
            source.clip = clip;
            source.Play();
            for (float t = 0; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, MusicVolume, t / FadeTime);
                yield return null;
            }
            source.volume = MusicVolume;
            fade = null;
        }
    }
}
