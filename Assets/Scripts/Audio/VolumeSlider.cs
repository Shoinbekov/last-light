using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vampire
{
    /// <summary>
    /// Повесь на UI Slider — он станет регулятором громкости (0–100%).
    /// Можно указать текст, в котором будет показан процент.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class VolumeSlider : MonoBehaviour
    {
        [SerializeField] private TMP_Text percentText;

        private Slider slider;

        void Awake()
        {
            slider = GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        void OnEnable()
        {
            // Меню паузы включается/выключается — каждый раз показываем актуальное значение
            slider.SetValueWithoutNotify(GameAudio.Volume);
            UpdateText(slider.value);
            slider.onValueChanged.AddListener(OnChanged);
        }

        void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnChanged);
        }

        /// <summary>Для UI, созданного из кода: куда писать проценты.</summary>
        public void SetPercentText(TMP_Text text)
        {
            percentText = text;
            if (slider != null) UpdateText(slider.value);
        }

        void OnChanged(float value)
        {
            GameAudio.Volume = value;
            UpdateText(value);
        }

        void UpdateText(float value)
        {
            if (percentText != null)
                percentText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }
}
