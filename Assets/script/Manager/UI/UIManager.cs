using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void OnStartGAme()
    {
        
        GlobalConter.Instance.OnGameStart();
        GlobalConter.Instance.OnChangeState(GameEnum.Playing);
        gameObject.SetActive(false);
    }
    public void OnRecord()
    {
        
    }
    public void OnOverGAme()
    {
        
        GlobalConter.Instance.OnGameOver();
        GlobalConter.Instance.OnChangeState(GameEnum.GameOver);
        gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
