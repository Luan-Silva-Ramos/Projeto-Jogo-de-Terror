#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

// Importar em Assets/Editor. Pilastras de canto como revestimento externo em duas faces.
public static class MansaoStonePilastersV16
{
    private struct Site
    {
        public string name;
        public Vector2 corner,prev,next;
        public bool shortPrev,shortNext;
        public Site(string name,float x,float z,float px,float pz,float nx,float nz,bool sp,bool sn)
        { this.name=name; corner=new Vector2(x,z); prev=new Vector2(px,pz).normalized; next=new Vector2(nx,nz).normalized; shortPrev=sp; shortNext=sn; }
        public Vector2 PrevNormal { get { return new Vector2(prev.y,-prev.x); } }
        public Vector2 NextNormal { get { return new Vector2(next.y,-next.x); } }
    }
    private class Data
    {
        public List<Vector3> vertices=new List<Vector3>();
        public List<Face> faces=new List<Face>();
        public void Tri(Vector3 a,Vector3 b,Vector3 c)
        {
            int first=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            faces.Add(new Face(new[]{first,first+1,first+2}));
        }
    }
    private class Set { public Site site; public Data basis,shaft,capital; }
    private static readonly Site[] Sites={
        new Site("Office_EntryReturn",3.9f,-6.61213f,0.0f,-1.0f,0.707107f,-0.707107f,true,false),
        new Site("Office_InnerFront",5.78787f,-8.5f,0.707107f,-0.707107f,1.0f,0.0f,false,false),
        new Site("Office_FrontCorner",8.61213f,-8.5f,1.0f,0.0f,0.707107f,0.707107f,false,false),
        new Site("Office_OuterCorner",10.5f,-6.61213f,0.707107f,0.707107f,0.0f,1.0f,false,false),
        new Site("Living_OuterCorner",-10.5f,-6.61213f,0.0f,-1.0f,0.707107f,-0.707107f,false,false),
        new Site("Living_FrontCorner",-8.61213f,-8.5f,0.707107f,-0.707107f,1.0f,0.0f,false,false),
        new Site("Living_InnerFront",-5.78787f,-8.5f,1.0f,0.0f,0.707107f,0.707107f,false,false),
        new Site("Living_EntryReturn",-3.9f,-6.61213f,0.707107f,0.707107f,0.0f,1.0f,false,true),
    };

    [MenuItem("Tools/Mansao/Criar pilastras de pedra frontais V16")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Show("Saia do Play Mode primeiro."); return; }
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform ground=mansion==null?null:mansion.Find("GroundFloor_V2");
        Transform parent=ground==null?null:ground.Find("Facade_Details");
        if(parent==null) { Show("Selecione um objeto da mansao. GroundFloor_V2/Facade_Details nao foi encontrado."); return; }
        if(parent.Find("Stone_CornerPilasters_V16")!=null) { Show("A V16 ja existe. Nenhuma copia foi criada."); return; }
        if(Vector3.Distance(parent.lossyScale,Vector3.one)>.001f)
        { Show("O organizador tem escala mundial diferente de 1. Nada foi alterado."); return; }
        Material stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Stone_Exterior.mat");
        if(stone==null) { Show("Nao encontrei M_Stone_Exterior na pasta de materiais. Nada foi alterado."); return; }

        ProBuilderMesh[] walls=ground.GetComponentsInChildren<ProBuilderMesh>(true);
        var sets=new List<Set>();
        foreach(Site site in Sites)
        {
            Vector3 prevPoint=new Vector3(site.corner.x-site.prev.x*.12f,1.75f,site.corner.y-site.prev.y*.12f);
            Vector3 nextPoint=new Vector3(site.corner.x+site.next.x*.12f,1.75f,site.corner.y+site.next.y*.12f);
            if(!CheckWall(prevPoint,site.PrevNormal,walls) || !CheckWall(nextPoint,site.NextNormal,walls))
            { Show("Nao consegui confirmar as duas faces externas do canto "+site.name+". Nada foi alterado."); return; }
            var set=new Set { site=site };
            set.basis=Loft(site,new float[,] {
                {-.01f,.42f,.24f,.18f},{.06f,.42f,.24f,.18f},{.12f,.38f,.22f,.14f},
                {.23f,.34f,.20f,.10f},{.26f,.32f,.18f,.09f} });
            set.shaft=new Data();
            for(int i=0;i<10;i++)
            {
                float y0=.26f+i*.292f,y1=.26f+(i+1)*.292f-.008f;
                float run=i%2==0?.30f:.34f,shortRun=i%2==0?.18f:.20f,projection=i%2==0?.09f:.11f;
                Data course=Loft(site,new float[,] { {y0,run,shortRun,projection},{y1,run,shortRun,projection} });
                Append(set.shaft,course);
            }
            set.capital=Loft(site,new float[,] {
                {3.18f,.35f,.21f,.12f},{3.24f,.35f,.21f,.12f},{3.34f,.38f,.26f,.16f},
                {3.38f,.40f,.30f,.20f},{3.46f,.40f,.30f,.20f} });
            if(!Audit(site,set.basis) || !Audit(site,set.shaft) || !Audit(site,set.capital))
            { Show("Uma peca ultrapassaria o encaixe externo previsto em "+site.name+". Nada foi alterado."); return; }
            sets.Add(set);
        }

        Undo.IncrementCurrentGroup();
        int undoGroup=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Criar pilastras de pedra V16");
        try
        {
            Transform root=Organizer("Stone_CornerPilasters_V16",parent);
            foreach(Set set in sets)
            {
                Transform corner=Organizer("Pilaster_"+set.site.name+"_V16",root);
                corner.position=new Vector3(set.site.corner.x,0,set.site.corner.y);
                corner.rotation=Quaternion.identity;
                corner.localScale=Vector3.one;
                Build("Stone_Base_V16",set.basis,corner,stone);
                Build("Stone_Courses_V16",set.shaft,corner,stone);
                Build("Stone_Capital_V16",set.capital,corner,stone);
            }
            EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
            Selection.activeGameObject=root.gameObject;
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("V16 criada: oito pilastras externas em duas faces, cada uma com base, dez fiadas e capitel. Retornos junto a entrada estreitados. Encaixe traseiro de 5 mm nas faces das paredes. Paredes, pisos, janelas, portas e conjuntos anteriores preservados.",root);
        }
        catch(System.Exception error)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(error);
            Show("A montagem foi desfeita devido a um erro. Consulte a Console.");
        }
    }

    private static bool CheckWall(Vector3 point,Vector2 expected,ProBuilderMesh[] meshes)
    {
        Vector3 expectedNormal=new Vector3(expected.x,0,expected.y);
        foreach(ProBuilderMesh mesh in meshes)
        {
            if(!mesh.gameObject.activeInHierarchy) continue;
            Renderer renderer=mesh.GetComponent<Renderer>();
            if(renderer==null || renderer.bounds.SqrDistance(point)>.0009f) continue;
            foreach(Face face in mesh.faces)
            {
                var ids=face.indexes;
                for(int i=0;i<ids.Count;i+=3)
                {
                    Vector3 a=mesh.transform.TransformPoint(mesh.positions[ids[i]]);
                    Vector3 b=mesh.transform.TransformPoint(mesh.positions[ids[i+1]]);
                    Vector3 c=mesh.transform.TransformPoint(mesh.positions[ids[i+2]]);
                    Vector3 normal=Vector3.Cross(b-a,c-a);
                    float length=normal.magnitude;
                    if(length<.000001f) continue;
                    normal/=length;
                    if(Mathf.Abs(Vector3.Dot(normal,expectedNormal))<.99f) continue;
                    float distance=Vector3.Dot(normal,point-a);
                    if(Mathf.Abs(distance)>.02f) continue;
                    Vector3 q=point-normal*distance,u=b-a,v=c-a,w=q-a;
                    float uu=Vector3.Dot(u,u),uv=Vector3.Dot(u,v),vv=Vector3.Dot(v,v);
                    float wu=Vector3.Dot(w,u),wv=Vector3.Dot(w,v),den=uu*vv-uv*uv;
                    if(Mathf.Abs(den)<.00000001f) continue;
                    float s=(vv*wu-uv*wv)/den,t=(uu*wv-uv*wu)/den;
                    if(s>=-.0001f && t>=-.0001f && s+t<=1.0001f) return true;
                }
            }
        }
        return false;
    }

    private static List<Vector2> Ring(Site site,float run,float shortRun,float projection)
    {
        Vector2 a=site.PrevNormal,b=site.NextNormal;
        Vector2 miter=(a+b)/(1f+Vector2.Dot(a,b));
        float prevRun=site.shortPrev?shortRun:run,nextRun=site.shortNext?shortRun:run;
        var p=new List<Vector2> {
            -site.prev*prevRun-a*.005f,-miter*.005f,site.next*nextRun-b*.005f,
            site.next*nextRun+b*projection,miter*projection,-site.prev*prevRun+a*projection
        };
        float area=0;
        for(int i=0;i<p.Count;i++) area+=p[i].x*p[(i+1)%p.Count].y-p[(i+1)%p.Count].x*p[i].y;
        if(area<0) p.Reverse();
        return p;
    }
    private static Data Loft(Site site,float[,] rows)
    {
        var rings=new List<Vector3[]>();
        for(int k=0;k<rows.GetLength(0);k++)
        {
            List<Vector2> p=Ring(site,rows[k,1],rows[k,2],rows[k,3]);
            var ring=new Vector3[p.Count];
            for(int i=0;i<p.Count;i++) ring[i]=new Vector3(p[i].x,rows[k,0],p[i].y);
            rings.Add(ring);
        }
        var result=new Data();
        for(int k=0;k<rings.Count-1;k++) for(int i=0;i<rings[k].Length;i++)
        {
            int j=(i+1)%rings[k].Length;
            Vector3 a=rings[k][i],b=rings[k][j],c=rings[k+1][j],d=rings[k+1][i];
            result.Tri(a,c,b); result.Tri(a,d,c);
        }
        List<int[]> bottom=Triangulate(rings[0]),top=Triangulate(rings[rings.Count-1]);
        foreach(int[] t in bottom) result.Tri(rings[0][t[0]],rings[0][t[1]],rings[0][t[2]]);
        Vector3[] last=rings[rings.Count-1];
        foreach(int[] t in top) result.Tri(last[t[2]],last[t[1]],last[t[0]]);
        return result;
    }
    private static float Cross(Vector3 a,Vector3 b,Vector3 c)
    { return (b.x-a.x)*(c.z-a.z)-(b.z-a.z)*(c.x-a.x); }
    private static List<int[]> Triangulate(Vector3[] p)
    {
        var ids=new List<int>(); for(int i=0;i<p.Length;i++) ids.Add(i);
        var result=new List<int[]>();
        while(ids.Count>3)
        {
            bool found=false;
            for(int k=0;k<ids.Count;k++)
            {
                int a=ids[(k+ids.Count-1)%ids.Count],b=ids[k],c=ids[(k+1)%ids.Count];
                if(Cross(p[a],p[b],p[c])<=.0000001f) continue;
                bool occupied=false;
                foreach(int i in ids)
                    if(i!=a && i!=b && i!=c && Cross(p[a],p[b],p[i])>=-.0000001f &&
                       Cross(p[b],p[c],p[i])>=-.0000001f && Cross(p[c],p[a],p[i])>=-.0000001f)
                    { occupied=true; break; }
                if(occupied) continue;
                result.Add(new[]{a,b,c}); ids.RemoveAt(k); found=true; break;
            }
            if(!found) throw new System.InvalidOperationException("Nao foi possivel fechar o perfil da pilastra.");
        }
        result.Add(ids.ToArray()); return result;
    }
    private static void Append(Data into,Data source)
    {
        for(int i=0;i<source.vertices.Count;i+=3)
            into.Tri(source.vertices[i],source.vertices[i+1],source.vertices[i+2]);
    }
    private static bool Audit(Site site,Data data)
    {
        Vector2 a=site.PrevNormal,b=site.NextNormal;
        for(int i=0;i<data.vertices.Count;i+=3)
        {
            Vector3 p=data.vertices[i],q=data.vertices[i+1],r=data.vertices[i+2];
            for(int u=0;u<=4;u++) for(int v=0;v<=4-u;v++)
            {
                Vector3 x=p*(1f-(u+v)/4f)+q*(u/4f)+r*(v/4f);
                Vector2 point=new Vector2(x.x,x.z);
                if(Vector2.Dot(point,a)<-.02f && Vector2.Dot(point,b)<-.02f) return false;
            }
        }
        return true;
    }

    private static Transform Organizer(string name,Transform parent)
    {
        var go=new GameObject(name);
        SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);
        go.transform.SetParent(parent,false);
        Undo.RegisterCreatedObjectUndo(go,"Criar organizador V16");
        return go.transform;
    }
    private static void Build(string name,Data data,Transform parent,Material stone)
    {
        ProBuilderMesh mesh=ProBuilderMesh.Create(data.vertices,data.faces);
        SceneManager.MoveGameObjectToScene(mesh.gameObject,parent.gameObject.scene);
        mesh.name=name; mesh.transform.SetParent(parent,false);
        mesh.transform.localPosition=Vector3.zero;
        mesh.transform.localRotation=Quaternion.identity;
        mesh.transform.localScale=Vector3.one;
        Undo.RegisterCreatedObjectUndo(mesh.gameObject,"Criar peca de pedra V16");
        mesh.GetComponent<MeshRenderer>().sharedMaterial=stone;
        mesh.ToMesh(); mesh.Refresh();
    }
    private static void Show(string message)
    { EditorUtility.DisplayDialog("Pilastras de pedra V16",message,"OK"); }
}
#endif

