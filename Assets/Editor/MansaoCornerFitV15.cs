#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

// Importar em Assets/Editor. Ajuste do contato entre pilares e acabamentos.
// Os ornamentos dos apices da V13 sao preservados.
public static class MansaoCornerFitV15
{
    private struct Site
    {
        public string name;
        public Vector3 center,normal;
        public Site(string name,float x,float z,float nx,float nz)
        { this.name=name; center=new Vector3(x,5.45f,z); normal=new Vector3(nx,0,nz).normalized; }
    }
    private class Data
    {
        public List<Vector3> vertices=new List<Vector3>();
        public List<Face> faces=new List<Face>();
        public void Tri(Vector3 a,Vector3 b,Vector3 c)
        {
            int first=vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            faces.Add(new Face(new[]{first,first+1,first+2}));
        }
    }
    private class Set
    {
        public Site site;
        public Transform post,joint;
        public Quaternion direction;
        public Data support,basis,capital;
    }
    private class Edge
    {
        public Vector2 a,b;
        public int count=1;
        public Edge(Vector2 a,Vector2 b) { this.a=a; this.b=b; }
    }
    private static readonly Site[] Sites={
        new Site("Office_OuterCorner",10.52f,-6.62f,0.92388f,-0.382683f),
        new Site("Office_FrontCorner",8.62f,-8.52f,0.382683f,-0.92388f),
        new Site("Office_InnerFront",5.78f,-8.52f,-0.382683f,-0.92388f),
        new Site("Office_EntryReturn",3.88f,-6.62f,-0.92388f,-0.382683f),
        new Site("Living_OuterCorner",-10.52f,-6.62f,-0.92388f,-0.382683f),
        new Site("Living_FrontCorner",-8.62f,-8.52f,-0.382683f,-0.92388f),
        new Site("Living_InnerFront",-5.78f,-8.52f,0.382683f,-0.92388f),
        new Site("Living_EntryReturn",-3.88f,-6.62f,0.92388f,-0.382683f),
    };

    [MenuItem("Tools/Mansao/Corrigir encaixes de pilares V15")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Show("Saia do Play Mode primeiro."); return; }
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("GroundFloor_V2/Exterior_Timber_Bands");
        Transform roofs=mansion==null?null:mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null || roofs==null) { Show("Selecione um objeto da mansao. Os organizadores nao foram encontrados."); return; }
        if(parent.Find("Corner_Fit_Corrections_V15")!=null) { Show("A V15 ja existe. Nenhuma copia foi criada."); return; }
        if(Vector3.Distance(parent.lossyScale,Vector3.one)>.001f) { Show("O organizador precisa ter escala mundial 1. Nada foi alterado."); return; }
        Transform oldSupports=roofs.Find("Roof_Ornaments_V13/Corbels_Front_V13");
        Transform oldFinishes=parent.Find("Post_BasesCapitals_V14");
        Transform cornices=roofs.Find("Cornices_Upper_V10");
        if(oldSupports==null || oldFinishes==null || cornices==null || !cornices.gameObject.activeInHierarchy)
        { Show("Preciso encontrar os suportes V13, os acabamentos V14 e as cornijas V10. Nada foi alterado."); return; }

        Transform east=FindActive(mansion,"Ceiling_Upper_MasterRoom");
        Transform west=FindActive(mansion,"Ceiling_Upper_GuestRoom");
        List<Vector2> eastBoundary=Boundary(east),westBoundary=Boundary(west);
        if(eastBoundary==null || westBoundary==null)
        { Show("Nao consegui verificar o contorno real das alas. Nada foi alterado."); return; }
        var sets=new List<Set>();
        foreach(Site site in Sites)
        {
            Transform post=FindActive(mansion,"Timber_Post_"+site.name+"_Upper");
            Transform joint=FindJoint(mansion,site.name);
            if(!CheckSource(post,site.center,.24f,3.5f) ||
                !CheckSource(joint,new Vector3(site.center.x,3.6f,site.center.z),.32f,.32f))
            { Show("O pilar ou bloco de "+site.name+" difere das medidas conferidas. Nada foi alterado."); return; }
            Quaternion direction=Quaternion.LookRotation(site.normal,Vector3.up);
            if(!MultipleOf45(Quaternion.Angle(post.rotation,direction)) ||
                !MultipleOf45(Quaternion.Angle(post.rotation,joint.rotation)))
            { Show("A rotacao de "+site.name+" foi alterada e precisa de nova confericao. Nada foi alterado."); return; }
            var set=new Set { site=site,post=post,joint=joint,direction=direction };
            Vector3 actualCenter=post.GetComponent<Renderer>().bounds.center;
            set.support=Support(post,direction);
            set.basis=Base(post,joint);
            set.capital=FromArray(CapitalTemplate);
            if(!Audit(set.support,Matrix4x4.TRS(new Vector3(actualCenter.x,6.6f,actualCenter.z),direction,Vector3.one),eastBoundary,westBoundary) ||
                !Audit(set.basis,Matrix4x4.TRS(new Vector3(actualCenter.x,3.73f,actualCenter.z),post.rotation,Vector3.one),eastBoundary,westBoundary) ||
                !Audit(set.capital,Matrix4x4.TRS(new Vector3(actualCenter.x,6.96f,actualCenter.z),post.rotation,Vector3.one),eastBoundary,westBoundary))
            { Show("Uma peca ultrapassaria a margem interna prevista em "+site.name+". Nada foi alterado."); return; }
            sets.Add(set);
        }
        Material wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Wood_Exterior_PBR.mat");
        if(wood==null) { Show("Nao encontrei o material existente de madeira. Nada foi alterado."); return; }

        Undo.IncrementCurrentGroup();
        int undoGroup=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Corrigir encaixes de pilares V15");
        try
        {
            Transform root=Organizer("Corner_Fit_Corrections_V15",parent);
            Transform supports=Organizer("Corbels_Fitted_V15",root);
            Transform bases=Organizer("Bases_Fitted_V15",root);
            Transform capitals=Organizer("Capitals_Fitted_V15",root);
            foreach(Set set in sets)
            {
                Vector3 c=set.post.GetComponent<Renderer>().bounds.center;
                Build("Corbel_"+set.site.name+"_Fitted_V15",set.support,new Vector3(c.x,6.6f,c.z),set.direction,supports,wood);
                Build("PostBase_"+set.site.name+"_Fitted_V15",set.basis,new Vector3(c.x,3.73f,c.z),set.post.rotation,bases,wood);
                Build("PostCapital_"+set.site.name+"_Fitted_V15",set.capital,new Vector3(c.x,6.96f,c.z),set.post.rotation,capitals,wood);
            }
            Deactivate(oldSupports.gameObject);
            Deactivate(oldFinishes.gameObject);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            Selection.activeGameObject=root.gameObject;
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("V15 criada: suportes ajustados as faces/quinas reais, capiteis menores e bases ligando a rotacao do bloco a do pilar. Somente Corbels_Front_V13 e Post_BasesCapitals_V14 foram desativados como reserva. Ornamentos dos apices, pilares, blocos e cornijas preservados.",root);
        }
        catch(System.Exception error)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(error);
            Show("A correcao foi desfeita devido a um erro. Consulte a Console.");
        }
    }

    private static Transform FindActive(Transform root,string name)
    {
        Transform found=null;
        foreach(Transform t in root.GetComponentsInChildren<Transform>(true))
            if(t.name==name && t.gameObject.activeInHierarchy) { if(found!=null) return null; found=t; }
        return found;
    }
    private static Transform FindJoint(Transform root,string site)
    {
        Transform found=null;
        foreach(Transform t in root.GetComponentsInChildren<Transform>(true))
            if(t.gameObject.activeInHierarchy && (t.name=="Timber_Joint_"+site || t.name=="Timber_Joint_"+site+"_Upper"))
            { if(found!=null) return null; found=t; }
        return found;
    }
    private static bool MultipleOf45(float angle)
    { return Mathf.Abs(angle-Mathf.Round(angle/45f)*45f)<.02f; }
    private static bool CheckSource(Transform t,Vector3 center,float width,float height)
    {
        if(t==null || Vector3.Distance(t.lossyScale,Vector3.one)>.001f || Vector3.Dot(t.up,Vector3.up)<.9999f) return false;
        Renderer r=t.GetComponent<Renderer>();
        ProBuilderMesh mesh=t.GetComponent<ProBuilderMesh>();
        if(r==null || mesh==null || Vector3.Distance(r.bounds.center,center)>.015f ||
            Mathf.Abs(r.bounds.size.y-height)>.015f) return false;
        Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        Vector3 max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
        foreach(Vector3 p in mesh.positions) { min=Vector3.Min(min,p); max=Vector3.Max(max,p); }
        return Mathf.Abs(max.x-min.x-width)<.01f && Mathf.Abs(max.z-min.z-width)<.01f;
    }

    // Intersecao do raio transversal do suporte com a secao quadrada real do pilar.
    private static float Front(float x,Transform post,Quaternion direction)
    {
        Vector3 tangent=Quaternion.Inverse(post.rotation)*(direction*Vector3.right);
        Vector3 normal=Quaternion.Inverse(post.rotation)*(direction*Vector3.forward);
        float lower=float.NegativeInfinity,upper=float.PositiveInfinity;
        float[] offsets={tangent.x*x,tangent.z*x},rays={normal.x,normal.z};
        for(int i=0;i<2;i++)
        {
            if(Mathf.Abs(rays[i])<.000001f)
            { if(Mathf.Abs(offsets[i])>.12001f) return float.NaN; continue; }
            float a=(-.12f-offsets[i])/rays[i],b=(.12f-offsets[i])/rays[i];
            lower=Mathf.Max(lower,Mathf.Min(a,b));
            upper=Mathf.Min(upper,Mathf.Max(a,b));
        }
        return upper>=lower?upper:float.NaN;
    }
    private static Vector3 Fit(Vector3 p,int part,Transform post,Quaternion direction)
    {
        float rear=Front(p.x,post,direction)-.005f;
        float back=part==0?.07f:part==1?.04f:.055f;
        float front=part==0?.32f:part==1?.34f:.14f;
        float tip=part==0?.30f:part==1?.31f:rear+.045f;
        return new Vector3(p.x,p.y,rear+(p.z-back)/(front-back)*(tip-rear));
    }
    private static List<Vector3> ClipX(List<Vector3> p,bool positive)
    {
        var result=new List<Vector3>();
        for(int i=0;i<p.Count;i++)
        {
            Vector3 a=p[i],b=p[(i+1)%p.Count];
            float da=a.x*(positive?1:-1),db=b.x*(positive?1:-1);
            if(da>=-.0000001f) result.Add(a);
            if(da*db<-.0000000001f) result.Add(Vector3.LerpUnclamped(a,b,da/(da-db)));
        }
        return result;
    }
    private static Data Support(Transform post,Quaternion direction)
    {
        var result=new Data();
        int bodyEnd=SupportTemplate.Length-24*9;
        for(int i=0;i<SupportTemplate.Length;i+=9)
        {
            int part=i<bodyEnd?0:i<bodyEnd+12*9?1:2;
            var p=new List<Vector3>();
            for(int j=0;j<9;j+=3) p.Add(new Vector3(SupportTemplate[i+j],SupportTemplate[i+j+1],SupportTemplate[i+j+2]));
            float min=Mathf.Min(p[0].x,Mathf.Min(p[1].x,p[2].x)),max=Mathf.Max(p[0].x,Mathf.Max(p[1].x,p[2].x));
            var polys=new List<List<Vector3>>();
            if(min<-.0000001f && max>.0000001f) { polys.Add(ClipX(p,true)); polys.Add(ClipX(p,false)); }
            else polys.Add(p);
            foreach(List<Vector3> poly in polys)
                for(int k=1;k<poly.Count-1;k++)
                    result.Tri(Fit(poly[0],part,post,direction),Fit(poly[k],part,post,direction),Fit(poly[k+1],part,post,direction));
        }
        return result;
    }

    private static Data Base(Transform post,Transform joint)
    {
        // y, largura do bloco, largura do pilar, mistura entre as duas orientacoes.
        float[,] rows={ {0,.34f,.34f,0},{.06f,.34f,.34f,0},{.06f,.31f,.31f,0},
            {.15f,.31f,.29f,.5f},{.20f,.29f,.29f,1},{.23f,.27f,.27f,1} };
        var rings=new List<Vector3[]>();
        Vector3 jointOffset=Quaternion.Inverse(post.rotation)*
            (joint.GetComponent<Renderer>().bounds.center-post.GetComponent<Renderer>().bounds.center);
        for(int k=0;k<rows.GetLength(0);k++)
        {
            var ring=new Vector3[16];
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI/8f;
                Vector3 ray=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                Vector3 inJoint=Quaternion.Inverse(joint.rotation)*(post.rotation*ray);
                float jointRadius=rows[k,1]*.5f/Mathf.Max(Mathf.Abs(inJoint.x),Mathf.Abs(inJoint.z));
                float postRadius=rows[k,2]*.5f/Mathf.Max(Mathf.Abs(ray.x),Mathf.Abs(ray.z));
                float radius=Mathf.Lerp(jointRadius,postRadius,rows[k,3]);
                float followJoint=1f-rows[k,3];
                ring[i]=new Vector3(ray.x*radius+jointOffset.x*followJoint,rows[k,0],
                    ray.z*radius+jointOffset.z*followJoint);
            }
            rings.Add(ring);
        }
        var result=new Data();
        for(int k=0;k<rings.Count-1;k++) for(int i=0;i<16;i++)
        {
            int j=(i+1)%16;
            Vector3 a=rings[k][i],b=rings[k][j],c=rings[k+1][j],d=rings[k+1][i];
            result.Tri(a,c,b); result.Tri(a,d,c);
        }
        for(int i=0;i<16;i++)
        {
            int j=(i+1)%16;
            result.Tri(Vector3.zero,rings[0][i],rings[0][j]);
            result.Tri(new Vector3(0,.23f,0),rings[rings.Count-1][j],rings[rings.Count-1][i]);
        }
        return result;
    }
    private static Data FromArray(float[] xyz)
    {
        var result=new Data();
        for(int i=0;i<xyz.Length;i+=9)
            result.Tri(new Vector3(xyz[i],xyz[i+1],xyz[i+2]),new Vector3(xyz[i+3],xyz[i+4],xyz[i+5]),
                new Vector3(xyz[i+6],xyz[i+7],xyz[i+8]));
        return result;
    }

    private static string Key(Vector2 p)
    { return Mathf.RoundToInt(p.x*10000f)+","+Mathf.RoundToInt(p.y*10000f); }
    private static List<Vector2> Boundary(Transform t)
    {
        if(t==null) return null;
        ProBuilderMesh mesh=t.GetComponent<ProBuilderMesh>();
        Renderer r=t.GetComponent<Renderer>();
        if(mesh==null || r==null || Mathf.Abs(r.bounds.min.y-7.2f)>.015f) return null;
        var edges=new Dictionary<string,Edge>();
        foreach(Face face in mesh.faces)
        {
            var ids=face.indexes;
            for(int i=0;i<ids.Count;i+=3)
            {
                Vector3 a=t.TransformPoint(mesh.positions[ids[i]]),b=t.TransformPoint(mesh.positions[ids[i+1]]),
                    c=t.TransformPoint(mesh.positions[ids[i+2]]);
                if(Mathf.Abs(a.y-7.2f)>.005f || Mathf.Abs(b.y-7.2f)>.005f || Mathf.Abs(c.y-7.2f)>.005f) continue;
                Vector2[] p={new Vector2(a.x,a.z),new Vector2(b.x,b.z),new Vector2(c.x,c.z)};
                if(Mathf.Abs(Cross(p[0],p[1],p[2]))<.000001f) continue;
                for(int j=0;j<3;j++)
                {
                    Vector2 u=p[j],v=p[(j+1)%3]; string ku=Key(u),kv=Key(v);
                    string key=string.CompareOrdinal(ku,kv)<0?ku+"|"+kv:kv+"|"+ku;
                    Edge edge;
                    if(edges.TryGetValue(key,out edge)) edge.count++; else edges.Add(key,new Edge(u,v));
                }
            }
        }
        var boundary=new List<Edge>();
        foreach(Edge e in edges.Values) if(e.count==1) boundary.Add(e);
        if(boundary.Count<3) return null;
        var result=new List<Vector2>{boundary[0].a,boundary[0].b}; boundary.RemoveAt(0);
        while(boundary.Count>0)
        {
            Vector2 end=result[result.Count-1],next=Vector2.zero; int index=-1;
            for(int i=0;i<boundary.Count;i++)
            {
                if(Vector2.Distance(boundary[i].a,end)<.001f) { index=i; next=boundary[i].b; break; }
                if(Vector2.Distance(boundary[i].b,end)<.001f) { index=i; next=boundary[i].a; break; }
            }
            if(index<0) return null;
            result.Add(next); boundary.RemoveAt(index);
        }
        if(Vector2.Distance(result[0],result[result.Count-1])>.001f) return null;
        result.RemoveAt(result.Count-1); return result;
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
    private static float Distance(List<Vector2> poly,Vector2 p)
    {
        float best=float.PositiveInfinity;
        for(int i=0;i<poly.Count;i++)
        {
            Vector2 a=poly[i],b=poly[(i+1)%poly.Count],d=b-a;
            float u=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
            best=Mathf.Min(best,Vector2.Distance(p,a+d*u));
        }
        return best;
    }
    private static bool Audit(Data data,Matrix4x4 matrix,List<Vector2> east,List<Vector2> west)
    {
        foreach(Vector3 p in data.vertices)
            if(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
                float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z)) return false;
        for(int i=0;i<data.vertices.Count;i+=3)
        {
            Vector3 a=matrix.MultiplyPoint3x4(data.vertices[i]),b=matrix.MultiplyPoint3x4(data.vertices[i+1]),
                c=matrix.MultiplyPoint3x4(data.vertices[i+2]);
            for(int u=0;u<=4;u++) for(int v=0;v<=4-u;v++)
            {
                Vector3 p=a*(1f-(u+v)/4f)+b*(u/4f)+c*(v/4f); Vector2 q=new Vector2(p.x,p.z);
                if((Inside(east,q) && Distance(east,q)>.27f) || (Inside(west,q) && Distance(west,q)>.27f)) return false;
            }
        }
        return true;
    }

    private static Transform Organizer(string name,Transform parent)
    {
        var go=new GameObject(name);
        SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);
        go.transform.SetParent(parent,false);
        Undo.RegisterCreatedObjectUndo(go,"Criar organizador V15");
        return go.transform;
    }
    private static void Build(string name,Data data,Vector3 anchor,Quaternion rotation,Transform parent,Material wood)
    {
        ProBuilderMesh mesh=ProBuilderMesh.Create(data.vertices,data.faces);
        SceneManager.MoveGameObjectToScene(mesh.gameObject,parent.gameObject.scene);
        mesh.name=name;
        mesh.transform.SetParent(parent,false);
        mesh.transform.position=anchor;
        mesh.transform.rotation=rotation;
        mesh.transform.localScale=Vector3.one;
        Undo.RegisterCreatedObjectUndo(mesh.gameObject,"Criar acabamento ajustado V15");
        mesh.GetComponent<MeshRenderer>().sharedMaterial=wood;
        mesh.ToMesh(); mesh.Refresh();
    }
    private static void Deactivate(GameObject go)
    { Undo.RecordObject(go,"Preservar acabamento anterior"); go.SetActive(false); }
    private static void Show(string message)
    { EditorUtility.DisplayDialog("Encaixes de pilares V15",message,"OK"); }

    private static readonly float[] SupportTemplate = {
        -0.09f, 0.48f, 0.07f, -0.09f, 0.0f, 0.07f, -0.09f, 0.0f, 0.11f,
        0.09f, 0.0f, 0.11f, 0.09f, 0.0f, 0.07f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.0f, 0.11f, -0.09f, 0.03f, 0.11f,
        0.09f, 0.03f, 0.11f, 0.09f, 0.0f, 0.11f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.03f, 0.11f, -0.09f, 0.038524f, 0.119488f,
        0.09f, 0.038524f, 0.119488f, 0.09f, 0.03f, 0.11f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.038524f, 0.119488f, -0.09f, 0.049028f, 0.127986f,
        0.09f, 0.049028f, 0.127986f, 0.09f, 0.038524f, 0.119488f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.049028f, 0.127986f, -0.09f, 0.061406f, 0.135547f,
        0.09f, 0.061406f, 0.135547f, 0.09f, 0.049028f, 0.127986f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.061406f, 0.135547f, -0.09f, 0.075556f, 0.142222f,
        0.09f, 0.075556f, 0.142222f, 0.09f, 0.061406f, 0.135547f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.075556f, 0.142222f, -0.09f, 0.091372f, 0.148064f,
        0.09f, 0.091372f, 0.148064f, 0.09f, 0.075556f, 0.142222f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.091372f, 0.148064f, -0.09f, 0.10875f, 0.153125f,
        0.09f, 0.10875f, 0.153125f, 0.09f, 0.091372f, 0.148064f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.10875f, 0.153125f, -0.09f, 0.127587f, 0.157457f,
        0.09f, 0.127587f, 0.157457f, 0.09f, 0.10875f, 0.153125f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.127587f, 0.157457f, -0.09f, 0.147778f, 0.161111f,
        0.09f, 0.147778f, 0.161111f, 0.09f, 0.127587f, 0.157457f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.147778f, 0.161111f, -0.09f, 0.169219f, 0.164141f,
        0.09f, 0.169219f, 0.164141f, 0.09f, 0.147778f, 0.161111f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.169219f, 0.164141f, -0.09f, 0.191806f, 0.166597f,
        0.09f, 0.191806f, 0.166597f, 0.09f, 0.169219f, 0.164141f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.191806f, 0.166597f, -0.09f, 0.215434f, 0.168533f,
        0.09f, 0.215434f, 0.168533f, 0.09f, 0.191806f, 0.166597f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.215434f, 0.168533f, -0.09f, 0.24f, 0.17f,
        0.09f, 0.24f, 0.17f, 0.09f, 0.215434f, 0.168533f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.24f, 0.17f, -0.09f, 0.266649f, 0.173524f,
        0.09f, 0.266649f, 0.173524f, 0.09f, 0.24f, 0.17f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.266649f, 0.173524f, -0.09f, 0.291528f, 0.179028f,
        0.09f, 0.291528f, 0.179028f, 0.09f, 0.266649f, 0.173524f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.291528f, 0.179028f, -0.09f, 0.314531f, 0.186406f,
        0.09f, 0.314531f, 0.186406f, 0.09f, 0.291528f, 0.179028f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.314531f, 0.186406f, -0.09f, 0.335556f, 0.195556f,
        0.09f, 0.335556f, 0.195556f, 0.09f, 0.314531f, 0.186406f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.335556f, 0.195556f, -0.09f, 0.354497f, 0.206372f,
        0.09f, 0.354497f, 0.206372f, 0.09f, 0.335556f, 0.195556f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.354497f, 0.206372f, -0.09f, 0.37125f, 0.21875f,
        0.09f, 0.37125f, 0.21875f, 0.09f, 0.354497f, 0.206372f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.37125f, 0.21875f, -0.09f, 0.385712f, 0.232587f,
        0.09f, 0.385712f, 0.232587f, 0.09f, 0.37125f, 0.21875f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.385712f, 0.232587f, -0.09f, 0.397778f, 0.247778f,
        0.09f, 0.397778f, 0.247778f, 0.09f, 0.385712f, 0.232587f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.397778f, 0.247778f, -0.09f, 0.407344f, 0.264219f,
        0.09f, 0.407344f, 0.264219f, 0.09f, 0.397778f, 0.247778f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.407344f, 0.264219f, -0.09f, 0.414306f, 0.281806f,
        0.09f, 0.414306f, 0.281806f, 0.09f, 0.407344f, 0.264219f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.414306f, 0.281806f, -0.09f, 0.418559f, 0.300434f,
        0.09f, 0.418559f, 0.300434f, 0.09f, 0.414306f, 0.281806f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, -0.09f, 0.418559f, 0.300434f, -0.09f, 0.42f, 0.32f,
        0.09f, 0.42f, 0.32f, 0.09f, 0.418559f, 0.300434f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.42f, 0.32f, -0.09f, 0.48f, 0.32f, -0.09f, 0.48f, 0.07f,
        0.09f, 0.48f, 0.07f, 0.09f, 0.48f, 0.32f, 0.09f, 0.42f, 0.32f,
        -0.09f, 0.0f, 0.07f, 0.09f, 0.0f, 0.07f, 0.09f, 0.0f, 0.11f,
        -0.09f, 0.0f, 0.07f, 0.09f, 0.0f, 0.11f, -0.09f, 0.0f, 0.11f,
        -0.09f, 0.0f, 0.11f, 0.09f, 0.0f, 0.11f, 0.09f, 0.03f, 0.11f,
        -0.09f, 0.0f, 0.11f, 0.09f, 0.03f, 0.11f, -0.09f, 0.03f, 0.11f,
        -0.09f, 0.03f, 0.11f, 0.09f, 0.03f, 0.11f, 0.09f, 0.038524f, 0.119488f,
        -0.09f, 0.03f, 0.11f, 0.09f, 0.038524f, 0.119488f, -0.09f, 0.038524f, 0.119488f,
        -0.09f, 0.038524f, 0.119488f, 0.09f, 0.038524f, 0.119488f, 0.09f, 0.049028f, 0.127986f,
        -0.09f, 0.038524f, 0.119488f, 0.09f, 0.049028f, 0.127986f, -0.09f, 0.049028f, 0.127986f,
        -0.09f, 0.049028f, 0.127986f, 0.09f, 0.049028f, 0.127986f, 0.09f, 0.061406f, 0.135547f,
        -0.09f, 0.049028f, 0.127986f, 0.09f, 0.061406f, 0.135547f, -0.09f, 0.061406f, 0.135547f,
        -0.09f, 0.061406f, 0.135547f, 0.09f, 0.061406f, 0.135547f, 0.09f, 0.075556f, 0.142222f,
        -0.09f, 0.061406f, 0.135547f, 0.09f, 0.075556f, 0.142222f, -0.09f, 0.075556f, 0.142222f,
        -0.09f, 0.075556f, 0.142222f, 0.09f, 0.075556f, 0.142222f, 0.09f, 0.091372f, 0.148064f,
        -0.09f, 0.075556f, 0.142222f, 0.09f, 0.091372f, 0.148064f, -0.09f, 0.091372f, 0.148064f,
        -0.09f, 0.091372f, 0.148064f, 0.09f, 0.091372f, 0.148064f, 0.09f, 0.10875f, 0.153125f,
        -0.09f, 0.091372f, 0.148064f, 0.09f, 0.10875f, 0.153125f, -0.09f, 0.10875f, 0.153125f,
        -0.09f, 0.10875f, 0.153125f, 0.09f, 0.10875f, 0.153125f, 0.09f, 0.127587f, 0.157457f,
        -0.09f, 0.10875f, 0.153125f, 0.09f, 0.127587f, 0.157457f, -0.09f, 0.127587f, 0.157457f,
        -0.09f, 0.127587f, 0.157457f, 0.09f, 0.127587f, 0.157457f, 0.09f, 0.147778f, 0.161111f,
        -0.09f, 0.127587f, 0.157457f, 0.09f, 0.147778f, 0.161111f, -0.09f, 0.147778f, 0.161111f,
        -0.09f, 0.147778f, 0.161111f, 0.09f, 0.147778f, 0.161111f, 0.09f, 0.169219f, 0.164141f,
        -0.09f, 0.147778f, 0.161111f, 0.09f, 0.169219f, 0.164141f, -0.09f, 0.169219f, 0.164141f,
        -0.09f, 0.169219f, 0.164141f, 0.09f, 0.169219f, 0.164141f, 0.09f, 0.191806f, 0.166597f,
        -0.09f, 0.169219f, 0.164141f, 0.09f, 0.191806f, 0.166597f, -0.09f, 0.191806f, 0.166597f,
        -0.09f, 0.191806f, 0.166597f, 0.09f, 0.191806f, 0.166597f, 0.09f, 0.215434f, 0.168533f,
        -0.09f, 0.191806f, 0.166597f, 0.09f, 0.215434f, 0.168533f, -0.09f, 0.215434f, 0.168533f,
        -0.09f, 0.215434f, 0.168533f, 0.09f, 0.215434f, 0.168533f, 0.09f, 0.24f, 0.17f,
        -0.09f, 0.215434f, 0.168533f, 0.09f, 0.24f, 0.17f, -0.09f, 0.24f, 0.17f,
        -0.09f, 0.24f, 0.17f, 0.09f, 0.24f, 0.17f, 0.09f, 0.266649f, 0.173524f,
        -0.09f, 0.24f, 0.17f, 0.09f, 0.266649f, 0.173524f, -0.09f, 0.266649f, 0.173524f,
        -0.09f, 0.266649f, 0.173524f, 0.09f, 0.266649f, 0.173524f, 0.09f, 0.291528f, 0.179028f,
        -0.09f, 0.266649f, 0.173524f, 0.09f, 0.291528f, 0.179028f, -0.09f, 0.291528f, 0.179028f,
        -0.09f, 0.291528f, 0.179028f, 0.09f, 0.291528f, 0.179028f, 0.09f, 0.314531f, 0.186406f,
        -0.09f, 0.291528f, 0.179028f, 0.09f, 0.314531f, 0.186406f, -0.09f, 0.314531f, 0.186406f,
        -0.09f, 0.314531f, 0.186406f, 0.09f, 0.314531f, 0.186406f, 0.09f, 0.335556f, 0.195556f,
        -0.09f, 0.314531f, 0.186406f, 0.09f, 0.335556f, 0.195556f, -0.09f, 0.335556f, 0.195556f,
        -0.09f, 0.335556f, 0.195556f, 0.09f, 0.335556f, 0.195556f, 0.09f, 0.354497f, 0.206372f,
        -0.09f, 0.335556f, 0.195556f, 0.09f, 0.354497f, 0.206372f, -0.09f, 0.354497f, 0.206372f,
        -0.09f, 0.354497f, 0.206372f, 0.09f, 0.354497f, 0.206372f, 0.09f, 0.37125f, 0.21875f,
        -0.09f, 0.354497f, 0.206372f, 0.09f, 0.37125f, 0.21875f, -0.09f, 0.37125f, 0.21875f,
        -0.09f, 0.37125f, 0.21875f, 0.09f, 0.37125f, 0.21875f, 0.09f, 0.385712f, 0.232587f,
        -0.09f, 0.37125f, 0.21875f, 0.09f, 0.385712f, 0.232587f, -0.09f, 0.385712f, 0.232587f,
        -0.09f, 0.385712f, 0.232587f, 0.09f, 0.385712f, 0.232587f, 0.09f, 0.397778f, 0.247778f,
        -0.09f, 0.385712f, 0.232587f, 0.09f, 0.397778f, 0.247778f, -0.09f, 0.397778f, 0.247778f,
        -0.09f, 0.397778f, 0.247778f, 0.09f, 0.397778f, 0.247778f, 0.09f, 0.407344f, 0.264219f,
        -0.09f, 0.397778f, 0.247778f, 0.09f, 0.407344f, 0.264219f, -0.09f, 0.407344f, 0.264219f,
        -0.09f, 0.407344f, 0.264219f, 0.09f, 0.407344f, 0.264219f, 0.09f, 0.414306f, 0.281806f,
        -0.09f, 0.407344f, 0.264219f, 0.09f, 0.414306f, 0.281806f, -0.09f, 0.414306f, 0.281806f,
        -0.09f, 0.414306f, 0.281806f, 0.09f, 0.414306f, 0.281806f, 0.09f, 0.418559f, 0.300434f,
        -0.09f, 0.414306f, 0.281806f, 0.09f, 0.418559f, 0.300434f, -0.09f, 0.418559f, 0.300434f,
        -0.09f, 0.418559f, 0.300434f, 0.09f, 0.418559f, 0.300434f, 0.09f, 0.42f, 0.32f,
        -0.09f, 0.418559f, 0.300434f, 0.09f, 0.42f, 0.32f, -0.09f, 0.42f, 0.32f,
        -0.09f, 0.42f, 0.32f, 0.09f, 0.42f, 0.32f, 0.09f, 0.48f, 0.32f,
        -0.09f, 0.42f, 0.32f, 0.09f, 0.48f, 0.32f, -0.09f, 0.48f, 0.32f,
        -0.09f, 0.48f, 0.32f, 0.09f, 0.48f, 0.32f, 0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.32f, 0.09f, 0.48f, 0.07f, -0.09f, 0.48f, 0.07f,
        -0.09f, 0.48f, 0.07f, 0.09f, 0.48f, 0.07f, 0.09f, 0.0f, 0.07f,
        -0.09f, 0.48f, 0.07f, 0.09f, 0.0f, 0.07f, -0.09f, 0.0f, 0.07f,
        -0.115f, 0.5f, 0.04f, 0.115f, 0.5f, 0.04f, 0.115f, 0.42f, 0.04f,
        -0.115f, 0.5f, 0.04f, 0.115f, 0.42f, 0.04f, -0.115f, 0.42f, 0.04f,
        -0.115f, 0.42f, 0.34f, 0.115f, 0.42f, 0.34f, 0.115f, 0.5f, 0.34f,
        -0.115f, 0.42f, 0.34f, 0.115f, 0.5f, 0.34f, -0.115f, 0.5f, 0.34f,
        0.115f, 0.42f, 0.04f, 0.115f, 0.42f, 0.34f, -0.115f, 0.42f, 0.34f,
        0.115f, 0.42f, 0.04f, -0.115f, 0.42f, 0.34f, -0.115f, 0.42f, 0.04f,
        0.115f, 0.5f, 0.04f, 0.115f, 0.5f, 0.34f, 0.115f, 0.42f, 0.34f,
        0.115f, 0.5f, 0.04f, 0.115f, 0.42f, 0.34f, 0.115f, 0.42f, 0.04f,
        -0.115f, 0.5f, 0.04f, -0.115f, 0.5f, 0.34f, 0.115f, 0.5f, 0.34f,
        -0.115f, 0.5f, 0.04f, 0.115f, 0.5f, 0.34f, 0.115f, 0.5f, 0.04f,
        -0.115f, 0.42f, 0.04f, -0.115f, 0.42f, 0.34f, -0.115f, 0.5f, 0.34f,
        -0.115f, 0.42f, 0.04f, -0.115f, 0.5f, 0.34f, -0.115f, 0.5f, 0.04f,
        -0.105f, 0.055f, 0.055f, 0.105f, 0.055f, 0.055f, 0.105f, -0.015f, 0.055f,
        -0.105f, 0.055f, 0.055f, 0.105f, -0.015f, 0.055f, -0.105f, -0.015f, 0.055f,
        -0.105f, -0.015f, 0.14f, 0.105f, -0.015f, 0.14f, 0.105f, 0.055f, 0.14f,
        -0.105f, -0.015f, 0.14f, 0.105f, 0.055f, 0.14f, -0.105f, 0.055f, 0.14f,
        0.105f, -0.015f, 0.055f, 0.105f, -0.015f, 0.14f, -0.105f, -0.015f, 0.14f,
        0.105f, -0.015f, 0.055f, -0.105f, -0.015f, 0.14f, -0.105f, -0.015f, 0.055f,
        0.105f, 0.055f, 0.055f, 0.105f, 0.055f, 0.14f, 0.105f, -0.015f, 0.14f,
        0.105f, 0.055f, 0.055f, 0.105f, -0.015f, 0.14f, 0.105f, -0.015f, 0.055f,
        -0.105f, 0.055f, 0.055f, -0.105f, 0.055f, 0.14f, 0.105f, 0.055f, 0.14f,
        -0.105f, 0.055f, 0.055f, 0.105f, 0.055f, 0.14f, 0.105f, 0.055f, 0.055f,
        -0.105f, -0.015f, 0.055f, -0.105f, -0.015f, 0.14f, -0.105f, 0.055f, 0.14f,
        -0.105f, -0.015f, 0.055f, -0.105f, 0.055f, 0.14f, -0.105f, 0.055f, 0.055f,
    };
    private static readonly float[] CapitalTemplate = {
        -0.135f, 0.0f, -0.135f, 0.135f, 0.035f, -0.135f, 0.135f, 0.0f, -0.135f,
        -0.135f, 0.0f, -0.135f, -0.135f, 0.035f, -0.135f, 0.135f, 0.035f, -0.135f,
        0.135f, 0.0f, -0.135f, 0.135f, 0.035f, 0.135f, 0.135f, 0.0f, 0.135f,
        0.135f, 0.0f, -0.135f, 0.135f, 0.035f, -0.135f, 0.135f, 0.035f, 0.135f,
        0.135f, 0.0f, 0.135f, -0.135f, 0.035f, 0.135f, -0.135f, 0.0f, 0.135f,
        0.135f, 0.0f, 0.135f, 0.135f, 0.035f, 0.135f, -0.135f, 0.035f, 0.135f,
        -0.135f, 0.0f, 0.135f, -0.135f, 0.035f, -0.135f, -0.135f, 0.0f, -0.135f,
        -0.135f, 0.0f, 0.135f, -0.135f, 0.035f, 0.135f, -0.135f, 0.035f, -0.135f,
        -0.135f, 0.035f, -0.135f, 0.15f, 0.035f, -0.15f, 0.135f, 0.035f, -0.135f,
        -0.135f, 0.035f, -0.135f, -0.15f, 0.035f, -0.15f, 0.15f, 0.035f, -0.15f,
        0.135f, 0.035f, -0.135f, 0.15f, 0.035f, 0.15f, 0.135f, 0.035f, 0.135f,
        0.135f, 0.035f, -0.135f, 0.15f, 0.035f, -0.15f, 0.15f, 0.035f, 0.15f,
        0.135f, 0.035f, 0.135f, -0.15f, 0.035f, 0.15f, -0.135f, 0.035f, 0.135f,
        0.135f, 0.035f, 0.135f, 0.15f, 0.035f, 0.15f, -0.15f, 0.035f, 0.15f,
        -0.135f, 0.035f, 0.135f, -0.15f, 0.035f, -0.15f, -0.135f, 0.035f, -0.135f,
        -0.135f, 0.035f, 0.135f, -0.15f, 0.035f, 0.15f, -0.15f, 0.035f, -0.15f,
        -0.15f, 0.035f, -0.15f, 0.17f, 0.095f, -0.17f, 0.15f, 0.035f, -0.15f,
        -0.15f, 0.035f, -0.15f, -0.17f, 0.095f, -0.17f, 0.17f, 0.095f, -0.17f,
        0.15f, 0.035f, -0.15f, 0.17f, 0.095f, 0.17f, 0.15f, 0.035f, 0.15f,
        0.15f, 0.035f, -0.15f, 0.17f, 0.095f, -0.17f, 0.17f, 0.095f, 0.17f,
        0.15f, 0.035f, 0.15f, -0.17f, 0.095f, 0.17f, -0.15f, 0.035f, 0.15f,
        0.15f, 0.035f, 0.15f, 0.17f, 0.095f, 0.17f, -0.17f, 0.095f, 0.17f,
        -0.15f, 0.035f, 0.15f, -0.17f, 0.095f, -0.17f, -0.15f, 0.035f, -0.15f,
        -0.15f, 0.035f, 0.15f, -0.17f, 0.095f, 0.17f, -0.17f, 0.095f, -0.17f,
        -0.17f, 0.095f, -0.17f, 0.18f, 0.095f, -0.18f, 0.17f, 0.095f, -0.17f,
        -0.17f, 0.095f, -0.17f, -0.18f, 0.095f, -0.18f, 0.18f, 0.095f, -0.18f,
        0.17f, 0.095f, -0.17f, 0.18f, 0.095f, 0.18f, 0.17f, 0.095f, 0.17f,
        0.17f, 0.095f, -0.17f, 0.18f, 0.095f, -0.18f, 0.18f, 0.095f, 0.18f,
        0.17f, 0.095f, 0.17f, -0.18f, 0.095f, 0.18f, -0.17f, 0.095f, 0.17f,
        0.17f, 0.095f, 0.17f, 0.18f, 0.095f, 0.18f, -0.18f, 0.095f, 0.18f,
        -0.17f, 0.095f, 0.17f, -0.18f, 0.095f, -0.18f, -0.17f, 0.095f, -0.17f,
        -0.17f, 0.095f, 0.17f, -0.18f, 0.095f, 0.18f, -0.18f, 0.095f, -0.18f,
        -0.18f, 0.095f, -0.18f, 0.18f, 0.14f, -0.18f, 0.18f, 0.095f, -0.18f,
        -0.18f, 0.095f, -0.18f, -0.18f, 0.14f, -0.18f, 0.18f, 0.14f, -0.18f,
        0.18f, 0.095f, -0.18f, 0.18f, 0.14f, 0.18f, 0.18f, 0.095f, 0.18f,
        0.18f, 0.095f, -0.18f, 0.18f, 0.14f, -0.18f, 0.18f, 0.14f, 0.18f,
        0.18f, 0.095f, 0.18f, -0.18f, 0.14f, 0.18f, -0.18f, 0.095f, 0.18f,
        0.18f, 0.095f, 0.18f, 0.18f, 0.14f, 0.18f, -0.18f, 0.14f, 0.18f,
        -0.18f, 0.095f, 0.18f, -0.18f, 0.14f, -0.18f, -0.18f, 0.095f, -0.18f,
        -0.18f, 0.095f, 0.18f, -0.18f, 0.14f, 0.18f, -0.18f, 0.14f, -0.18f,
        -0.135f, 0.0f, -0.135f, 0.135f, 0.0f, -0.135f, 0.135f, 0.0f, 0.135f,
        -0.135f, 0.0f, -0.135f, 0.135f, 0.0f, 0.135f, -0.135f, 0.0f, 0.135f,
        -0.18f, 0.14f, -0.18f, 0.18f, 0.14f, 0.18f, 0.18f, 0.14f, -0.18f,
        -0.18f, 0.14f, -0.18f, -0.18f, 0.14f, 0.18f, 0.18f, 0.14f, 0.18f,
    };
}
#endif

