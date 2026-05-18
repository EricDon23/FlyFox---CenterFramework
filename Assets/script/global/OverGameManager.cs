using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OverGameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GlobalConter.Instance.GameOver += GameOver;
    }
    void GameOver()
    {
       Application.Quit();//退出游戏
       EditorApplication.isPlaying = false;
    }
}
