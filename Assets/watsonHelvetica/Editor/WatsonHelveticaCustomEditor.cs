
using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(WatsonHelvetica))] 

class WatsonHelveticaCustomEditor:Editor
{
    static void WatsonHelvetica()
    {
        GameObject newWatsonHelvetica = new GameObject("Watson Helvetica");

        //add character models
        GameObject newAlphabets;

        newAlphabets = Instantiate(AssetDatabase.LoadAssetAtPath("Assets/WatsonHelvetica/Models/_Alphabets.fbx", typeof(GameObject))) as GameObject;
        newAlphabets.name = "_Alphabets";
        newAlphabets.transform.parent = newWatsonHelvetica.transform;

        //add script
        newWatsonHelvetica.AddComponent<WatsonHelvetica>();

    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

