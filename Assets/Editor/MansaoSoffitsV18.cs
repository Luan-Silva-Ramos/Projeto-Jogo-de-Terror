#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

// Acabamento externo independente; nao modifica os tetos existentes.
public static class MansaoSoffitsV18
{
    private static readonly Vector2[] Path = {
        new Vector2(1.74f,-4.85f),new Vector2(3.65f,-4.85f),
        new Vector2(3.65f,-6.715683f),new Vector2(5.684317f,-8.75f),
        new Vector2(8.715683f,-8.75f),new Vector2(10.75f,-6.715683f),
        new Vector2(10.75f,-1.35f),new Vector2(12.45f,-1.35f),
        new Vector2(12.45f,10.55f),new Vector2(7.95f,10.55f),
        new Vector2(7.95f,8.55f),new Vector2(5.05f,8.55f),
        new Vector2(5.05f,11.95f),new Vector2(-10.45f,11.95f),
        new Vector2(-10.45f,1.45f),new Vector2(-10.75f,1.45f),
        new Vector2(-10.75f,-6.715683f),new Vector2(-8.715683f,-8.75f),
        new Vector2(-5.684317f,-8.75f),new Vector2(-3.65f,-6.715683f),
        new Vector2(-3.65f,-4.85f),new Vector2(-1.74f,-4.85f)
    };
    private static Vector2 Normal(Vector2 a,Vector2 b)
    { Vector2 d=(b-a).normalized; return new Vector2(d.y,-d.x); }
    private static Vector2 Offset(int i,float distance)
    {
        Vector2 n;
        if(i==0) n=Normal(Path[0],Path[1]);
        else if(i==Path.Length-1) n=Normal(Path[i-1],Path[i]);
        else {
            Vector2 a=Normal(Path[i-1],Path[i]),b=Normal(Path[i],Path[i+1]);
            n=(a+b)/(1+Vector2.Dot(a,b));
        }
        return Path[i]+n*distance;
    }
    [MenuItem("Tools/Mansao/Criar acabamento inferior dos beirais V18")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Tell("Saia do Play Mode."); return; }
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null) { Tell("Selecione um objeto da mansao."); return; }
        if(parent.Find("Soffits_Upper_V18")!=null) { Tell("A V18 ja existe. Nada foi duplicado."); return; }
        Transform cornice=parent.Find("Cornices_Upper_V10/Cornice_MainFascia_V10");
        Transform roof=parent.Find("Roof_Full_Assembly_V05/Roof_WholeHouse_Joined_V05");
        Renderer cr=cornice==null?null:cornice.GetComponent<Renderer>();
        Renderer rr=roof==null?null:roof.GetComponent<Renderer>();
        if(cr==null || rr==null || !cornice.gameObject.activeInHierarchy || !roof.gameObject.activeInHierarchy ||
            Mathf.Abs(cr.bounds.min.y-7.13f)>.01f || Mathf.Abs(cr.bounds.max.y-7.35f)>.01f ||
            Mathf.Abs(rr.bounds.max.y-10.2f)>.025f || Mathf.Abs(rr.bounds.min.x+10.75f)>.025f ||
            Mathf.Abs(rr.bounds.max.x-12.45f)>.025f || Mathf.Abs(rr.bounds.min.z+8.75f)>.025f ||
            Mathf.Abs(rr.bounds.max.z-11.95f)>.025f)
        { Tell("O telhado V05 e a cornija V10 precisam estar ativos nas medidas originais. Nada foi alterado."); return; }
        Material wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Wood_Exterior_PBR.mat");
        if(wood==null) { Tell("Material de madeira nao encontrado."); return; }
        // Conferir cada trecho contra a borda externa dos tetos ativos.
        var ceilings=new List<ProBuilderMesh>();
        foreach(ProBuilderMesh pb in mansion.GetComponentsInChildren<ProBuilderMesh>(true))
            if(pb.gameObject.activeInHierarchy && pb.name.StartsWith("Ceiling_Upper_")) ceilings.Add(pb);
        for(int i=0;i<Path.Length-1;i++)
        {
            Vector2 p=(Offset(i,-.25f)+Offset(i+1,-.25f))*.5f;
            if(!CeilingEdgeNear(new Vector3(p.x,7.2f,p.y),ceilings))
            { Tell("A borda do teto no trecho "+(i+1)+" difere do contorno previsto. Nada foi alterado."); return; }
        }
        Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Criar acabamento inferior dos beirais V18");
        try {
            GameObject root=new GameObject("Soffits_Upper_V18");
            SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);
            root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar acabamento dos beirais");
            for(int i=0;i<Path.Length-1;i++) Build(i,root.transform,wood);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            Undo.CollapseUndoOperations(group); Selection.activeGameObject=root;
            Tell("21 trechos de madeira criados. Tetos, cornijas e suportes preservados. Ctrl+Z desfaz o lote.");
        } catch(System.Exception error) {
            Undo.RevertAllDownToGroup(group); Debug.LogException(error);
            Tell("O lote foi desfeito devido a um erro. Consulte a Console.");
        }
    }
    private static bool CeilingEdgeNear(Vector3 point,List<ProBuilderMesh> ceilings)
    {
        foreach(ProBuilderMesh pb in ceilings) foreach(Face face in pb.faces)
        {
            var ids=face.indexes;
            for(int j=0;j<ids.Count;j+=3) {
                Vector3 a=pb.transform.TransformPoint(pb.positions[ids[j]]);
                Vector3 b=pb.transform.TransformPoint(pb.positions[ids[j+1]]);
                Vector3 c=pb.transform.TransformPoint(pb.positions[ids[j+2]]);
                // Somente triangulos de faces verticais na altura inferior do teto.
                Vector3 n=Vector3.Cross(b-a,c-a).normalized;
                if(Mathf.Abs(n.y)>.01f) continue;
                if(SegmentNear(point,a,b) || SegmentNear(point,b,c) || SegmentNear(point,c,a)) return true;
            }
        }
        return false;
    }
    private static bool SegmentNear(Vector3 p,Vector3 a,Vector3 b)
    {
        if(Mathf.Abs(a.y-7.2f)>.015f || Mathf.Abs(b.y-7.2f)>.015f) return false;
        Vector3 d=b-a; if(d.sqrMagnitude<.000001f) return false;
        float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);
        return Vector3.Distance(p,a+d*t)<.025f;
    }
    private static void Build(int i,Transform parent,Material wood)
    {
        Vector2[] ring={Offset(i,-.255f),Offset(i+1,-.255f),Offset(i+1,-.025f),Offset(i,-.025f)};
        float area=0; for(int j=0;j<4;j++) area+=ring[j].x*ring[(j+1)%4].y-ring[(j+1)%4].x*ring[j].y;
        if(area<0) System.Array.Reverse(ring);
        var world=new Vector3[8];
        for(int j=0;j<4;j++) { world[j]=new Vector3(ring[j].x,7.185f,ring[j].y); world[j+4]=new Vector3(ring[j].x,7.205f,ring[j].y); }
        var verts=new List<Vector3>(); var faces=new List<Face>();
        int[][] quads={new[]{0,1,2,3},new[]{7,6,5,4},new[]{0,4,5,1},new[]{1,5,6,2},new[]{2,6,7,3},new[]{3,7,4,0}};
        foreach(int[] q in quads) {
            int start=verts.Count; foreach(int index in q) verts.Add(parent.InverseTransformPoint(world[index]));
            faces.Add(new Face(new[]{start,start+1,start+2,start,start+2,start+3}));
        }
        ProBuilderMesh pb=ProBuilderMesh.Create(verts,faces);
        SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);
        pb.transform.SetParent(parent,false); pb.transform.localPosition=Vector3.zero;
        pb.transform.localRotation=Quaternion.identity; pb.transform.localScale=Vector3.one;
        pb.name="Soffit_Segment_"+(i+1).ToString("D2")+"_V18";
        Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar trecho de beiral");
        pb.GetComponent<MeshRenderer>().sharedMaterial=wood; pb.ToMesh(); pb.Refresh();
    }
    private static void Tell(string text) { EditorUtility.DisplayDialog("Acabamento dos beirais V18",text,"OK"); }
}
#endif
