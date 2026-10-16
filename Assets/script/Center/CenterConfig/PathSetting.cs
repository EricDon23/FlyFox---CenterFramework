using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New PathSetting", menuName = "ResPathSetting")]
public class PathSetting : ScriptableObject
{
   public List<string> ResourcesPath = new List<string>();

    public List<string> AssetBundles = new List<string>();

    public List<string> Addressable = new List<string>();

    public List<string> UnityWebRequst = new List<string>();
}
