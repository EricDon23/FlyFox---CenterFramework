using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartGameManager : MonoBehaviour
{
    [SerializeField]GameObject levelBuild;
    [SerializeField]GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        GlobalConter.Instance.GameStart += GameRuning ;
    }
    void GameRuning()
    {
       levelBuild.SetActive(true);
    }
    void updatate()
    {
        
    }
}
