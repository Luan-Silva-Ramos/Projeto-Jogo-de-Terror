#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

// Correcao exclusiva da cobertura V30. Nao executa automaticamente.
public static class MansaoRoofJunctionV31
{
    const float Scale=.8f/1.84f, Slope=1.85f/4.25f;
    const string Main="Roof_WholeHouse_Joined_V05_Slate_V20";
    const string Gable="Fronton_Integrated_Roof_V08_Slate_V20";
    const string Closure="Roof_Junction_Closures_Slate_V21";
    const string Seams="Roof_WholeHouse_Joined_V05_Seams_V20";
    class Data {
        public ProBuilderMesh source;
        public List<Vertex> vertices=new List<Vertex>();
        public List<Face> faces=new List<Face>();
        public Material[] materials;
    }
    class Edge {public Vector3 a,b,n,inside,n2,inside2;public int count=1;}
    static float Background(Vector3 p){return 7.4f+Slope*Mathf.Max(0,p.z+4.85f);}
    // Retira somente o antigo ressalto triangular. O plano principal continua original.
    public static Vector3 FlattenOldBranch(Vector3 p)
    {
        if(p.z< -4.86f)return p;
        float branch=9.15f-(1.75f/1.6f)*Mathf.Abs(p.x);
        float excess=Mathf.Max(0,branch-Background(p));
        if(excess>0 && p.y>=branch-.125f)p.y-=excess;
        return p;
    }
    public static Vector3 ShortenGable(Vector3 p)
    {
        if(p.y>7.4f)p.y=7.4f+(p.y-7.4f)*Scale;
        if(p.z> -4.85f)p.z=-4.85f+(p.z+4.85f)*Scale;
        return p;
    }
    [MenuItem("Tools/Mansao/Corrigir encontro do telhado V31")]
    static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null){Tell("Selecione um objeto da Mansion.");return;}
        if(parent.Find("Roof_Central_Junction_V31")!=null){Tell("A V31 ja existe. Nenhuma copia foi criada.");return;}
        int group=-1;
        try {
            ProBuilderMesh main=Find(mansion,Main),gable=Find(mansion,Gable),closure=Find(mansion,Closure);
            string[] replaced={Main,Gable,Closure,Seams};
            var current=new List<ProBuilderMesh>();
            foreach(string name in replaced){var p=Find(mansion,name+"_Reduced_V30");if(!p.gameObject.activeInHierarchy)throw new InvalidOperationException(p.name+" precisa estar ativo.");current.Add(p);}
            ProBuilderMesh frame=Find(mansion,"Fronton_CurvedFrame_AndLegs_V08_Reduced_V30");
            if(!frame.gameObject.activeInHierarchy || Mathf.Abs(frame.GetComponent<Renderer>().bounds.max.y-8.2f)>.025f)
                throw new InvalidOperationException("O frontao aprovado V30 nao esta na altura esperada (Y 8.20).");
            if(Mathf.Abs(main.GetComponent<Renderer>().bounds.max.y-10.2f)>.025f)
                throw new InvalidOperationException("A reserva original do telhado foi alterada.");
            Data a=Copy(main,false),b=Copy(gable,true),c=Copy(closure,false);
            Material metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Roof_Seam_V20.mat");
            if(metal==null)throw new InvalidOperationException("Material M_Roof_Seam_V20 nao encontrado.");
            Data seam=MakeSeams(a,metal);
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Corrigir encontro do telhado V31");
            GameObject root=new GameObject("Roof_Central_Junction_V31");SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar correcao V31");
            Build(a,root.transform,"Roof_Main_Plane_V31");Build(b,root.transform,"Roof_Short_Gable_V31");Build(c,root.transform,"Roof_Junction_Closures_V31");
            if(seam.faces.Count>0)Build(seam,root.transform,"Roof_Seams_V31");
            foreach(var p in current){Undo.RecordObject(p.gameObject,"Preservar cobertura V30 como reserva");p.gameObject.SetActive(false);}
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);Selection.activeGameObject=root;Undo.CollapseUndoOperations(group);
            Debug.Log("V31: plano principal recuperado e cobertura curta do frontao encontra esse plano. Janela, moldura, reboco, lanternas e tetos internos preservados. V30 guardada desativada.",root);
        } catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("Nada foi aplicado: "+e.Message);}
    }
    static ProBuilderMesh Find(Transform root,string name)
    {
        ProBuilderMesh found=null;
        foreach(var p in root.GetComponentsInChildren<ProBuilderMesh>(true))if(p.name==name){if(found!=null)throw new InvalidOperationException("Nome duplicado: "+name);found=p;}
        if(found==null)throw new InvalidOperationException("Nao encontrei "+name+" (incluindo reservas).");return found;
    }
    static Data Copy(ProBuilderMesh source,bool gable)
    {
        var d=new Data{source=source,materials=source.GetComponent<MeshRenderer>().sharedMaterials};var uvs=source.textures;
        foreach(Face face in source.faces){var ids=face.indexes;for(int i=0;i<ids.Count;i+=3){
            var p=new Vector3[3];var uv=new Vector2[3];bool changed=false;
            for(int k=0;k<3;k++){int id=ids[i+k];Vector3 old=source.transform.TransformPoint(source.positions[id]);p[k]=gable?ShortenGable(old):FlattenOldBranch(old);changed|=(p[k]-old).sqrMagnitude>1e-10f;uv[k]=uvs!=null && uvs.Count==source.vertexCount?uvs[id]:Vector2.zero;}
            Vector3 cross=Vector3.Cross(p[1]-p[0],p[2]-p[0]);if(cross.sqrMagnitude<1e-10f)continue;
            Vector3 n=cross.normalized;
            if(changed && n.y>.025f){Vector3 u=Vector3.Cross(Vector3.up,n).normalized;if(u.sqrMagnitude<.5f)u=Vector3.right;for(int k=0;k<3;k++)uv[k]=new Vector2(Vector3.Dot(p[k],u)/3f,(p[k].y-7.4f)/1.8f);}
            foreach(var point in p)if(point.y<7.269f)throw new InvalidOperationException("A correcao ultrapassaria a altura externa segura em "+source.name);
            Add(d,p,uv,face.submeshIndex,face.smoothingGroup);
        }}
        if(d.faces.Count==0)throw new InvalidOperationException("Malha vazia: "+source.name);return d;
    }
    static void Add(Data d,Vector3[] p,Vector2[] uv,int material,int smoothing=0)
    {
        int start=d.vertices.Count;for(int k=0;k<3;k++)d.vertices.Add(new Vertex{position=p[k],uv0=uv[k]});
        var f=new Face(new[]{start,start+1,start+2});f.manualUV=true;f.submeshIndex=material;f.smoothingGroup=smoothing;d.faces.Add(f);
    }
    static string Key(Vector3 p){return Mathf.RoundToInt(p.x*1000)+","+Mathf.RoundToInt(p.y*1000)+","+Mathf.RoundToInt(p.z*1000);}
    static Data MakeSeams(Data source,Material metal)
    {
        var edges=new Dictionary<string,Edge>();
        foreach(var f in source.faces){var ids=f.indexes;Vector3[] p={source.vertices[ids[0]].position,source.vertices[ids[1]].position,source.vertices[ids[2]].position};Vector3 n=Vector3.Cross(p[1]-p[0],p[2]-p[0]).normalized;if(n.y<=.025f)continue;
            for(int k=0;k<3;k++){Vector3 a=p[k],b=p[(k+1)%3],third=p[(k+2)%3];string ka=Key(a),kb=Key(b),key=string.CompareOrdinal(ka,kb)<0?ka+"/"+kb:kb+"/"+ka;Vector3 inside=Vector3.ProjectOnPlane(third-(a+b)*.5f,(b-a).normalized).normalized;
                if(edges.TryGetValue(key,out Edge edge)){edge.count++;edge.n2=n;edge.inside2=inside;}else edges.Add(key,new Edge{a=a,b=b,n=n,inside=inside});}}
        var d=new Data{materials=new[]{metal}};
        foreach(var e in edges.Values){if(e.count!=2 || Vector3.Dot(e.n,e.n2)>.990268f || Vector3.Distance(e.a,e.b)<.1f)continue;SeamQuad(d,e.a,e.b,e.inside,e.n);SeamQuad(d,e.a,e.b,e.inside2,e.n2);}return d;
    }
    static void SeamQuad(Data d,Vector3 a,Vector3 b,Vector3 inside,Vector3 n)
    {
        Vector3[] q={a+n*.006f,b+n*.006f,b+inside*.045f+n*.006f,a+inside*.045f+n*.006f};if(Vector3.Dot(Vector3.Cross(q[1]-q[0],q[2]-q[0]),n)<0)Array.Reverse(q);
        Add(d,new[]{q[0],q[1],q[2]},new Vector2[3],0);Add(d,new[]{q[0],q[2],q[3]},new Vector2[3],0);
    }
    static void Build(Data d,Transform parent,string name)
    {
        var local=new List<Vector3>();for(int i=0;i<d.vertices.Count;i++){var v=d.vertices[i];v.position=parent.InverseTransformPoint(v.position);d.vertices[i]=v;local.Add(v.position);}
        ProBuilderMesh pb=ProBuilderMesh.Create(d.vertices,d.faces,SharedVertex.GetSharedVerticesWithPositions(local),null,d.materials);
        SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;pb.name=name;Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar malha V31");pb.ToMesh();pb.Refresh();
        var collider=pb.GetComponent<MeshCollider>();var original=d.source==null?null:d.source.GetComponent<MeshCollider>();
        if(original!=null){if(collider==null)collider=Undo.AddComponent<MeshCollider>(pb.gameObject);collider.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;collider.sharedMaterial=original.sharedMaterial;collider.enabled=original.enabled;}
        else if(collider!=null)Undo.DestroyObjectImmediate(collider);
        if(d.source!=null){pb.gameObject.layer=d.source.gameObject.layer;var old=d.source.GetComponent<MeshRenderer>();var r=pb.GetComponent<MeshRenderer>();r.shadowCastingMode=old.shadowCastingMode;r.receiveShadows=old.receiveShadows;}
    }
    static void Tell(string message){EditorUtility.DisplayDialog("Encontro do telhado V31",message,"OK");}
}
#endif
