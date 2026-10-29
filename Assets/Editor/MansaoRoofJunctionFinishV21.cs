#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoRoofJunctionFinishV21
{
    [MenuItem("Tools/Mansao/Revestir fechamentos entre telhados V21")]
    private static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Tell("Saia do Play Mode."); return; }
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null) { Tell("Selecione um objeto da mansao."); return; }
        if(parent.Find("Roof_Junction_Finish_V21")!=null) { Tell("A V21 ja existe. Nada foi duplicado."); return; }
        Transform sourceTransform=parent.Find("Roof_Corrections_V06/Roof_Junction_Closures_V06");
        ProBuilderMesh source=sourceTransform==null?null:sourceTransform.GetComponent<ProBuilderMesh>();
        Transform current=parent.Find("Slate_Roofs_V20");
        if(source==null || !source.gameObject.activeInHierarchy || current==null || !current.gameObject.activeInHierarchy)
        { Tell("Mantenha a cobertura V20 e os fechamentos V06 ativos. Nada foi alterado."); return; }
        Material slate=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Roof_Slate_V20.mat");
        Material metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Roof_Seam_V20.mat");
        Renderer original=source.GetComponent<Renderer>();
        if(slate==null || metal==null || original==null || slate.shader==null || slate.shader.name!="Mansao/Slate V20")
        { Tell("Nao encontrei os materiais V20 esperados. Nada foi alterado."); return; }
        if(original.bounds.min.y<7.19f)
        { Tell("Os fechamentos estao abaixo da altura prevista. Nada foi alterado."); return; }

        var positions=new List<Vector3>();var vertices=new List<Vertex>();var faces=new List<Face>();
        int clad=0;
        foreach(Face face in source.faces)
        {
            var ids=face.indexes;
            for(int i=0;i<ids.Count;i+=3)
            {
                Vector3[] points={source.transform.TransformPoint(source.positions[ids[i]]),
                    source.transform.TransformPoint(source.positions[ids[i+1]]),
                    source.transform.TransformPoint(source.positions[ids[i+2]])};
                Vector3 normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
                bool vertical=Mathf.Abs(normal.y)<.01f;
                Vector3 u=Vector3.Cross(Vector3.up,normal).normalized;
                int first=vertices.Count;
                foreach(Vector3 world in points)
                {
                    // Mesma geometria, sem espessura adicional e sem deslocamento.
                    Vector3 local=parent.InverseTransformPoint(world);
                    Vector2 uv=vertical?new Vector2(Vector3.Dot(world,u)/3f,(world.y-7.4f)/1.8f):new Vector2(world.x/3f,world.z/3f);
                    positions.Add(local);vertices.Add(new Vertex {position=local,uv0=uv});
                }
                Face next=new Face(new[]{first,first+1,first+2});
                next.manualUV=true;next.smoothingGroup=face.smoothingGroup;next.submeshIndex=vertical?0:1;
                faces.Add(next);if(vertical) clad++;
            }
        }
        if(clad==0) { Tell("Nao encontrei faces verticais para revestir. Nada foi alterado."); return; }
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Revestir fechamentos de telhado V21");
        try
        {
            GameObject root=new GameObject("Roof_Junction_Finish_V21");
            SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);
            root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar acabamento dos encontros");
            ProBuilderMesh pb=ProBuilderMesh.Create(vertices,faces,SharedVertex.GetSharedVerticesWithPositions(positions),null,new[]{slate,metal});
            SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);
            pb.transform.SetParent(root.transform,false);pb.transform.localPosition=Vector3.zero;
            pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;
            pb.name="Roof_Junction_Closures_Slate_V21";
            Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar fechamentos revestidos");
            pb.gameObject.layer=source.gameObject.layer;
            GameObjectUtility.SetStaticEditorFlags(pb.gameObject,GameObjectUtility.GetStaticEditorFlags(source.gameObject));
            Renderer renderer=pb.GetComponent<Renderer>();
            renderer.shadowCastingMode=original.shadowCastingMode;renderer.receiveShadows=original.receiveShadows;
            Bounds before=original.bounds,after=renderer.bounds;
            if(Vector3.Distance(before.min,after.min)>.001f || Vector3.Distance(before.max,after.max)>.001f)
                throw new System.InvalidOperationException("A copia nao preservou os limites originais.");
            MeshCollider oldCollider=source.GetComponent<MeshCollider>(),collider=pb.GetComponent<MeshCollider>();
            if(oldCollider!=null)
            {
                if(collider==null) collider=Undo.AddComponent<MeshCollider>(pb.gameObject);
                collider.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;collider.convex=oldCollider.convex;
                collider.isTrigger=oldCollider.isTrigger;collider.sharedMaterial=oldCollider.sharedMaterial;collider.enabled=oldCollider.enabled;
            }
            else if(collider!=null) Undo.DestroyObjectImmediate(collider);
            Undo.RecordObject(source.gameObject,"Preservar fechamentos de reboco como reserva");
            source.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;
            Tell("Fechamentos revestidos com ardosia nas faces verticais e metal escuro nas bordas. Geometria preservada; original V06 desativado como reserva. Tetos internos e frontao central preservados. Confira antes de salvar. Ctrl+Z desfaz o lote.");
        }
        catch(System.Exception ex)
        {
            Undo.RevertAllDownToGroup(group);Debug.LogException(ex);
            Tell("O lote foi desfeito devido a um erro. Consulte a Console.");
        }
    }
    private static void Tell(string text) { EditorUtility.DisplayDialog("Fechamentos de telhado V21",text,"OK"); }
}
#endif
