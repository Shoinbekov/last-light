using System;
using UnityEngine;

namespace Vampire
{
    /// <summary>Результат одного забега — для экрана Game Over.</summary>
    public struct RunResult
    {
        public float time;          // сколько продержался в этом забеге (сек)
        public float previousBest;  // рекорд ДО этого забега (0 = рекорда ещё не было)
        public bool levelPassed;    // убил финального босса
        public bool IsFirstRun => previousBest <= 0f;
        public bool IsNewRecord => time > previousBest;
    }

    /// <summary>
    /// Прогресс игрока: лучшее время, число забегов и побед.
    /// Хранится в PlayerPrefs, переживает перезапуск игры.
    /// </summary>
    public static class RecordManager
    {
        const string BestKey = "BestSurvivalTime";
        const string RunsKey = "RunsPlayed";
        const string WinsKey = "LevelsPassed";
        const string KillsKey = "BestKills";

        public static float BestTime => PlayerPrefs.GetFloat(BestKey, 0f);
        public static int RunsPlayed => PlayerPrefs.GetInt(RunsKey, 0);
        public static int Wins => PlayerPrefs.GetInt(WinsKey, 0);
        public static int BestKills => PlayerPrefs.GetInt(KillsKey, 0);

        /// <summary>Вызывается, когда забег закончился (смерть или победа).</summary>
        public static event Action<RunResult> OnRunFinished;

        public static RunResult SubmitRun(float time, bool levelPassed, int kills)
        {
            var result = new RunResult { time = time, previousBest = BestTime, levelPassed = levelPassed };

            PlayerPrefs.SetInt(RunsKey, RunsPlayed + 1);
            if (levelPassed) PlayerPrefs.SetInt(WinsKey, Wins + 1);
            if (result.IsNewRecord) PlayerPrefs.SetFloat(BestKey, time);
            if (kills > BestKills) PlayerPrefs.SetInt(KillsKey, kills);
            PlayerPrefs.Save();

            OnRunFinished?.Invoke(result);
            return result;
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(BestKey);
            PlayerPrefs.DeleteKey(RunsKey);
            PlayerPrefs.DeleteKey(WinsKey);
            PlayerPrefs.DeleteKey(KillsKey);
            PlayerPrefs.Save();
        }

        public static string Format(float seconds)
        {
            var t = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
            return t.ToString(@"mm\:ss");
        }
    }
}
