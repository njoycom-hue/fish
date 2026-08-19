using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>디스크에 저장되는 플레이어 진행 상황.</summary>
    [Serializable]
    public class SaveData
    {
        public int highScore;
        public int totalCoins;
        public int playCount;
        public bool sfxEnabled = true;
        public bool musicEnabled = true;
    }

    /// <summary>
    /// PlayerPrefs 에 JSON 한 덩어리로 저장하는 단순 세이브 시스템.
    /// 필드를 추가해도 기존 세이브는 기본값으로 채워지므로 마이그레이션이 필요 없다.
    /// </summary>
    public static class SaveSystem
    {
        private const string Key = "game.save.v1";

        private static SaveData cached;

        public static SaveData Data
        {
            get
            {
                if (cached == null)
                {
                    Load();
                }

                return cached;
            }
        }

        public static void Load()
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                cached = new SaveData();
                return;
            }

            try
            {
                cached = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch (ArgumentException)
            {
                // 저장 데이터가 깨졌으면 조용히 초기화한다 — 게임을 막을 이유가 없다.
                Debug.LogWarning("[SaveSystem] 저장 데이터를 읽지 못해 초기화합니다.");
                cached = new SaveData();
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        /// <summary>한 판이 끝났을 때 호출 — 최고 점수 갱신 여부를 반환한다.</summary>
        public static bool SubmitRun(int score, int coins)
        {
            SaveData data = Data;
            data.playCount++;
            data.totalCoins += coins;

            bool isNewRecord = score > data.highScore;
            if (isNewRecord)
            {
                data.highScore = score;
            }

            Save();
            return isNewRecord;
        }

        /// <summary>테스트 및 "데이터 초기화" 메뉴용.</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            cached = new SaveData();
        }
    }
}
