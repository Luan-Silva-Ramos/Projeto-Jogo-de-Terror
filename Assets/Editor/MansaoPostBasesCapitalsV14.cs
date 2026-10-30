#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

// Importar em Assets/Editor. Bases e capiteis dos oito pilares frontais existentes.
public static class MansaoPostBasesCapitalsV14
{
    private struct Site
    {
        public string name;
        public Vector3 position;
        public Site(string name,float x,float z) { this.name=name; position=new Vector3(x,5.45f,z); }
    }
    private class Edge
    {
        public Vector2 a,b;
        public int count;
        public Edge(Vector2 a,Vector2 b) { this.a=a; this.b=b; count=1; }
    }
    private static readonly Site[] Sites={
        new Site("Office_OuterCorner",10.52f,-6.62f),
        new Site("Office_FrontCorner",8.62f,-8.52f),
        new Site("Office_InnerFront",5.78f,-8.52f),
        new Site("Office_EntryReturn",3.88f,-6.62f),
        new Site("Living_OuterCorner",-10.52f,-6.62f),
        new Site("Living_FrontCorner",-8.62f,-8.52f),
        new Site("Living_InnerFront",-5.78f,-8.52f),
        new Site("Living_EntryReturn",-3.88f,-6.62f),
    };

    [MenuItem("Tools/Mansao/Criar bases e capiteis frontais V14")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Show("Saia do Play Mode primeiro."); return; }
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("GroundFloor_V2/Exterior_Timber_Bands");
        if(parent==null) { Show("Selecione um objeto da mansao. Exterior_Timber_Bands nao foi encontrado."); return; }
        if(parent.Find("Post_BasesCapitals_V14")!=null) { Show("A V14 ja existe. Nenhuma copia foi criada."); return; }
        if(Vector3.Distance(parent.lossyScale,Vector3.one)>.001f)
        { Show("O organizador tem escala mundial diferente de 1. Nada foi alterado."); return; }

        var posts=new Dictionary<string,Transform>();
        Transform east=null,west=null;
        foreach(Transform t in mansion.GetComponentsInChildren<Transform>(true))
        {
            if(!t.gameObject.activeInHierarchy) continue;
            if(t.name=="Ceiling_Upper_MasterRoom") east=t;
            if(t.name=="Ceiling_Upper_GuestRoom") west=t;
            foreach(Site s in Sites) if(t.name=="Timber_Post_"+s.name+"_Upper") posts[s.name]=t;
        }
        foreach(Site s in Sites)
        {
            Transform t;
            if(!posts.TryGetValue(s.name,out t)) { Show("Nao encontrei o pilar "+s.name+". Nada foi alterado."); return; }
            Renderer r=t.GetComponent<Renderer>();
            if(r==null || Vector3.Distance(r.bounds.center,s.position)>.025f ||
                Mathf.Abs(r.bounds.size.y-3.5f)>.025f ||
                Vector3.Distance(t.lossyScale,Vector3.one)>.001f ||
                Vector3.Dot(t.up,Vector3.up)<.9999f)
            { Show("O pilar "+s.name+" difere das medidas conferidas. Nada foi alterado."); return; }
        }
        List<Vector2> eastBoundary=ReadBoundary(east),westBoundary=ReadBoundary(west);
        if(eastBoundary==null || westBoundary==null)
        { Show("Nao consegui verificar o contorno real dos tetos internos. Nada foi alterado."); return; }
        foreach(Site s in Sites)
        {
            Transform post=posts[s.name];
            Vector3 center=post.GetComponent<Renderer>().bounds.center;
            if(!CheckPiece(Base,Matrix4x4.TRS(new Vector3(center.x,3.71f,center.z),post.rotation,Vector3.one),eastBoundary,westBoundary) ||
               !CheckPiece(Capital,Matrix4x4.TRS(new Vector3(center.x,6.88f,center.z),post.rotation,Vector3.one),eastBoundary,westBoundary))
            { Show("Uma peca ultrapassaria a margem prevista na parede do pilar "+s.name+". Nada foi alterado."); return; }
        }
        Material wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Wood_Exterior_PBR.mat");
        if(wood==null) { Show("Nao encontrei o material existente de madeira. Nada foi alterado."); return; }

        Undo.IncrementCurrentGroup();
        int group=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Criar bases e capiteis V14");
        try
        {
            var root=new GameObject("Post_BasesCapitals_V14");
            SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);
            root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar acabamentos de pilares V14");
            Transform bases=Organizer("PostBases_Front_V14",root.transform);
            Transform capitals=Organizer("PostCapitals_Front_V14",root.transform);
            foreach(Site s in Sites)
            {
                Transform post=posts[s.name];
                Vector3 center=post.GetComponent<Renderer>().bounds.center;
                Build("PostBase_"+s.name+"_V14",Base,new Vector3(center.x,3.71f,center.z),post.rotation,bases,wood);
                Build("PostCapital_"+s.name+"_V14",Capital,new Vector3(center.x,6.88f,center.z),post.rotation,capitals,wood);
            }
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            Selection.activeGameObject=root;
            Undo.CollapseUndoOperations(group);
            Debug.Log("V14 criada: oito bases e oito capiteis acompanhando a rotacao real dos pilares. Pilares, blocos, faixas, suportes e conjuntos anteriores preservados ativos. Recuo na espessura das paredes verificado por amostragem.",root);
        }
        catch(System.Exception error)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(error);
            Show("A montagem foi desfeita devido a um erro. Consulte a Console.");
        }
    }

    private static string Key(Vector2 p)
    { return Mathf.RoundToInt(p.x*10000f)+","+Mathf.RoundToInt(p.y*10000f); }

    private static List<Vector2> ReadBoundary(Transform t)
    {
        if(t==null) return null;
        ProBuilderMesh mesh=t.GetComponent<ProBuilderMesh>();
        Renderer renderer=t.GetComponent<Renderer>();
        if(mesh==null || renderer==null || Mathf.Abs(renderer.bounds.min.y-7.2f)>.015f) return null;
        float y=renderer.bounds.min.y;
        var edges=new Dictionary<string,Edge>();
        foreach(Face face in mesh.faces)
        {
            var ids=face.indexes;
            for(int i=0;i<ids.Count;i+=3)
            {
                Vector3 a=t.TransformPoint(mesh.positions[ids[i]]),b=t.TransformPoint(mesh.positions[ids[i+1]]),
                    c=t.TransformPoint(mesh.positions[ids[i+2]]);
                if(Mathf.Abs(a.y-y)>.005f || Mathf.Abs(b.y-y)>.005f || Mathf.Abs(c.y-y)>.005f) continue;
                Vector2[] p={new Vector2(a.x,a.z),new Vector2(b.x,b.z),new Vector2(c.x,c.z)};
                if(Mathf.Abs(Cross(p[0],p[1],p[2]))<.000001f) continue;
                for(int j=0;j<3;j++)
                {
                    Vector2 u=p[j],v=p[(j+1)%3];
                    string ku=Key(u),kv=Key(v);
                    string key=string.CompareOrdinal(ku,kv)<0?ku+"|"+kv:kv+"|"+ku;
                    Edge edge;
                    if(edges.TryGetValue(key,out edge)) edge.count++;
                    else edges.Add(key,new Edge(u,v));
                }
            }
        }
        var boundary=new List<Edge>();
        foreach(Edge e in edges.Values) if(e.count==1) boundary.Add(e);
        if(boundary.Count<3) return null;
        var result=new List<Vector2>{boundary[0].a,boundary[0].b};
        boundary.RemoveAt(0);
        while(boundary.Count>0)
        {
            Vector2 last=result[result.Count-1];
            int found=-1;
            Vector2 next=Vector2.zero;
            for(int i=0;i<boundary.Count;i++)
            {
                if(Vector2.Distance(boundary[i].a,last)<.001f) { found=i; next=boundary[i].b; break; }
                if(Vector2.Distance(boundary[i].b,last)<.001f) { found=i; next=boundary[i].a; break; }
            }
            if(found<0) return null;
            result.Add(next); boundary.RemoveAt(found);
        }
        if(Vector2.Distance(result[0],result[result.Count-1])>.001f) return null;
        result.RemoveAt(result.Count-1);
        return result;
    }

    private static float Cross(Vector2 a,Vector2 b,Vector2 p)
    { return (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x); }
    private static bool Inside(List<Vector2> poly,Vector2 p)
    {
        bool result=false;
        for(int i=0,j=poly.Count-1;i<poly.Count;j=i++)
        {
            Vector2 a=poly[i],b=poly[j];
            if((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) result=!result;
        }
        return result;
    }
    private static float DistanceToBoundary(List<Vector2> poly,Vector2 p)
    {
        float best=float.PositiveInfinity;
        for(int i=0;i<poly.Count;i++)
        {
            Vector2 a=poly[i],b=poly[(i+1)%poly.Count],d=b-a;
            float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
            best=Mathf.Min(best,Vector2.Distance(p,a+d*t));
        }
        return best;
    }
    private static bool CheckPiece(float[] xyz,Matrix4x4 matrix,List<Vector2> east,List<Vector2> west)
    {
        for(int i=0;i<xyz.Length;i+=9)
        {
            Vector3 a=matrix.MultiplyPoint3x4(new Vector3(xyz[i],xyz[i+1],xyz[i+2]));
            Vector3 b=matrix.MultiplyPoint3x4(new Vector3(xyz[i+3],xyz[i+4],xyz[i+5]));
            Vector3 c=matrix.MultiplyPoint3x4(new Vector3(xyz[i+6],xyz[i+7],xyz[i+8]));
            for(int u=0;u<=4;u++) for(int v=0;v<=4-u;v++)
            {
                Vector3 p=a*(1f-(u+v)/4f)+b*(u/4f)+c*(v/4f);
                Vector2 q=new Vector2(p.x,p.z);
                // Parede de 30 cm; conserva pelo menos 3 cm ate a face interna.
                if((Inside(east,q) && DistanceToBoundary(east,q)>.27f) ||
                   (Inside(west,q) && DistanceToBoundary(west,q)>.27f)) return false;
            }
        }
        return true;
    }
    private static Transform Organizer(string name,Transform parent)
    {
        var go=new GameObject(name);
        SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);
        go.transform.SetParent(parent,false);
        Undo.RegisterCreatedObjectUndo(go,"Criar organizador V14");
        return go.transform;
    }
    private static void Build(string name,float[] xyz,Vector3 anchor,Quaternion rotation,Transform parent,Material material)
    {
        var positions=new List<Vector3>();
        var faces=new List<Face>();
        for(int i=0;i<xyz.Length;i+=9)
        {
            int first=positions.Count;
            for(int j=0;j<9;j+=3) positions.Add(new Vector3(xyz[i+j],xyz[i+j+1],xyz[i+j+2]));
            faces.Add(new Face(new[]{first,first+1,first+2}));
        }
        ProBuilderMesh mesh=ProBuilderMesh.Create(positions,faces);
        SceneManager.MoveGameObjectToScene(mesh.gameObject,parent.gameObject.scene);
        mesh.name=name;
        mesh.transform.SetParent(parent,false);
        mesh.transform.position=anchor;
        mesh.transform.rotation=rotation;
        mesh.transform.localScale=Vector3.one;
        Undo.RegisterCreatedObjectUndo(mesh.gameObject,"Criar acabamento de pilar V14");
        mesh.GetComponent<MeshRenderer>().sharedMaterial=material;
        mesh.ToMesh();
        mesh.Refresh();
    }
    private static void Show(string message)
    { EditorUtility.DisplayDialog("Bases e capiteis V14",message,"OK"); }

    private static readonly float[] Base = {
        -0.2f, 0.0f, -0.2f, 0.2f, 0.06f, -0.2f, 0.2f, 0.0f, -0.2f,
        -0.2f, 0.0f, -0.2f, -0.2f, 0.06f, -0.2f, 0.2f, 0.06f, -0.2f,
        0.2f, 0.0f, -0.2f, 0.2f, 0.06f, 0.2f, 0.2f, 0.0f, 0.2f,
        0.2f, 0.0f, -0.2f, 0.2f, 0.06f, -0.2f, 0.2f, 0.06f, 0.2f,
        0.2f, 0.0f, 0.2f, -0.2f, 0.06f, 0.2f, -0.2f, 0.0f, 0.2f,
        0.2f, 0.0f, 0.2f, 0.2f, 0.06f, 0.2f, -0.2f, 0.06f, 0.2f,
        -0.2f, 0.0f, 0.2f, -0.2f, 0.06f, -0.2f, -0.2f, 0.0f, -0.2f,
        -0.2f, 0.0f, 0.2f, -0.2f, 0.06f, 0.2f, -0.2f, 0.06f, -0.2f,
        -0.2f, 0.06f, -0.2f, 0.175f, 0.06f, -0.175f, 0.2f, 0.06f, -0.2f,
        -0.2f, 0.06f, -0.2f, -0.175f, 0.06f, -0.175f, 0.175f, 0.06f, -0.175f,
        0.2f, 0.06f, -0.2f, 0.175f, 0.06f, 0.175f, 0.2f, 0.06f, 0.2f,
        0.2f, 0.06f, -0.2f, 0.175f, 0.06f, -0.175f, 0.175f, 0.06f, 0.175f,
        0.2f, 0.06f, 0.2f, -0.175f, 0.06f, 0.175f, -0.2f, 0.06f, 0.2f,
        0.2f, 0.06f, 0.2f, 0.175f, 0.06f, 0.175f, -0.175f, 0.06f, 0.175f,
        -0.2f, 0.06f, 0.2f, -0.175f, 0.06f, -0.175f, -0.2f, 0.06f, -0.2f,
        -0.2f, 0.06f, 0.2f, -0.175f, 0.06f, 0.175f, -0.175f, 0.06f, -0.175f,
        -0.175f, 0.06f, -0.175f, 0.15f, 0.15f, -0.15f, 0.175f, 0.06f, -0.175f,
        -0.175f, 0.06f, -0.175f, -0.15f, 0.15f, -0.15f, 0.15f, 0.15f, -0.15f,
        0.175f, 0.06f, -0.175f, 0.15f, 0.15f, 0.15f, 0.175f, 0.06f, 0.175f,
        0.175f, 0.06f, -0.175f, 0.15f, 0.15f, -0.15f, 0.15f, 0.15f, 0.15f,
        0.175f, 0.06f, 0.175f, -0.15f, 0.15f, 0.15f, -0.175f, 0.06f, 0.175f,
        0.175f, 0.06f, 0.175f, 0.15f, 0.15f, 0.15f, -0.15f, 0.15f, 0.15f,
        -0.175f, 0.06f, 0.175f, -0.15f, 0.15f, -0.15f, -0.175f, 0.06f, -0.175f,
        -0.175f, 0.06f, 0.175f, -0.15f, 0.15f, 0.15f, -0.15f, 0.15f, -0.15f,
        -0.15f, 0.15f, -0.15f, 0.15f, 0.25f, -0.15f, 0.15f, 0.15f, -0.15f,
        -0.15f, 0.15f, -0.15f, -0.15f, 0.25f, -0.15f, 0.15f, 0.25f, -0.15f,
        0.15f, 0.15f, -0.15f, 0.15f, 0.25f, 0.15f, 0.15f, 0.15f, 0.15f,
        0.15f, 0.15f, -0.15f, 0.15f, 0.25f, -0.15f, 0.15f, 0.25f, 0.15f,
        0.15f, 0.15f, 0.15f, -0.15f, 0.25f, 0.15f, -0.15f, 0.15f, 0.15f,
        0.15f, 0.15f, 0.15f, 0.15f, 0.25f, 0.15f, -0.15f, 0.25f, 0.15f,
        -0.15f, 0.15f, 0.15f, -0.15f, 0.25f, -0.15f, -0.15f, 0.15f, -0.15f,
        -0.15f, 0.15f, 0.15f, -0.15f, 0.25f, 0.15f, -0.15f, 0.25f, -0.15f,
        -0.15f, 0.25f, -0.15f, 0.135f, 0.28f, -0.135f, 0.15f, 0.25f, -0.15f,
        -0.15f, 0.25f, -0.15f, -0.135f, 0.28f, -0.135f, 0.135f, 0.28f, -0.135f,
        0.15f, 0.25f, -0.15f, 0.135f, 0.28f, 0.135f, 0.15f, 0.25f, 0.15f,
        0.15f, 0.25f, -0.15f, 0.135f, 0.28f, -0.135f, 0.135f, 0.28f, 0.135f,
        0.15f, 0.25f, 0.15f, -0.135f, 0.28f, 0.135f, -0.15f, 0.25f, 0.15f,
        0.15f, 0.25f, 0.15f, 0.135f, 0.28f, 0.135f, -0.135f, 0.28f, 0.135f,
        -0.15f, 0.25f, 0.15f, -0.135f, 0.28f, -0.135f, -0.15f, 0.25f, -0.15f,
        -0.15f, 0.25f, 0.15f, -0.135f, 0.28f, 0.135f, -0.135f, 0.28f, -0.135f,
        -0.2f, 0.0f, -0.2f, 0.2f, 0.0f, -0.2f, 0.2f, 0.0f, 0.2f,
        -0.2f, 0.0f, -0.2f, 0.2f, 0.0f, 0.2f, -0.2f, 0.0f, 0.2f,
        -0.135f, 0.28f, -0.135f, 0.135f, 0.28f, 0.135f, 0.135f, 0.28f, -0.135f,
        -0.135f, 0.28f, -0.135f, -0.135f, 0.28f, 0.135f, 0.135f, 0.28f, 0.135f,
    };
    private static readonly float[] Capital = {
        -0.135f, 0.0f, -0.135f, 0.135f, 0.06f, -0.135f, 0.135f, 0.0f, -0.135f,
        -0.135f, 0.0f, -0.135f, -0.135f, 0.06f, -0.135f, 0.135f, 0.06f, -0.135f,
        0.135f, 0.0f, -0.135f, 0.135f, 0.06f, 0.135f, 0.135f, 0.0f, 0.135f,
        0.135f, 0.0f, -0.135f, 0.135f, 0.06f, -0.135f, 0.135f, 0.06f, 0.135f,
        0.135f, 0.0f, 0.135f, -0.135f, 0.06f, 0.135f, -0.135f, 0.0f, 0.135f,
        0.135f, 0.0f, 0.135f, 0.135f, 0.06f, 0.135f, -0.135f, 0.06f, 0.135f,
        -0.135f, 0.0f, 0.135f, -0.135f, 0.06f, -0.135f, -0.135f, 0.0f, -0.135f,
        -0.135f, 0.0f, 0.135f, -0.135f, 0.06f, 0.135f, -0.135f, 0.06f, -0.135f,
        -0.135f, 0.06f, -0.135f, 0.15f, 0.06f, -0.15f, 0.135f, 0.06f, -0.135f,
        -0.135f, 0.06f, -0.135f, -0.15f, 0.06f, -0.15f, 0.15f, 0.06f, -0.15f,
        0.135f, 0.06f, -0.135f, 0.15f, 0.06f, 0.15f, 0.135f, 0.06f, 0.135f,
        0.135f, 0.06f, -0.135f, 0.15f, 0.06f, -0.15f, 0.15f, 0.06f, 0.15f,
        0.135f, 0.06f, 0.135f, -0.15f, 0.06f, 0.15f, -0.135f, 0.06f, 0.135f,
        0.135f, 0.06f, 0.135f, 0.15f, 0.06f, 0.15f, -0.15f, 0.06f, 0.15f,
        -0.135f, 0.06f, 0.135f, -0.15f, 0.06f, -0.15f, -0.135f, 0.06f, -0.135f,
        -0.135f, 0.06f, 0.135f, -0.15f, 0.06f, 0.15f, -0.15f, 0.06f, -0.15f,
        -0.15f, 0.06f, -0.15f, 0.2f, 0.2f, -0.2f, 0.15f, 0.06f, -0.15f,
        -0.15f, 0.06f, -0.15f, -0.2f, 0.2f, -0.2f, 0.2f, 0.2f, -0.2f,
        0.15f, 0.06f, -0.15f, 0.2f, 0.2f, 0.2f, 0.15f, 0.06f, 0.15f,
        0.15f, 0.06f, -0.15f, 0.2f, 0.2f, -0.2f, 0.2f, 0.2f, 0.2f,
        0.15f, 0.06f, 0.15f, -0.2f, 0.2f, 0.2f, -0.15f, 0.06f, 0.15f,
        0.15f, 0.06f, 0.15f, 0.2f, 0.2f, 0.2f, -0.2f, 0.2f, 0.2f,
        -0.15f, 0.06f, 0.15f, -0.2f, 0.2f, -0.2f, -0.15f, 0.06f, -0.15f,
        -0.15f, 0.06f, 0.15f, -0.2f, 0.2f, 0.2f, -0.2f, 0.2f, -0.2f,
        -0.2f, 0.2f, -0.2f, 0.22f, 0.2f, -0.22f, 0.2f, 0.2f, -0.2f,
        -0.2f, 0.2f, -0.2f, -0.22f, 0.2f, -0.22f, 0.22f, 0.2f, -0.22f,
        0.2f, 0.2f, -0.2f, 0.22f, 0.2f, 0.22f, 0.2f, 0.2f, 0.2f,
        0.2f, 0.2f, -0.2f, 0.22f, 0.2f, -0.22f, 0.22f, 0.2f, 0.22f,
        0.2f, 0.2f, 0.2f, -0.22f, 0.2f, 0.22f, -0.2f, 0.2f, 0.2f,
        0.2f, 0.2f, 0.2f, 0.22f, 0.2f, 0.22f, -0.22f, 0.2f, 0.22f,
        -0.2f, 0.2f, 0.2f, -0.22f, 0.2f, -0.22f, -0.2f, 0.2f, -0.2f,
        -0.2f, 0.2f, 0.2f, -0.22f, 0.2f, 0.22f, -0.22f, 0.2f, -0.22f,
        -0.22f, 0.2f, -0.22f, 0.22f, 0.28f, -0.22f, 0.22f, 0.2f, -0.22f,
        -0.22f, 0.2f, -0.22f, -0.22f, 0.28f, -0.22f, 0.22f, 0.28f, -0.22f,
        0.22f, 0.2f, -0.22f, 0.22f, 0.28f, 0.22f, 0.22f, 0.2f, 0.22f,
        0.22f, 0.2f, -0.22f, 0.22f, 0.28f, -0.22f, 0.22f, 0.28f, 0.22f,
        0.22f, 0.2f, 0.22f, -0.22f, 0.28f, 0.22f, -0.22f, 0.2f, 0.22f,
        0.22f, 0.2f, 0.22f, 0.22f, 0.28f, 0.22f, -0.22f, 0.28f, 0.22f,
        -0.22f, 0.2f, 0.22f, -0.22f, 0.28f, -0.22f, -0.22f, 0.2f, -0.22f,
        -0.22f, 0.2f, 0.22f, -0.22f, 0.28f, 0.22f, -0.22f, 0.28f, -0.22f,
        -0.22f, 0.28f, -0.22f, 0.2f, 0.28f, -0.2f, 0.22f, 0.28f, -0.22f,
        -0.22f, 0.28f, -0.22f, -0.2f, 0.28f, -0.2f, 0.2f, 0.28f, -0.2f,
        0.22f, 0.28f, -0.22f, 0.2f, 0.28f, 0.2f, 0.22f, 0.28f, 0.22f,
        0.22f, 0.28f, -0.22f, 0.2f, 0.28f, -0.2f, 0.2f, 0.28f, 0.2f,
        0.22f, 0.28f, 0.22f, -0.2f, 0.28f, 0.2f, -0.22f, 0.28f, 0.22f,
        0.22f, 0.28f, 0.22f, 0.2f, 0.28f, 0.2f, -0.2f, 0.28f, 0.2f,
        -0.22f, 0.28f, 0.22f, -0.2f, 0.28f, -0.2f, -0.22f, 0.28f, -0.22f,
        -0.22f, 0.28f, 0.22f, -0.2f, 0.28f, 0.2f, -0.2f, 0.28f, -0.2f,
        -0.2f, 0.28f, -0.2f, 0.2f, 0.34f, -0.2f, 0.2f, 0.28f, -0.2f,
        -0.2f, 0.28f, -0.2f, -0.2f, 0.34f, -0.2f, 0.2f, 0.34f, -0.2f,
        0.2f, 0.28f, -0.2f, 0.2f, 0.34f, 0.2f, 0.2f, 0.28f, 0.2f,
        0.2f, 0.28f, -0.2f, 0.2f, 0.34f, -0.2f, 0.2f, 0.34f, 0.2f,
        0.2f, 0.28f, 0.2f, -0.2f, 0.34f, 0.2f, -0.2f, 0.28f, 0.2f,
        0.2f, 0.28f, 0.2f, 0.2f, 0.34f, 0.2f, -0.2f, 0.34f, 0.2f,
        -0.2f, 0.28f, 0.2f, -0.2f, 0.34f, -0.2f, -0.2f, 0.28f, -0.2f,
        -0.2f, 0.28f, 0.2f, -0.2f, 0.34f, 0.2f, -0.2f, 0.34f, -0.2f,
        -0.135f, 0.0f, -0.135f, 0.135f, 0.0f, -0.135f, 0.135f, 0.0f, 0.135f,
        -0.135f, 0.0f, -0.135f, 0.135f, 0.0f, 0.135f, -0.135f, 0.0f, 0.135f,
        -0.2f, 0.34f, -0.2f, 0.2f, 0.34f, 0.2f, 0.2f, 0.34f, -0.2f,
        -0.2f, 0.34f, -0.2f, -0.2f, 0.34f, 0.2f, 0.2f, 0.34f, 0.2f,
    };
}
#endif

