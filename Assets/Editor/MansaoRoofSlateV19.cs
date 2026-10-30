#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoRoofSlateV19
{
    private const string Folder="Assets/_Game/Art/Textures/RoofSlates03/";
    private const string MaterialPath="Assets/_Game/Art/Materials/M_Roof_Slate_V19.mat";
    private static readonly string[] Sources={
        "Roof_Full_Assembly_V05/Roof_WholeHouse_Joined_V05",
        "Roof_Corrections_V06/Roof_Lower_ServiceWest_V06",
        "Roof_Corrections_V06/Roof_Lower_ServiceRear_V06",
        "Roof_Corrections_V06/Roof_Lower_Pantry_V06",
        "Fronton_Central_Integrated_V08/Fronton_Integrated_Roof_V08"
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

    [MenuItem("Tools/Mansao/Aplicar ardosia nos telhados V19")]
    private static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Tell("Saia do Play Mode."); return; }
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null) { Tell("Selecione um objeto da mansao."); return; }
        if(parent.Find("Slate_Roofs_V19")!=null) { Tell("A V19 ja existe. Nada foi duplicado."); return; }
        if(Shader.Find("Universal Render Pipeline/Lit")==null) { Tell("Shader URP/Lit nao encontrado."); return; }
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
        Undo.SetCurrentGroupName("Aplicar ardosia V19");
        try {
            GameObject root=new GameObject("Slate_Roofs_V19");
            SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);
            root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar telhados com ardosia");
            foreach(Data data in sets)
            {
                Build(data,root.transform,slate);
                Undo.RecordObject(data.source.gameObject,"Preservar telhado original como reserva");
                data.source.gameObject.SetActive(false);
            }
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
            Undo.CollapseUndoOperations(group); Selection.activeGameObject=root;
            Tell("Ardosia aplicada a cinco malhas de cobertura. Geometria preservada, originais desativados como reserva. Tetos internos, fechamentos, madeira, rufos e jardim preservados. Ctrl+Z desfaz a montagem da cena; os assets importados permanecem.");
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
                    // 3 metros por repeticao em ambas as direcoes da superficie.
                    data.uv.Add(top ? new Vector2(Vector3.Dot(world,u)/3f,Vector3.Dot(world,v)/3f) :
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
        pb.name=data.source.name+"_Slate_V19";
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
        Import(diffuse,true,false); Import(normal,false,true); Import(rough,false,false,true);
        string packed=Folder+"roof_slates_03_metallic_smoothness_V19.png";
        if(!File.Exists(packed)) {
            Texture2D source=AssetDatabase.LoadAssetAtPath<Texture2D>(rough);
            Color32[] pixels=source.GetPixels32();
            for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(0,0,0,(byte)(255-pixels[i].r));
            Texture2D map=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false,true);
            try {map.SetPixels32(pixels);map.Apply();File.WriteAllBytes(packed,map.EncodeToPNG());}
            finally {Object.DestroyImmediate(map);}
            AssetDatabase.ImportAsset(packed);
        }
        Import(rough,false,false); Import(packed,false,false);
        Material material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(material==null) {
            if(!AssetDatabase.IsValidFolder("Assets/_Game/Art/Materials"))
                throw new DirectoryNotFoundException("Assets/_Game/Art/Materials");
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name="M_Roof_Slate_V19";
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse));
            material.SetColor("_BaseColor",new Color(.55f,.60f,.66f,1));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.SetFloat("_BumpScale",.65f); material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed));
            material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Metallic",0);
            material.SetFloat("_Smoothness",.7f); material.SetFloat("_SmoothnessTextureChannel",0);
            material.SetTextureScale("_BaseMap",Vector2.one);
            AssetDatabase.CreateAsset(material,MaterialPath); AssetDatabase.SaveAssets();
        }
        return material;
    }
    private static void Tell(string text) {EditorUtility.DisplayDialog("Ardosia V19",text,"OK");}
}
#endif
