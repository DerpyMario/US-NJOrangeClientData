using UnityEngine;
using UnityEditor;
using StageLib;
using System.IO;
using System.Collections.Generic;

public class BasicStageJsonImporter : EditorWindow
{
    private string jsonPath = "";
    private string stageName = "NewStage";
    private string prefabRootPath = "Assets/Prefabs";
    private string bundleRootPath = "Assets/AssetBundles";
    private Vector2 scrollPos;
    private StageData stageData;
    private GameObject stageRoot;
    
    [MenuItem("StageLib/Load Stage Data (experimental)")]
    static void Init()
    {
        var window = GetWindow<StageJsonImporter>();
        window.titleContent = new GUIContent("Stage Importer");
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        
        EditorGUILayout.BeginHorizontal();
        jsonPath = EditorGUILayout.TextField("JSON File", jsonPath);
        if(GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            jsonPath = EditorUtility.OpenFilePanel("Select Stage JSON", "", "json");
        }
        EditorGUILayout.EndHorizontal();

        stageName = EditorGUILayout.TextField("Stage Name", stageName);
        prefabRootPath = EditorGUILayout.TextField("Prefab Root Path", prefabRootPath);
        bundleRootPath = EditorGUILayout.TextField("Bundle Root Path", bundleRootPath);

        if(GUILayout.Button("Load Stage data"))
        {
            LoadAndCreateStage();
        }

        DisplayStageInfo();

        EditorGUILayout.EndVertical();
    }

    private void DisplayStageInfo()
    {
        if(stageData == null) return;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.LabelField("Stage Data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Version: {stageData.nVer}");
        EditorGUILayout.LabelField($"Clip Width: {stageData.fStageClipWidth}");
        EditorGUILayout.LabelField($"Groups: {stageData.Datas.Count}");
        EditorGUILayout.EndScrollView();
    }

    private void LoadAndCreateStage()
    {
        if(string.IsNullOrEmpty(jsonPath)) return;

        string jsonContent = File.ReadAllText(jsonPath);
        stageData = StageData.LoadByJSONStr(jsonContent);
        
        CreateStageHierarchy();
    }

    private void CreateStageHierarchy()
    {
        stageRoot = new GameObject(stageName);
        int groupIndex = 0;
        
        foreach(var groupData in stageData.Datas)
        {
            GameObject groupObj = new GameObject($"Group_{groupIndex++}_{groupData.fClipMinx:F2}_{groupData.fClipMaxx:F2}");
            groupObj.transform.SetParent(stageRoot.transform);

            foreach(var objData in groupData.Datas)
            {
                CreateStageObject(objData, groupObj.transform);
            }
        }

        Selection.activeGameObject = stageRoot;
        EditorUtility.SetDirty(stageRoot);
    }

    private void CreateStageObject(StageObjData objData, Transform parent)
    {
        GameObject prefab = LoadPrefabFromPaths(objData);
        GameObject obj = prefab != null ? PrefabUtility.InstantiatePrefab(prefab) as GameObject : new GameObject(objData.name);
        
        obj.transform.SetParent(parent);
        obj.name = objData.name;
        obj.transform.localPosition = objData.position;
        obj.transform.localRotation = objData.rotate;
        obj.transform.localScale = objData.scale;

        ProcessObjectProperties(obj, objData);
    }

    private GameObject LoadPrefabFromPaths(StageObjData objData)
    {
        string[] searchPaths = {
            objData.path,
            Path.Combine(prefabRootPath, objData.path),
            Path.Combine(bundleRootPath, objData.bunldepath),
            Path.Combine("Assets/Resources", objData.path)
        };

        foreach(string path in searchPaths)
        {
            if(string.IsNullOrEmpty(path)) continue;
            
            string assetPath = path;
            if(!path.EndsWith(".prefab")) assetPath += ".prefab";
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if(prefab != null) return prefab;
        }

        return null;
    }

    private void ProcessObjectProperties(GameObject obj, StageObjData objData)
    {
        if(string.IsNullOrEmpty(objData.property)) return;

        string[] props = objData.property.Split(',');
        if(props.Length == 0) return;

        int typeId;
        if(!int.TryParse(props[0], out typeId)) return;

        StageObjType objType = (StageObjType)typeId;
        AddStageComponent(obj, objType, objData);

        var objIniter = obj.GetComponent<StageObjIniter>() ?? obj.AddComponent<StageObjIniter>();
        objIniter.sPrefabPath = objData.path;
        objIniter.sImagePath = objData.bunldepath;
    }

    private void AddStageComponent(GameObject obj, StageObjType type, StageObjData data)
    {
        StageSLBase component = null;

        switch(type)
        {
            case StageObjType.START_OBJ:
                component = obj.AddComponent<StageStartPoint>();
                break;
            case StageObjType.MAPEVENT_OBJ:
                component = obj.AddComponent<StageOneWorkEvent>();
                break;
            case StageObjType.RIDEABLE_OBJ:
                component = obj.AddComponent<StageRideableObj>();
                break;
            case StageObjType.STAGEREBORN_OBJ:
                component = obj.AddComponent<StageRebounEvent>();
                break;
            case StageObjType.PATROLPATH_OBJ:
                component = obj.AddComponent<StagePatrolPath>();
                break;
        }

        if(component != null)
        {
            component.LoadByString(data.property);
        }
    }
}
