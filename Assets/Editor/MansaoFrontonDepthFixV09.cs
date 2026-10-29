#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

// Importar em Assets/Editor. Corrige somente a profundidade do reboco da V08.
public static class MansaoFrontonDepthFixV09
{
    [MenuItem("Tools/Mansao/Corrigir profundidade do frontao V09")]
    private static void Fix()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Show("Saia do Play Mode primeiro."); return; }
        Transform mansion = Selection.activeTransform;
        while (mansion != null && mansion.name != "Mansion") mansion = mansion.parent;
        Transform exterior = mansion == null ? null : mansion.Find("UpperFloor_V2/Exterior_Roofs");
        Transform group = exterior == null ? null : exterior.Find("Fronton_Central_Integrated_V08");
        if (group == null || !group.gameObject.activeInHierarchy)
        { Show("Selecione um objeto da mansao. O conjunto V08 precisa estar ativo."); return; }
        const string replacementName = "Fronton_Plaster_Exterior_V09";
        if (group.Find(replacementName) != null)
        { Show("A correcao V09 ja existe. Nao criei outra copia."); return; }
        Transform original = group.Find("Fronton_Integrated_Plaster_V08");
        ProBuilderMesh source = original == null ? null : original.GetComponent<ProBuilderMesh>();
        Renderer sourceRenderer = original == null ? null : original.GetComponent<Renderer>();
        if (source == null || sourceRenderer == null || !original.gameObject.activeInHierarchy ||
            Mathf.Abs(sourceRenderer.bounds.min.z + 4.94f) > .015f ||
            Mathf.Abs(sourceRenderer.bounds.max.z + 4.48f) > .015f)
        { Show("O preenchimento V08 nao esta na profundidade prevista. Nada foi alterado."); return; }

        Renderer wall = null;
        Renderer ceiling = null;
        foreach (Transform item in mansion.GetComponentsInChildren<Transform>(true))
        {
            if (item.name == "Wall_Upper_Atrium_Window_Above_New" && item.gameObject.activeInHierarchy)
                wall = item.GetComponent<Renderer>();
            if (item.name == "Ceiling_Upper_Atrium" && item.gameObject.activeInHierarchy)
                ceiling = item.GetComponent<Renderer>();
        }
        if (wall == null || Mathf.Abs(wall.bounds.min.z + 4.8f) > .015f ||
            Mathf.Abs(wall.bounds.max.z + 4.5f) > .015f || ceiling == null ||
            Mathf.Abs(ceiling.bounds.min.y - 7.2f) > .015f)
        { Show("Nao consegui confirmar a parede (Z -4.80 a -4.50) e o teto interno (Y 7.20). Nada foi alterado."); return; }

        float outerWall = wall.bounds.min.z;
        float innerWall = wall.bounds.max.z;
        float rearLimit = outerWall + .005f; // Encaixe de 5 mm na face externa.
        // Confere tambem moldura, cornijas e pequena cobertura da V08.
        foreach (Renderer part in group.GetComponentsInChildren<Renderer>())
        {
            if (part == sourceRenderer) continue;
            // A pequena cobertura pode avancar para tras somente acima do teto interno.
            if (part.bounds.max.z > innerWall - .02f && part.bounds.min.y < ceiling.bounds.min.y + .02f)
            {
                Show("Outra peca da V08 precisa de revisao: " + part.name +
                    ". Ela ultrapassa o limite de seguranca externo. Nada foi alterado.");
                return;
            }
        }

        var positions = new List<Vector3>();
        int adjusted = 0;
        foreach (Vector3 local in source.positions)
        {
            Vector3 world = original.TransformPoint(local);
            if (world.z > rearLimit) { world.z = rearLimit; adjusted++; }
            positions.Add(group.InverseTransformPoint(world));
        }
        if (adjusted == 0) { Show("Nenhum vertice precisa desta correcao. Nada foi alterado."); return; }
        var faces = new List<Face>();
        foreach (Face face in source.faces) faces.Add(new Face(face));

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Corrigir profundidade do frontao V09");
        try
        {
            ProBuilderMesh fixedMesh = ProBuilderMesh.Create(positions, faces);
            SceneManager.MoveGameObjectToScene(fixedMesh.gameObject, group.gameObject.scene);
            fixedMesh.name = replacementName;
            fixedMesh.transform.SetParent(group, false);
            fixedMesh.transform.localPosition = Vector3.zero;
            fixedMesh.transform.localRotation = Quaternion.identity;
            fixedMesh.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(fixedMesh.gameObject, "Criar preenchimento externo V09");
            fixedMesh.GetComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
            fixedMesh.ToMesh();
            fixedMesh.Refresh();

            Bounds corrected = fixedMesh.GetComponent<Renderer>().bounds;
            if (corrected.max.z > rearLimit + .001f ||
                Mathf.Abs(corrected.min.z - sourceRenderer.bounds.min.z) > .001f ||
                Mathf.Abs(corrected.min.y - sourceRenderer.bounds.min.y) > .001f ||
                Mathf.Abs(corrected.max.y - sourceRenderer.bounds.max.y) > .001f ||
                Mathf.Abs(corrected.min.x - sourceRenderer.bounds.min.x) > .001f ||
                Mathf.Abs(corrected.max.x - sourceRenderer.bounds.max.x) > .001f)
                throw new System.InvalidOperationException("Os limites da nova peca nao passaram na verificacao.");

            Undo.RecordObject(original.gameObject, "Preservar preenchimento V08");
            original.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
            Selection.activeGameObject = fixedMesh.gameObject;
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("V09 aplicada: face frontal preservada; fundo limitado a Z " + rearLimit.ToString("F3") +
                ". Distancia ate a face interna da parede: " + (innerWall - corrected.max.z).ToString("F3") +
                " m. Demais pecas ativas da V08 conferidas em relacao a parede e ao teto. Parede, janela e teto preservados.", fixedMesh);
        }
        catch (System.Exception error)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(error);
            Show("A correcao foi desfeita devido a um erro. Consulte a Console.");
        }
    }

    private static void Show(string message)
    { EditorUtility.DisplayDialog("Profundidade do frontao", message, "OK"); }
}
#endif
