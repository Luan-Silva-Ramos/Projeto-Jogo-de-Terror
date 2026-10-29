#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoRoofSlateV20
{
    private const string Folder="Assets/_Game/Art/Textures/RoofSlates03/";
    private const string MaterialPath="Assets/_Game/Art/Materials/M_Roof_Slate_V20.mat";
    private static readonly string[] Sources={
        "Slate_Roofs_V19/Roof_WholeHouse_Joined_V05_Slate_V19",
        "Slate_Roofs_V19/Roof_Lower_ServiceWest_V06_Slate_V19",
        "Slate_Roofs_V19/Roof_Lower_ServiceRear_V06_Slate_V19",
        "Slate_Roofs_V19/Roof_Lower_Pantry_V06_Slate_V19",
        "Slate_Roofs_V19/Fronton_Integrated_Roof_V08_Slate_V19"
    };
    private class Data
    {
        public ProBuilderMesh source;
        public List<Vector3> vertices=new List<Vector3>();
        public List<Vector2> uv=new List<Vector2>();
        public List<Face> faces=new List<Face>();
        public Material[] materials;
        public int slateFaces;
    }

    [MenuItem("Tools/Mansao/Corrigir emendas e tom da ardosia V20")]
    private static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Tell("Saia do Play Mode."); return; }
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null) { Tell("Selecione um objeto da mansao."); return; }
        if(parent.Find("Slate_Roofs_V20")!=null) { Tell("A V20 ja existe. Nada foi duplicado."); return; }
        if(Shader.Find("Mansao/Slate V20")==null) { Tell("Importe MansaoSlateV20.shader antes de executar."); return; }
        string[] files={"roof_slates_03_diff_2k.jpg","roof_slates_03_nor_gl_2k.png","roof_slates_03_rough_2k.png"};
        foreach(string file in files)
            if(AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+file)==null)
            { Tell("Falta importar "+Folder+file+". Copie a pasta Assets do pacote para o projeto."); return; }
        var sets=new List<Data>();
        foreach(string path in Sources)
        {
            Transform t=parent.Find(path);
            ProBuilderMesh source=t==null?null:t.GetComponent<ProBuilderMesh>();
            if(source==null || !t.gameObject.activeInHierarchy)
            { Tell("Telhado original nao encontrado ativo: "+path+". Nada foi alterado."); return; }
            if(source.GetComponent<MeshRenderer>()==null) { Tell("Telhado sem Renderer. Nada foi alterado."); return; }
            Data data=Prepare(source);
            if(data.slateFaces==0) { Tell("Nao encontrei faces superiores em "+t.name+". Nada foi alterado."); return; }
            sets.Add(data);
        }
        // Importacao e criacao de assets nao fazem parte do Undo da cena.
        Material slate;
        try { slate=GetMaterial(); }
        catch(System.Exception ex) { Debug.LogException(ex); Tell("Nao foi possivel preparar o material. A cena nao foi alterada."); return; }
        Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Corrigir ardosia V20");
        try {
            GameObject root=new GameObject("Slate_Roofs_V20");
            SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);
            root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar telhados com ardosia");
            foreach(Data data in sets)
            {
                Build(data,root.transform,slate);
                if(!data.source.name.Contains("Fronton_")) BuildSeams(data,root.transform);
                Undo.RecordObject(data.source.gameObject,"Preservar telhado original como reserva");
                data.source.gameObject.SetActive(false);
            }
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            Undo.CollapseUndoOperations(group); Selection.activeGameObject=root;
            Tell("V20 criada: cor neutra, fileiras por altura e arremates estreitos. A borda clara foi excluida da amostragem. Malhas V19 preservadas desativadas. Ctrl+Z desfaz a montagem da cena; assets permanecem.");
        } catch(System.Exception ex) {
            Undo.RevertAllDownToGroup(group); Debug.LogException(ex);
            Tell("A montagem da cena foi desfeita devido a um erro. Consulte a Console.");
        }
    }
    private static Data Prepare(ProBuilderMesh source)
    {
        var data=new Data {source=source};
        Material[] old=source.GetComponent<MeshRenderer>().sharedMaterials;
        data.materials=new Material[old.Length+1]; old.CopyTo(data.materials,0);
        IList<Vector2> oldUV=source.textures;
        foreach(Face face in source.faces)
        {
            var ids=face.indexes;
            for(int i=0;i<ids.Count;i+=3)
            {
                int[] triangle={ids[i],ids[i+1],ids[i+2]};
                Vector3 a=source.transform.TransformPoint(source.positions[triangle[0]]);
                Vector3 b=source.transform.TransformPoint(source.positions[triangle[1]]);
                Vector3 c=source.transform.TransformPoint(source.positions[triangle[2]]);
                Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                bool top=normal.y>.025f;
                Vector3 u=Vector3.Cross(Vector3.up,normal).normalized;
                Vector3 v=Vector3.Cross(normal,u).normalized;
                if(u.sqrMagnitude<.5f) {u=Vector3.right; v=Vector3.forward;}
                int first=data.vertices.Count;
                foreach(int id in triangle)
                {
                    Vector3 world=source.transform.TransformPoint(source.positions[id]);
                    data.vertices.Add(world);
                    bool low=source.name.Contains("Roof_Lower_");
                    float rowV=(world.y-(low?3.8f:7.4f))/(low?.5f:1.8f);
                    data.uv.Add(top ? new Vector2(Vector3.Dot(world,u)/3f,rowV) :
                        oldUV!=null && oldUV.Count==source.vertexCount ? oldUV[id] : Vector2.zero);
                }
                Face f=new Face(new[]{first,first+1,first+2});
                f.manualUV=true; f.smoothingGroup=face.smoothingGroup;
                f.submeshIndex=top ? old.Length : face.submeshIndex;
                data.faces.Add(f); if(top) data.slateFaces++;
            }
        }
        return data;
    }
    private static void Build(Data data,Transform parent,Material slate)
    {
        var local=new List<Vector3>();
        foreach(Vector3 world in data.vertices) local.Add(parent.InverseTransformPoint(world));
        data.materials[data.materials.Length-1]=slate;
        var vertices=new List<Vertex>();
        for(int i=0;i<local.Count;i++) vertices.Add(new Vertex {position=local[i],uv0=data.uv[i]});
        ProBuilderMesh pb=ProBuilderMesh.Create(vertices,data.faces,
            SharedVertex.GetSharedVerticesWithPositions(local),null,data.materials);
        SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);
        pb.transform.SetParent(parent,false); pb.transform.localPosition=Vector3.zero;
        pb.transform.localRotation=Quaternion.identity; pb.transform.localScale=Vector3.one;
        pb.name=data.source.name.Replace("_Slate_V19", "")+"_Slate_V20";
        Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar cobertura revestida");
        pb.gameObject.layer=data.source.gameObject.layer;
        GameObjectUtility.SetStaticEditorFlags(pb.gameObject,GameObjectUtility.GetStaticEditorFlags(data.source.gameObject));
        MeshRenderer renderer=pb.GetComponent<MeshRenderer>();
        MeshRenderer original=data.source.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode=original.shadowCastingMode; renderer.receiveShadows=original.receiveShadows;
        pb.textures=data.uv; pb.ToMesh(); pb.Refresh();
        MeshCollider oldCollider=data.source.GetComponent<MeshCollider>();
        MeshCollider collider=pb.GetComponent<MeshCollider>();
        if(oldCollider!=null) {
            if(collider==null) collider=Undo.AddComponent<MeshCollider>(pb.gameObject);
            collider.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;
            collider.convex=oldCollider.convex; collider.isTrigger=oldCollider.isTrigger;
            collider.sharedMaterial=oldCollider.sharedMaterial; collider.enabled=oldCollider.enabled;
        } else if(collider!=null) Undo.DestroyObjectImmediate(collider);
        Bounds before=original.bounds,after=renderer.bounds;
        if(Vector3.Distance(before.min,after.min)>.001f || Vector3.Distance(before.max,after.max)>.001f)
            throw new System.InvalidOperationException("A copia nao preservou os limites de "+data.source.name);
    }
    private static void Import(string path,bool srgb,bool normal,bool readable=false)
    {
        TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null) throw new System.IO.FileNotFoundException(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.sRGBTexture=srgb; importer.isReadable=readable;
        importer.wrapMode=TextureWrapMode.Repeat; importer.filterMode=FilterMode.Trilinear;
        importer.mipmapEnabled=true; importer.anisoLevel=8; importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }
    private static Material GetMaterial()
    {
        string diffuse=Folder+"roof_slates_03_diff_2k.jpg";
        string normal=Folder+"roof_slates_03_nor_gl_2k.png";
        string rough=Folder+"roof_slates_03_rough_2k.png";
        Import(diffuse,true,false); Import(normal,false,true); Import(rough,false,false);
        Material material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(material==null) {
            if(!AssetDatabase.IsValidFolder("Assets/_Game/Art/Materials"))
                throw new DirectoryNotFoundException("Assets/_Game/Art/Materials");
            material=new Material(Shader.Find("Mansao/Slate V20"));
            material.name="M_Roof_Slate_V20";
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse));
            material.SetColor("_BaseColor",new Color(.46f,.46f,.46f,1));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.SetFloat("_BumpScale",.35f);
            material.SetTexture("_RoughMap",AssetDatabase.LoadAssetAtPath<Texture2D>(rough));
            material.SetFloat("_Smoothness",.35f); material.SetFloat("_VEnd",.965f);
            material.SetTextureScale("_BaseMap",Vector2.one);
            AssetDatabase.CreateAsset(material,MaterialPath); AssetDatabase.SaveAssets();
        }
        return material;
    }
    private class Edge
    {
        public Vector3 a,b,normal,inside;
        public int count=1;
        public Vector3 otherNormal,otherInside;
    }
    private static string PointKey(Vector3 p)
    {return Mathf.RoundToInt(p.x*1000)+","+Mathf.RoundToInt(p.y*1000)+","+Mathf.RoundToInt(p.z*1000);}
    private static void BuildSeams(Data data,Transform parent)
    {
        var edges=new Dictionary<string,Edge>();
        foreach(Face face in data.source.faces)
        {
            var ids=face.indexes;
            for(int i=0;i<ids.Count;i+=3)
            {
                Vector3[] p={data.source.transform.TransformPoint(data.source.positions[ids[i]]),
                    data.source.transform.TransformPoint(data.source.positions[ids[i+1]]),
                    data.source.transform.TransformPoint(data.source.positions[ids[i+2]])};
                Vector3 n=Vector3.Cross(p[1]-p[0],p[2]-p[0]).normalized;
                if(n.y<=.025f) continue;
                for(int j=0;j<3;j++)
                {
                    Vector3 a=p[j],b=p[(j+1)%3],third=p[(j+2)%3];
                    string ka=PointKey(a),kb=PointKey(b);
                    string key=string.CompareOrdinal(ka,kb)<0?ka+"/"+kb:kb+"/"+ka;
                    Vector3 along=(b-a).normalized;
                    Vector3 inward=Vector3.ProjectOnPlane(third-(a+b)*.5f,along).normalized;
                    if(edges.TryGetValue(key,out Edge edge)) {edge.count++;edge.otherNormal=n;edge.otherInside=inward;}
                    else edges.Add(key,new Edge {a=a,b=b,normal=n,inside=inward});
                }
            }
        }
        var vertices=new List<Vector3>();var faces=new List<Face>();
        foreach(Edge edge in edges.Values)
        {
            if(edge.count!=2 || Vector3.Dot(edge.normal,edge.otherNormal)>.990268f || Vector3.Distance(edge.a,edge.b)<.10f) continue;
            SeamQuad(edge.a,edge.b,edge.inside,edge.normal,parent,vertices,faces);
            SeamQuad(edge.a,edge.b,edge.otherInside,edge.otherNormal,parent,vertices,faces);
        }
        if(faces.Count==0) return;
        Material material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Roof_Seam_V20.mat");
        if(material==null)
        {
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name="M_Roof_Seam_V20";material.SetColor("_BaseColor",new Color(.13f,.13f,.13f,1));
            material.SetFloat("_Metallic",.25f);material.SetFloat("_Smoothness",.15f);
            AssetDatabase.CreateAsset(material,"Assets/_Game/Art/Materials/M_Roof_Seam_V20.mat");AssetDatabase.SaveAssets();
        }
        ProBuilderMesh pb=ProBuilderMesh.Create(vertices,faces);
        SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);
        pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;
        pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;
        pb.name=data.source.name.Replace("_Slate_V19", "")+"_Seams_V20";
        Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar arremates de cobertura");
        pb.GetComponent<MeshRenderer>().sharedMaterial=material;pb.ToMesh();pb.Refresh();
        MeshCollider collider=pb.GetComponent<MeshCollider>();if(collider!=null) Undo.DestroyObjectImmediate(collider);
    }
    private static void SeamQuad(Vector3 a,Vector3 b,Vector3 inside,Vector3 normal,Transform parent,List<Vector3> vertices,List<Face> faces)
    {
        Vector3[] p={a+normal*.006f,b+normal*.006f,b+inside*.045f+normal*.006f,a+inside*.045f+normal*.006f};
        if(Vector3.Dot(Vector3.Cross(p[1]-p[0],p[2]-p[0]),normal)<0) System.Array.Reverse(p);
        int first=vertices.Count;foreach(Vector3 point in p) vertices.Add(parent.InverseTransformPoint(point));
        faces.Add(new Face(new[]{first,first+1,first+2,first,first+2,first+3}));
    }
    private static void Tell(string text) {EditorUtility.DisplayDialog("Ardosia V20",text,"OK");}
}
#endif



