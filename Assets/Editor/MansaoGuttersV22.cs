#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoGuttersV22
{
    private static readonly Vector2[] Upper={
        new Vector2(1.74f,-4.85f),new Vector2(3.65f,-4.85f),new Vector2(3.65f,-6.715683f),
        new Vector2(5.684317f,-8.75f),new Vector2(8.715683f,-8.75f),new Vector2(10.75f,-6.715683f),
        new Vector2(10.75f,-1.35f),new Vector2(12.45f,-1.35f),new Vector2(12.45f,10.55f),
        new Vector2(7.95f,10.55f),new Vector2(7.95f,8.55f),new Vector2(5.05f,8.55f),
        new Vector2(5.05f,11.95f),new Vector2(-10.45f,11.95f),new Vector2(-10.45f,1.45f),
        new Vector2(-10.75f,1.45f),new Vector2(-10.75f,-6.715683f),new Vector2(-8.715683f,-8.75f),
        new Vector2(-5.684317f,-8.75f),new Vector2(-3.65f,-6.715683f),new Vector2(-3.65f,-4.85f),new Vector2(-1.74f,-4.85f)
    };
    private class Data
    {
        public List<Vector3> positions=new List<Vector3>();public List<Face> faces=new List<Face>();
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {int i=positions.Count;positions.Add(a);positions.Add(b);positions.Add(c);positions.Add(d);faces.Add(new Face(new[]{i,i+1,i+2,i,i+2,i+3}));}
    }
    private static Vector3 P(float x,float y,float z) {return new Vector3(x,y,z);}
    private static Vector3[][] Pipes()
    {
        return new[]{
            new[]{P(12.59f,7.04f,10.69f),P(12.59f,6.88f,10.69f),P(12.30f,6.55f,10.40f),P(12.30f,.42f,10.40f),P(12.48f,.24f,10.58f)},
            new[]{P(5.19f,7.04f,12.09f),P(5.19f,6.88f,12.09f),P(4.90f,6.55f,11.80f),P(4.90f,.42f,11.80f),P(5.08f,.24f,11.98f)},
            new[]{P(-10.59f,7.04f,12.09f),P(-10.59f,6.88f,12.09f),P(-10.30f,6.55f,11.80f),P(-10.30f,4.48f,11.80f),P(-10.43f,4.17f,11.93f)},
            new[]{P(-12.64f,3.57f,12.44f),P(-12.64f,3.40f,12.44f),P(-12.40f,3.12f,12.20f),P(-12.40f,.42f,12.20f),P(-12.58f,.24f,12.38f)},
            new[]{P(-4.33f,3.57f,13.84f),P(-4.33f,3.40f,13.84f),P(-4.45f,3.12f,13.60f),P(-4.45f,.42f,13.60f),P(-4.45f,.24f,13.84f)}
        };
    }
    [MenuItem("Tools/Mansao/Criar calhas e condutores V22")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) {Tell("Saia do Play Mode.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null) {Tell("Selecione um objeto da mansao.");return;}
        if(parent.Find("Gutters_Downpipes_V22")!=null) {Tell("A V22 ja existe. Nada foi duplicado.");return;}
        string[] required={"Slate_Roofs_V20","Roof_Junction_Finish_V21","Cornices_Upper_V10","Roof_Lower_Finishes_V11"};
        foreach(string name in required) {Transform t=parent.Find(name);if(t==null || !t.gameObject.activeInHierarchy){Tell("Mantenha "+name+" ativo. Nada foi alterado.");return;}}
        Material metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Roof_Seam_V20.mat");
        if(metal==null) {Tell("Material M_Roof_Seam_V20 nao encontrado.");return;}
        // Guardas de posicionamento da cobertura ja aprovada.
        Transform roof=parent.Find("Slate_Roofs_V20/Roof_WholeHouse_Joined_V05_Slate_V20");
        Renderer roofRenderer=roof==null?null:roof.GetComponent<Renderer>();
        if(roofRenderer==null || Mathf.Abs(roofRenderer.bounds.min.x+10.75f)>.025f || Mathf.Abs(roofRenderer.bounds.max.x-12.45f)>.025f || Mathf.Abs(roofRenderer.bounds.min.z+8.75f)>.025f || Mathf.Abs(roofRenderer.bounds.max.z-11.95f)>.025f || Mathf.Abs(roofRenderer.bounds.max.y-10.2f)>.025f)
        {Tell("A cobertura nao esta nas medidas previstas. Nada foi alterado.");return;}
        Vector3[][] pipes=Pipes();
        foreach(Renderer r in mansion.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.gameObject.activeInHierarchy) continue;
            bool opening=false;
            for(Transform t=r.transform;t!=null && t!=mansion;t=t.parent)
                if(t.name.IndexOf("Window",System.StringComparison.OrdinalIgnoreCase)>=0 || t.name.IndexOf("Door",System.StringComparison.OrdinalIgnoreCase)>=0 || t.name.StartsWith("WinterGarden")) {opening=true;break;}
            if(!opening) continue;
            Bounds bounds=r.bounds;bounds.Expand(.12f);
            foreach(Vector3[] path in pipes) for(int i=0;i<path.Length-1;i++)
            {
                Vector3 delta=path[i+1]-path[i];
                if(bounds.Contains(path[i]) || bounds.Contains(path[i+1]) || bounds.IntersectRay(new Ray(path[i],delta.normalized),out float distance) && distance<=delta.magnitude)
                {Tell("Um condutor ficaria proximo de "+r.name+". Nada foi alterado; envie o nome indicado para rever o trajeto.");return;}
            }
        }
        Data gutters=new Data();Channel(gutters,Upper,7.14f);
        Channel(gutters,new[]{new Vector2(-7.35f,12.3f),new Vector2(-12.5f,12.3f),new Vector2(-12.5f,.98f)},3.67f);
        Channel(gutters,new[]{new Vector2(-4.15f,13.7f),new Vector2(-7.35f,13.7f)},3.67f);
        Data drains=new Data();foreach(Vector3[] path in pipes) Tube(drains,path,.045f);
        Data brackets=new Data();
        Vector3[] outward={new Vector3(1,0,1).normalized,new Vector3(1,0,1).normalized,new Vector3(-1,0,1).normalized,new Vector3(-1,0,1).normalized,Vector3.forward};
        for(int k=0;k<pipes.Length;k++)foreach(float y in new[]{1.2f,2.8f,4.9f,6.1f})
        {
            if(y<pipes[k][3].y+.15f || y>pipes[k][2].y-.15f)continue;
            Vector3 center=new Vector3(pipes[k][2].x,y,pipes[k][2].z);
            Tube(brackets,new[]{center-Vector3.up*.018f,center+Vector3.up*.018f},.050f);
            Tube(brackets,new[]{center-outward[k]*.13f,center-outward[k]*.025f},.012f);
        }
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Criar calhas e condutores V22");
        try
        {
            GameObject root=new GameObject("Gutters_Downpipes_V22");SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar drenagem visual");
            Build("Gutters_Upper_Lower_V22",gutters,root.transform,metal);
            Build("Downpipes_Rear_V22",drains,root.transform,metal);
            Build("Downpipe_Brackets_V22",brackets,root.transform,metal);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;
            Tell("Calhas e cinco condutores criados. Nenhum objeto anterior foi alterado. Confira os encontros e saidas antes de salvar. Ctrl+Z desfaz o lote. Montagem visual, sem simulacao de agua.");
        }
        catch(System.Exception ex){Undo.RevertAllDownToGroup(group);Debug.LogException(ex);Tell("O lote foi desfeito devido a um erro. Consulte a Console.");}
    }
    private static Vector2 Normal(Vector2 a,Vector2 b) {Vector2 d=(b-a).normalized;return new Vector2(d.y,-d.x);}
    private static Vector2 Miter(Vector2[] path,int i)
    {
        if(i==0)return Normal(path[0],path[1]);if(i==path.Length-1)return Normal(path[i-1],path[i]);
        Vector2 a=Normal(path[i-1],path[i]),b=Normal(path[i],path[i+1]);return (a+b)/(1+Vector2.Dot(a,b));
    }
    private static void Channel(Data data,Vector2[] path,float top)
    {
        // Perfil em U: 17.5 cm de largura, 15 cm de altura, chapa de 1.2 cm.
        Vector2[] profile={new Vector2(.045f,0),new Vector2(.045f,-.15f),new Vector2(.22f,-.15f),new Vector2(.22f,0),new Vector2(.208f,0),new Vector2(.208f,-.138f),new Vector2(.057f,-.138f),new Vector2(.057f,0)};
        for(int i=0;i<path.Length-1;i++)
        {
            var a=new Vector3[8];var b=new Vector3[8];Vector2 na=Miter(path,i),nb=Miter(path,i+1);
            for(int j=0;j<8;j++){Vector2 pa=path[i]+na*profile[j].x,pb=path[i+1]+nb*profile[j].x;a[j]=P(pa.x,top+profile[j].y,pa.y);b[j]=P(pb.x,top+profile[j].y,pb.y);}
            // Perfil percorre o contorno com a cavidade aberta para cima.
            for(int j=0;j<8;j++)data.Quad(a[j],a[(j+1)%8],b[(j+1)%8],b[j]);
            if(i==0) EndPlate(data,a,true);if(i==path.Length-2)EndPlate(data,b,false);
        }
    }
    private static void EndPlate(Data data,Vector3[] ring,bool reverse)
    {
        int[][] q={new[]{0,1,6,7},new[]{1,2,5,6},new[]{2,3,4,5}};
        foreach(int[] ids in q)if(reverse)data.Quad(ring[ids[3]],ring[ids[2]],ring[ids[1]],ring[ids[0]]);else data.Quad(ring[ids[0]],ring[ids[1]],ring[ids[2]],ring[ids[3]]);
    }
    private static void Tube(Data data,Vector3[] path,float radius)
    {
        const int sides=12;var rings=new List<Vector3[]>();
        for(int i=0;i<path.Length;i++)
        {
            Vector3 tangent=i==0?(path[1]-path[0]).normalized:i==path.Length-1?(path[i]-path[i-1]).normalized:((path[i]-path[i-1]).normalized+(path[i+1]-path[i]).normalized).normalized;
            Vector3 u=Vector3.Cross(tangent,Vector3.right).normalized;if(u.sqrMagnitude<.1f)u=Vector3.Cross(tangent,Vector3.forward).normalized;
            Vector3 v=Vector3.Cross(tangent,u).normalized;var ring=new Vector3[sides];
            for(int j=0;j<sides;j++){float angle=j*Mathf.PI*2/sides;ring[j]=path[i]+radius*(u*Mathf.Cos(angle)+v*Mathf.Sin(angle));}rings.Add(ring);
        }
        // Tubos abertos nas duas extremidades; sem tampa que bloqueie a saida visual.
        for(int i=0;i<rings.Count-1;i++)for(int j=0;j<sides;j++)data.Quad(rings[i][j],rings[i][(j+1)%sides],rings[i+1][(j+1)%sides],rings[i+1][j]);
    }
    private static void Build(string name,Data data,Transform parent,Material material)
    {
        var local=new List<Vector3>();foreach(Vector3 p in data.positions)local.Add(parent.InverseTransformPoint(p));
        ProBuilderMesh pb=ProBuilderMesh.Create(local,data.faces);SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);
        pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;
        pb.name=name;Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar peca de drenagem");
        pb.GetComponent<MeshRenderer>().sharedMaterial=material;pb.ToMesh();pb.Refresh();
        MeshCollider collider=pb.GetComponent<MeshCollider>();if(collider!=null)Undo.DestroyObjectImmediate(collider);
    }
    private static void Tell(string text){EditorUtility.DisplayDialog("Calhas e condutores V22",text,"OK");}
}
#endif
