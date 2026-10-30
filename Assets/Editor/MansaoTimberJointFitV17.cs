#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MansaoTimberJointFitV17
{
    [MenuItem("Tools/Mansao/Ajustar blocos de madeira sobre a pedra V17")]
    private static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Transform mansion = Selection.activeTransform;
        while (mansion != null && mansion.name != "Mansion") mansion = mansion.parent;
        Transform parent = mansion == null ? null : mansion.Find("GroundFloor_V2/Exterior_Timber_Bands");
        if (parent == null) { Tell("Selecione um objeto da mansao."); return; }
        string[] suffixes = { "Office_OuterCorner", "Office_FrontCorner", "Office_InnerFront", "Office_EntryReturn", "Living_OuterCorner", "Living_FrontCorner", "Living_InnerFront", "Living_EntryReturn" };
        var targets = new List<Transform>();
        foreach (string suffix in suffixes)
        {
            Transform found = null;
            foreach (Transform item in parent.GetComponentsInChildren<Transform>(true))
            {
                if (!item.gameObject.activeInHierarchy) continue;
                if (item.name != "Timber_Joint_" + suffix && item.name != "Timber_Joint_" + suffix + "_Upper") continue;
                if (found != null) { Tell("Ha dois blocos ativos para " + suffix + ". Nada foi alterado."); return; }
                found = item;
            }
            if (found == null) { Tell("Bloco nao encontrado: " + suffix + ". Nada foi alterado."); return; }
            Vector3 targetScale = new Vector3(.8125f, 1, .8125f);
            if (Vector3.Distance(found.localScale, targetScale) < .0001f) continue;
            if (Vector3.Distance(found.localScale, Vector3.one) > .0001f || Vector3.Distance(found.parent.lossyScale, Vector3.one) > .0001f)
            { Tell("Escala inesperada em " + found.name + ". Nada foi alterado."); return; }
            MeshFilter filter = found.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || Vector3.Distance(filter.sharedMesh.bounds.size, new Vector3(.32f, .32f, .32f)) > .002f)
            { Tell("O bloco " + found.name + " nao tem as medidas esperadas de 0.32 m. Nada foi alterado."); return; }
            targets.Add(found);
        }
        if (targets.Count == 0) { Tell("Os oito blocos ja estao ajustados."); return; }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Ajustar largura dos blocos de madeira V17");
        foreach (Transform item in targets)
        {
            Undo.RecordObject(item, "Reduzir bloco horizontalmente");
            item.localScale = new Vector3(.8125f, 1, .8125f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);
        Tell("Blocos ajustados: " + targets.Count + ". Largura e profundidade finais: 0.26 m; altura mantida em 0.32 m. Ctrl+Z desfaz o lote.");
    }
    private static void Tell(string message) { EditorUtility.DisplayDialog("Encaixe da madeira V17", message, "OK"); }
}
#endif
