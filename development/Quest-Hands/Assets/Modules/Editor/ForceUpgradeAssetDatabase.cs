using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class ForceUpgradeAssetDatabase
{
    [MenuItem("pfc/Force AssetDatabase Upgrade")]
    // Start is called before the first frame update
    static void Upgrade()
    {
        AssetDatabase.ForceReserializeAssets();
    }
}
