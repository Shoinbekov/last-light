using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vampire
{
    /// <summary>
    /// Дополнительный интерфейс, который создаётся из кода (в сценах ничего настраивать не нужно):
    ///  • Главное меню: кнопка «Настройки» (громкость, язык, сброс прогресса) и панель рекорда в углу.
    ///  • Уровень: пауза на Esc (продолжить, громкость, в главное меню), рекорд в углу
    ///    и сообщение после забега — сравнение с рекордом или поздравление с новым рекордом.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        const string MenuScene = "Main Menu";
        const string LocalePrefKey = "SelectedLocale";   // тот же ключ, что в LocaleDropdown

        // ---------- цвета ----------
        static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.14f, 0.94f);
        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);
        static readonly Color ButtonColor = new Color(0.24f, 0.24f, 0.31f, 1f);
        static readonly Color AccentColor = new Color(0.91f, 0.69f, 0.29f, 1f);   // золотой
        static readonly Color TextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        static readonly Color MutedColor = new Color(0.70f, 0.70f, 0.76f, 1f);
        static readonly Color GoodColor = new Color(0.45f, 0.90f, 0.45f, 1f);

        // ---------- тексты (английский / русский) ----------
        static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            //               English                         Русский
            { "settings",   new[] { "Settings",              "Настройки" } },
            { "volume",     new[] { "Volume",                "Громкость" } },
            { "language",   new[] { "Language",              "Язык" } },
            { "close",      new[] { "Close",                 "Закрыть" } },
            { "quit",       new[] { "Quit game",             "Выйти из игры" } },
            { "reset",      new[] { "Reset progress",        "Сбросить прогресс" } },
            { "resetSure",  new[] { "Sure? Click again",     "Точно? Нажми ещё раз" } },
            { "pause",      new[] { "PAUSE",                 "ПАУЗА" } },
            { "resume",     new[] { "Resume",                "Продолжить" } },
            { "mainMenu",   new[] { "Main Menu",             "Главное меню" } },
            { "escHint",    new[] { "Esc — pause",           "Esc — пауза" } },
            { "bestRun",    new[] { "BEST RUN",              "ЛУЧШИЙ ЗАБЕГ" } },
            { "noRuns",     new[] { "No runs yet",           "Забегов ещё нет" } },
            { "runs",       new[] { "Runs",                  "Забегов" } },
            { "wins",       new[] { "Wins",                  "Побед" } },
            { "kills",      new[] { "Best kills",            "Рекорд убийств" } },
            { "best",       new[] { "Best",                  "Рекорд" } },
            { "newRecord",  new[] { "NEW RECORD!",           "НОВЫЙ РЕКОРД!" } },
            { "newRecordBody", new[] { "You survived {0} — longer than your old record {1}!",
                                       "Ты продержался {0} — дольше прежнего рекорда {1}!" } },
            { "firstRecord",   new[] { "FIRST RECORD",       "ПЕРВЫЙ РЕКОРД" } },
            { "firstRecordBody", new[] { "You survived {0}. Now try to beat it!",
                                         "Ты продержался {0}. Теперь попробуй побить!" } },
            { "noRecord",   new[] { "Your time: {0}",        "Твоё время: {0}" } },
            { "noRecordBody", new[] { "Record: {0} — {1} short. Try again!",
                                      "Рекорд: {0} — не хватило {1}. Попробуй ещё!" } },
        };

        static string Lang
        {
            get
            {
                // не дёргаем систему локализации, пока она не загрузилась
                if (!LocalizationSettings.HasSettings) return "en";
                var op = LocalizationSettings.SelectedLocaleAsync;
                var loc = op.IsDone ? op.Result : null;
                return loc != null && loc.Identifier.Code.StartsWith("ru") ? "ru" : "en";
            }
        }

        static string T(string key)
        {
            string[] v;
            if (!Texts.TryGetValue(key, out v)) return key;
            return Lang == "ru" ? v[1] : v[0];
        }

        // ================== запуск ==================
        static GameUI host;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (host != null) return;
            var go = new GameObject("Game UI Host");
            DontDestroyOnLoad(go);
            host = go.AddComponent<GameUI>();
        }

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            RecordManager.OnRunFinished += OnRunFinished;
            StartCoroutine(RestoreLocale());
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RecordManager.OnRunFinished -= OnRunFinished;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        IEnumerator RestoreLocale()
        {
            yield return LocalizationSettings.InitializationOperation;
            string saved = PlayerPrefs.GetString(LocalePrefKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                var loc = LocalizationSettings.AvailableLocales.GetLocale(saved);
                if (loc != null) LocalizationSettings.SelectedLocale = loc;
            }
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            RefreshTexts();
        }

        // ================== состояние текущей сцены ==================
        Canvas canvas;
        bool isMenu, isLevel;
        GameObject settingsPanel, pausePanel;
        bool paused, runFinished;
        float timeScaleBeforePause = 1f;
        readonly List<KeyValuePair<TMP_Text, Func<string>>> bindings = new List<KeyValuePair<TMP_Text, Func<string>>>();
        readonly List<Action> refreshers = new List<Action>();

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            bindings.Clear();
            refreshers.Clear();
            paused = false;
            runFinished = false;
            settingsPanel = pausePanel = null;

            isMenu = scene.name == MenuScene;
            isLevel = !isMenu && FindObjectOfType<LevelManager>() != null;
            if (!isMenu && !isLevel) return;

            canvas = CreateCanvas();
            if (isMenu) BuildMainMenu();
            else BuildLevel();
            RefreshTexts();
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if (isMenu)
            {
                if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf);
                return;
            }
            if (!isLevel || runFinished) return;

            if (paused) Resume();
            else if (Time.timeScale > 0f) Pause();   // если игра уже на паузе (выбор способности и т.п.) — не трогаем
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale locale) { RefreshTexts(); }

        void RefreshTexts()
        {
            foreach (var b in bindings)
                if (b.Key != null) b.Key.text = b.Value();
            foreach (var r in refreshers) r();
        }

        void Bind(TMP_Text text, Func<string> getter)
        {
            bindings.Add(new KeyValuePair<TMP_Text, Func<string>>(text, getter));
            text.text = getter();
        }

        // ================== главное меню ==================
        void BuildMainMenu()
        {
            // Кнопка «Настройки» — правый верхний угол
            var btn = MakeButton(canvas.transform, "settings", () => settingsPanel.SetActive(true));
            Place(btn.transform, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(260, 70));

            // Панель рекорда — левый нижний угол
            var box = MakeImage("Record Panel", canvas.transform, PanelColor);
            Place(box.transform, new Vector2(0, 0), new Vector2(30, 30), new Vector2(420, 210));
            var title = MakeText(box.transform, 26, AccentColor, TextAlignmentOptions.TopLeft);
            Stretch(title.rectTransform, 24, 18, 24, 18);
            Bind(title, () => T("bestRun"));
            var big = MakeText(box.transform, 64, TextColor, TextAlignmentOptions.Left);
            Stretch(big.rectTransform, 24, 18, 24, 18);
            big.fontStyle = FontStyles.Bold;
            Bind(big, () => RecordManager.RunsPlayed == 0 ? "--:--" : RecordManager.Format(RecordManager.BestTime));
            var info = MakeText(box.transform, 24, MutedColor, TextAlignmentOptions.BottomLeft);
            Stretch(info.rectTransform, 24, 18, 24, 18);
            Bind(info, () => RecordManager.RunsPlayed == 0
                ? T("noRuns")
                : T("runs") + ": " + RecordManager.RunsPlayed + "    " + T("wins") + ": " + RecordManager.Wins
                  + "\n" + T("kills") + ": " + RecordManager.BestKills);

            settingsPanel = BuildSettingsPanel(true);
            settingsPanel.SetActive(false);
        }

        // ================== уровень ==================
        void BuildLevel()
        {
            // Рекорд и подсказка про Esc — правый верхний угол, небольшим текстом
            var corner = MakeText(canvas.transform, 26, MutedColor, TextAlignmentOptions.TopRight);
            Place(corner.transform, new Vector2(1, 1), new Vector2(-30, -110), new Vector2(420, 80));
            Bind(corner, () => (RecordManager.RunsPlayed > 0 ? T("best") + ": " + RecordManager.Format(RecordManager.BestTime) + "\n" : "") + T("escHint"));
            corner.raycastTarget = false;

            pausePanel = BuildSettingsPanel(false);
            pausePanel.SetActive(false);
        }

        void Pause()
        {
            paused = true;
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
        }

        void Resume()
        {
            paused = false;
            Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
            pausePanel.SetActive(false);
        }

        void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        // ================== окно настроек / паузы ==================
        GameObject BuildSettingsPanel(bool inMenu)
        {
            // затемнение всего экрана (и блокирует клики по игре)
            var dim = MakeImage(inMenu ? "Settings" : "Pause Menu", canvas.transform, DimColor);
            Stretch(dim.rectTransform, 0, 0, 0, 0);

            var box = MakeImage("Window", dim.transform, PanelColor);
            Place(box.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, inMenu ? 650 : 520));

            var title = MakeText(box.transform, 52, AccentColor, TextAlignmentOptions.Center);
            Place(title.transform, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(640, 70));
            title.fontStyle = FontStyles.Bold;
            Bind(title, () => inMenu ? T("settings") : T("pause"));

            float y = -130;
            if (!inMenu)
            {
                var resume = MakeButton(box.transform, "resume", Resume, AccentColor, Color.black);
                Place(resume.transform, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(420, 80));
                y -= 115;
            }

            // Громкость: подпись, ползунок, проценты
            var volLabel = MakeText(box.transform, 30, TextColor, TextAlignmentOptions.Left);
            Place(volLabel.transform, new Vector2(0.5f, 1), new Vector2(-220, y), new Vector2(200, 50));
            Bind(volLabel, () => T("volume"));
            var slider = MakeSlider(box.transform);
            Place(slider.transform, new Vector2(0.5f, 1), new Vector2(40, y - 10), new Vector2(300, 30));
            var pct = MakeText(box.transform, 30, TextColor, TextAlignmentOptions.Right);
            Place(pct.transform, new Vector2(0.5f, 1), new Vector2(270, y), new Vector2(110, 50));
            slider.gameObject.AddComponent<VolumeSlider>().SetPercentText(pct);
            y -= 90;

            if (inMenu)
            {
                // Язык: две кнопки
                var langLabel = MakeText(box.transform, 30, TextColor, TextAlignmentOptions.Left);
                Place(langLabel.transform, new Vector2(0.5f, 1), new Vector2(-220, y), new Vector2(200, 50));
                Bind(langLabel, () => T("language"));
                var en = MakeButton(box.transform, null, () => SetLanguage("en"));
                Place(en.transform, new Vector2(0.5f, 1), new Vector2(30, y + 5), new Vector2(190, 60));
                SetLabel(en, "English");
                var ru = MakeButton(box.transform, null, () => SetLanguage("ru"));
                Place(ru.transform, new Vector2(0.5f, 1), new Vector2(240, y + 5), new Vector2(190, 60));
                SetLabel(ru, "Русский");
                refreshers.Add(() =>
                {
                    en.image.color = Lang == "en" ? AccentColor : ButtonColor;
                    ru.image.color = Lang == "ru" ? AccentColor : ButtonColor;
                    en.GetComponentInChildren<TMP_Text>().color = Lang == "en" ? Color.black : TextColor;
                    ru.GetComponentInChildren<TMP_Text>().color = Lang == "ru" ? Color.black : TextColor;
                });
                y -= 100;

                // Сброс прогресса (по двойному нажатию)
                bool armed = false;
                Button reset = null;
                reset = MakeButton(box.transform, null, () =>
                {
                    if (!armed) { armed = true; SetLabel(reset, T("resetSure")); return; }
                    armed = false;
                    RecordManager.ResetProgress();
                    RefreshTexts();
                });
                Place(reset.transform, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(420, 64));
                reset.image.color = new Color(0.45f, 0.18f, 0.18f, 1f);
                refreshers.Add(() => { armed = false; SetLabel(reset, T("reset")); });

                var quit = MakeButton(box.transform, "quit", QuitGame);
                Place(quit.transform, new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(420, 64));

                var close = MakeButton(box.transform, "close", () => settingsPanel.SetActive(false), AccentColor, Color.black);
                Place(close.transform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(420, 80));
            }
            else
            {
                var menu = MakeButton(box.transform, "mainMenu", GoToMainMenu);
                Place(menu.transform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(420, 80));
            }
            return dim.gameObject;
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void SetLanguage(string code)
        {
            var loc = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (loc == null) return;
            LocalizationSettings.SelectedLocale = loc;   // вызовет OnLocaleChanged → RefreshTexts
            PlayerPrefs.SetString(LocalePrefKey, code);
            PlayerPrefs.Save();
        }

        // ================== итог забега ==================
        void OnRunFinished(RunResult r)
        {
            if (!isLevel || canvas == null) return;
            runFinished = true;
            if (paused) { paused = false; pausePanel.SetActive(false); }

            Color frame = r.IsNewRecord ? AccentColor : MutedColor;
            var outer = MakeImage("Run Result", canvas.transform, frame);
            Place(outer.transform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(900, 124));
            var box = MakeImage("Inner", outer.transform, PanelColor);
            Stretch(box.rectTransform, 4, 4, 4, 4);

            var head = MakeText(box.transform, 40, r.IsNewRecord ? AccentColor : TextColor, TextAlignmentOptions.Top);
            Stretch(head.rectTransform, 20, 10, 20, 10);
            head.fontStyle = FontStyles.Bold;
            var body = MakeText(box.transform, 26, r.IsNewRecord ? GoodColor : MutedColor, TextAlignmentOptions.Bottom);
            Stretch(body.rectTransform, 20, 12, 20, 12);

            string now = RecordManager.Format(r.time), old = RecordManager.Format(r.previousBest);
            if (r.IsFirstRun)
            {
                Bind(head, () => T("firstRecord"));
                Bind(body, () => string.Format(T("firstRecordBody"), now));
            }
            else if (r.IsNewRecord)
            {
                Bind(head, () => T("newRecord"));
                Bind(body, () => string.Format(T("newRecordBody"), now, old));
                StartCoroutine(Pulse(outer.transform));
            }
            else
            {
                string diff = RecordManager.Format(r.previousBest - r.time);
                Bind(head, () => string.Format(T("noRecord"), now));
                Bind(body, () => string.Format(T("noRecordBody"), old, diff));
            }
            outer.raycastTarget = box.raycastTarget = false;
        }

        IEnumerator Pulse(Transform t)
        {
            // мягкая «пульсация» поздравления (работает на паузе)
            float time = 0;
            while (t != null)
            {
                time += Time.unscaledDeltaTime;
                float s = 1f + 0.03f * Mathf.Sin(time * 5f);
                t.localScale = new Vector3(s, s, 1);
                yield return null;
            }
        }

        // ================== помощники для UI ==================
        Canvas CreateCanvas()
        {
            var go = new GameObject("Game UI (code)");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 500;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return c;
        }

        static void Place(Transform t, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static TMP_Text MakeText(Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = true;
            t.enableAutoSizing = true;      // если текст длинный — шрифт уменьшится, а не вылезет за рамку
            t.fontSizeMin = Mathf.Max(14f, size * 0.4f);
            t.fontSizeMax = size;
            t.raycastTarget = false;
            return t;
        }

        Button MakeButton(Transform parent, string key, Action onClick)
        {
            return MakeButton(parent, key, onClick, ButtonColor, TextColor);
        }

        Button MakeButton(Transform parent, string key, Action onClick, Color bg, Color fg)
        {
            var img = MakeImage("Button", parent, bg);
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            b.colors = cb;
            b.onClick.AddListener(() => onClick());
            var label = MakeText(img.transform, 32, fg, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 8, 4, 8, 4);
            if (key != null) Bind(label, () => T(key));
            return b;
        }

        static void SetLabel(Button b, string text)
        {
            b.GetComponentInChildren<TMP_Text>().text = text;
        }

        static Slider MakeSlider(Transform parent)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.transform.SetParent(parent, false);
            var s = go.GetComponent<Slider>();
            foreach (var img in go.GetComponentsInChildren<Image>())
            {
                if (img.name == "Background") img.color = new Color(0.25f, 0.25f, 0.30f, 1f);
                else if (img.name == "Fill") img.color = AccentColor;
                else if (img.name == "Handle")
                {
                    img.color = Color.white;
                    img.rectTransform.sizeDelta = new Vector2(28, 14);
                }
            }
            return s;
        }
    }
}
