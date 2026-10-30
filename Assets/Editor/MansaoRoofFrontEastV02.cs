#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

// Coloque este arquivo em Assets/Editor. Executado somente pelo menu indicado.
public static class MansaoRoofFrontEastV02
{
    [MenuItem("Tools/Mansao/Criar telhado frontal leste V02")]
    private static void CreateRoof()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Telhado", "Saia do Play Mode primeiro.", "OK");
            return;
        }
        GameObject source = Selection.activeGameObject;
        if (source == null || source.name != "Ceiling_Upper_MasterRoom" ||
            source.GetComponent<ProBuilderMesh>() == null || !source.scene.IsValid())
        {
            EditorUtility.DisplayDialog("Telhado", "Selecione Ceiling_Upper_MasterRoom na Hierarchy antes de executar.", "OK");
            return;
        }
        Transform mansion = source.transform;
        while (mansion != null && mansion.name != "Mansion") mansion = mansion.parent;
        Transform parent = mansion == null ? null : mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if (parent == null)
        {
            EditorUtility.DisplayDialog("Telhado", "Nao encontrei Mansion/UpperFloor_V2/Exterior_Roofs. Nenhuma alteracao foi feita.", "OK");
            return;
        }
        if (parent.Find("Roof_FrontEast_V02") != null)
        {
            EditorUtility.DisplayDialog("Telhado", "Roof_FrontEast_V02 ja existe. Nao criei outra copia.", "OK");
            return;
        }

        Vector3[] world = {
            new Vector3(3.65f, 7.4f, -6.715683f), // A
            new Vector3(5.684317f, 7.4f, -8.75f), // B
            new Vector3(8.715683f, 7.4f, -8.75f), // C
            new Vector3(10.75f, 7.4f, -6.715683f), // D
            new Vector3(10.75f, 7.4f, -.55f), // E
            new Vector3(5.35f, 7.4f, -.55f), // F
            new Vector3(5.35f, 7.4f, -4.35f), // G
            new Vector3(3.65f, 7.4f, -4.35f), // H
            new Vector3(7.2f, 10.2f, -5.2f), // P: apice frontal
            new Vector3(8.05f, 9.2f, -2.3f) // R: ponto alto traseiro
        };
        int[][] triangles = {
            new[]{8,0,1}, new[]{8,1,2}, new[]{8,2,3},
            new[]{8,3,9}, new[]{9,3,4}, new[]{9,4,5},
            new[]{9,5,6}, new[]{9,6,8}, new[]{8,6,7}, new[]{8,7,0}
        };
        // Corrige a orientacao de cada face superior antes de fechar a espessura.
        foreach (int[] t in triangles)
        {
            if (Vector3.Cross(world[t[1]] - world[t[0]], world[t[2]] - world[t[0]]).y < 0)
            { int swap = t[1]; t[1] = t[2]; t[2] = swap; }
        }
        var positions = new List<Vector3>();
        var faces = new List<Face>();
        // Vertices separados por face preservam as quinas visuais do telhado.
        foreach (int[] t in triangles)
        {
            AddTriangle(world[t[0]], world[t[1]], world[t[2]], parent, positions, faces);
            Vector3 down = Vector3.down * .12f;
            AddTriangle(world[t[2]] + down, world[t[1]] + down, world[t[0]] + down,
                parent, positions, faces);
        }
        // Fechamentos verticais do beiral: 12 cm de espessura vertical.
        for (int i = 0; i < 8; i++)
        {
            int j = (i + 1) % 8;
            Vector3 a = world[i], b = world[j];
            Vector3 c = b + Vector3.down * .12f, d = a + Vector3.down * .12f;
            AddTriangle(a, b, c, parent, positions, faces);
            AddTriangle(a, c, d, parent, positions, faces);
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Criar telhado frontal leste V02");
        ProBuilderMesh roof = ProBuilderMesh.Create(positions, faces);
        roof.name = "Roof_FrontEast_V02";
        Undo.RegisterCreatedObjectUndo(roof.gameObject, "Criar telhado V02");
        roof.transform.SetParent(parent, false);
        roof.transform.localPosition = Vector3.zero;
        roof.transform.localRotation = Quaternion.identity;
        roof.transform.localScale = Vector3.one;
        var sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
            roof.GetComponent<MeshRenderer>().sharedMaterial = sourceRenderer.sharedMaterial;
        roof.ToMesh();
        roof.Refresh();

        foreach (Transform child in parent)
        {
            if (child.name == "Roof_Master_Front_Work" || child.name == "Roof_Masters_Front_Work")
            {
                Undo.RecordObject(child.gameObject, "Preservar tentativa anterior");
                child.gameObject.SetActive(false);
            }
        }
        Undo.RecordObject(source, "Manter teto interno ativo");
        source.SetActive(true);
        EditorSceneManager.MarkSceneDirty(source.scene);
        Selection.activeGameObject = roof.gameObject;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        Undo.CollapseUndoOperations(group);
        Debug.Log("Roof_FrontEast_V02 criado. Teto interno ativo; tentativa anterior preservada desativada. Confira a silhueta antes de salvar.", roof);
    }

    private static void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Transform parent,
        List<Vector3> vertices, List<Face> faces)
    {
        int first = vertices.Count;
        vertices.Add(parent.InverseTransformPoint(a));
        vertices.Add(parent.InverseTransformPoint(b));
        vertices.Add(parent.InverseTransformPoint(c));
        faces.Add(new Face(new[] { first, first + 1, first + 2 }));
    }
}
#endif
