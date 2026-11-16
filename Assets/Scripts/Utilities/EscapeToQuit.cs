using UnityEngine;

namespace Vampire
{
    /// <summary>
    /// Раньше по Esc игра сразу закрывалась (и в бою тоже, т.к. объект переживает смену сцены).
    /// Теперь Esc — это пауза/настройки (см. GameUI), а выход — кнопкой «Выйти из игры» в настройках.
    /// Компонент оставлен пустым, чтобы не ломать сцену Main Menu.
    /// </summary>
    public class EscapeToQuit : MonoBehaviour
    {
    }
}
