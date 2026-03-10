using UnityEngine;
using System;

public static class PlayerIDManager
{
    private const string ID_KEY = "User_Unique_ID";

    public static string GetPlayerID()
    {
        // 1. 이미 저장된 ID가 있는지 확인
        if (!PlayerPrefs.HasKey(ID_KEY))
        {
            // 2. 없다면 새로 생성 (예: 8329-asf2-...)
            string newID = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(ID_KEY, newID);
            PlayerPrefs.Save();
        }

        return PlayerPrefs.GetString(ID_KEY);
    }
}