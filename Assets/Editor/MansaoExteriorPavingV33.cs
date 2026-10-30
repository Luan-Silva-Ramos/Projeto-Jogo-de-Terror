#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoExteriorPavingV33
{
    const string Folder="Assets/_Game/Art/Textures/StoneTiles02/";
    const string Materials="Assets/_Game/Art/Materials/";
    class Data {public List<Vertex> vertices=new List<Vertex>();public List<Vector3> positions=new List<Vector3>();public List<Face> faces=new List<Face>();public float area;}
    [MenuItem("Tools/Mansao/Revestir piso frontal em pedra V33")]
    static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        if(mansion==null){Tell("Selecione Mansion ou um objeto dentro dela.");return;}
        int group=-1;
        try
        {
            Transform sourceTransform=mansion.Find("GroundFloor_V2/Floors/RoomFloors/Floor_Porch");
            if(sourceTransform==null || !sourceTransform.gameObject.activeInHierarchy)throw new InvalidOperationException("Floor_Porch precisa estar ativo na hierarquia aprovada.");
            if(sourceTransform.parent.Find("Exterior_Paving_V33")!=null)throw new InvalidOperationException("O conjunto V33 ja existe. Nenhuma copia foi criada.");
            var source=sourceTransform.GetComponent<ProBuilderMesh>();var renderer=sourceTransform.GetComponent<MeshRenderer>();
            if(source==null || renderer==null)throw new InvalidOperationException("Floor_Porch precisa continuar como malha ProBuilder.");
            Bounds expected=new Bounds();expected.SetMinMax(new Vector3(-11.5f,-.2f,-10.5f),new Vector3(11.5f,0,-4.7f));
            if(Vector3.Distance(renderer.bounds.min,expected.min)>.012f || Vector3.Distance(renderer.bounds.max,expected.max)>.012f)
                throw new InvalidOperationException("Floor_Porch difere das medidas conferidas. Min "+renderer.bounds.min.ToString("F3")+"; Max "+renderer.bounds.max.ToString("F3"));
            Data data=Prepare(source);
            Material stone=FloorMaterial(),edge=EdgeMaterial();
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Revestir piso frontal V33");
            GameObject root=new GameObject("Exterior_Paving_V33");SceneManager.MoveGameObjectToScene(root,sourceTransform.gameObject.scene);root.transform.SetParent(sourceTransform.parent,false);Undo.RegisterCreatedObjectUndo(root,"Criar piso externo em pedra");
            ProBuilderMesh pb=ProBuilderMesh.Create(data.vertices,data.faces,SharedVertex.GetSharedVerticesWithPositions(data.positions),null,new[]{stone,edge});
            SceneManager.MoveGameObjectToScene(pb.gameObject,sourceTransform.gameObject.scene);pb.transform.SetParent(root.transform,false);pb.transform.localPosition=sourceTransform.localPosition;pb.transform.localRotation=sourceTransform.localRotation;pb.transform.localScale=sourceTransform.localScale;pb.name="Floor_Porch_Stone_V33";pb.gameObject.layer=source.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar copia revestida do piso");pb.ToMesh();pb.Refresh();
            GameObjectUtility.SetStaticEditorFlags(pb.gameObject,GameObjectUtility.GetStaticEditorFlags(source.gameObject));
            foreach(Collider automatic in pb.GetComponents<Collider>())Undo.DestroyObjectImmediate(automatic);
            foreach(Collider original in source.GetComponents<Collider>())
            {
                Collider copied=(Collider)Undo.AddComponent(pb.gameObject,original.GetType());EditorUtility.CopySerialized(original,copied);
                if(copied is MeshCollider meshCollider)meshCollider.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;
            }
            var nextRenderer=pb.GetComponent<MeshRenderer>();nextRenderer.shadowCastingMode=renderer.shadowCastingMode;nextRenderer.receiveShadows=renderer.receiveShadows;
            if(Vector3.Distance(nextRenderer.bounds.min,renderer.bounds.min)>.001f || Vector3.Distance(nextRenderer.bounds.max,renderer.bounds.max)>.001f)
                throw new InvalidOperationException("Os limites da copia nao coincidiram com o piso original.");
            Undo.RecordObject(source.gameObject,"Preservar piso quadriculado como reserva");source.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(source.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;
            Debug.Log("V33: Floor_Porch revestido, UVs em escala de 2 m e borda de 28 cm nas tres margens externas. Topo Y 0 e geometria original preservados. Colliders copiados. Piso original guardado desativado; interiores, coberturas e traseira nao foram alterados. Ctrl+Z desfaz a montagem.",root);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi aplicado: "+e.Message);}
    }
    static Data Prepare(ProBuilderMesh source)
    {
        var data=new Data();
        foreach(Face face in source.faces){var ids=face.indexes;for(int i=0;i<ids.Count;i+=3){
            var polygon=new List<Vector3>();for(int k=0;k<3;k++)polygon.Add(source.transform.TransformPoint(source.positions[ids[i+k]]));
            Vector3 normal=Vector3.Cross(polygon[1]-polygon[0],polygon[2]-polygon[0]).normalized;bool top=normal.y>.99f;
            var polygons=new List<List<Vector3>>{polygon};
            if(top){polygons=Split(polygons,2,-10.22f);polygons=Split(polygons,0,-11.22f);polygons=Split(polygons,0,11.22f);}
            foreach(var part in polygons)for(int k=1;k<part.Count-1;k++){
                Vector3[] triangle={part[0],part[k],part[k+1]};Vector3 cross=Vector3.Cross(triangle[1]-triangle[0],triangle[2]-triangle[0]);if(cross.sqrMagnitude<1e-10f)continue;
                Vector3 center=(triangle[0]+triangle[1]+triangle[2])/3;
                bool border=!top || center.z< -10.22f || Mathf.Abs(center.x)>11.22f;
                int start=data.vertices.Count;
                foreach(Vector3 world in triangle){Vector3 local=source.transform.InverseTransformPoint(world);data.positions.Add(local);Vector2 uv=top?new Vector2(world.x/2f,world.z/2f):new Vector2((Mathf.Abs(normal.x)>.5f?world.z:world.x)/2f,world.y/2f);data.vertices.Add(new Vertex{position=local,uv0=uv});}
                Face f=new Face(new[]{start,start+1,start+2});f.manualUV=true;f.smoothingGroup=face.smoothingGroup;f.submeshIndex=border?1:0;data.faces.Add(f);if(top)data.area+=cross.magnitude*.5f;
            }
        }}
        if(data.area<70 || data.area>95)throw new InvalidOperationException("Area superior inesperada: "+data.area.ToString("F3")+" m2.");return data;
    }
    static List<List<Vector3>> Split(List<List<Vector3>> input,int axis,float cut)
    {
        var output=new List<List<Vector3>>();foreach(var p in input){float lo=float.PositiveInfinity,hi=float.NegativeInfinity;foreach(Vector3 v in p){lo=Mathf.Min(lo,v[axis]);hi=Mathf.Max(hi,v[axis]);}if(lo>=cut-.00001f || hi<=cut+.00001f){output.Add(p);continue;}var a=Clip(p,axis,cut,true);var b=Clip(p,axis,cut,false);if(a.Count>=3)output.Add(a);if(b.Count>=3)output.Add(b);}return output;
    }
    static List<Vector3> Clip(List<Vector3> input,int axis,float cut,bool greater)
    {
        var output=new List<Vector3>();Vector3 a=input[input.Count-1];float da=(greater?1:-1)*(a[axis]-cut);foreach(Vector3 b in input){float db=(greater?1:-1)*(b[axis]-cut);if((da>=0)!=(db>=0))output.Add(Vector3.Lerp(a,b,da/(da-db)));if(db>=0)output.Add(b);a=b;da=db;}return output;
    }
    static void Import(string path,bool srgb,bool normal,bool readable=false)
    {
        TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new FileNotFoundException(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=srgb;importer.isReadable=readable;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
    }
    static Material FloorMaterial()
    {
        string path=Materials+"M_Exterior_Flagstone_V33.mat";Material material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new InvalidOperationException("Shader URP/Lit nao encontrado.");
        string diffuse=Folder+"stone_tiles_02_diff_2k.jpg",normal=Folder+"stone_tiles_02_nor_gl_2k.jpg",rough=Folder+"stone_tiles_02_rough_2k.jpg";
        Import(diffuse,true,false);Import(normal,false,true);Import(rough,false,false,true);
        Texture2D roughness=AssetDatabase.LoadAssetAtPath<Texture2D>(rough);Color[] pixels=roughness.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(0,0,0,1-Mathf.Clamp01(pixels[i].r));
        Texture2D packed=new Texture2D(roughness.width,roughness.height,TextureFormat.RGBA32,false,true);packed.SetPixels(pixels);packed.Apply();string packedPath=Folder+"StoneTiles02_MetallicSmoothness_V33.png";
        File.WriteAllBytes(packedPath,packed.EncodeToPNG());UnityEngine.Object.DestroyImmediate(packed);AssetDatabase.ImportAsset(packedPath);Import(packedPath,false,false);Import(rough,false,false,false);
        material=new Material(shader);material.name="M_Exterior_Flagstone_V33";material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse));material.SetColor("_BaseColor",new Color(.9f,.9f,.9f,1));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));material.SetFloat("_BumpScale",.30f);material.EnableKeyword("_NORMALMAP");material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.3f);material.SetFloat("_SmoothnessTextureChannel",0);AssetDatabase.CreateAsset(material,path);AssetDatabase.SaveAssets();return material;
    }
    static Material EdgeMaterial()
    {
        string path=Materials+"M_Exterior_CutStone_V33.mat";Material material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
        material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="M_Exterior_CutStone_V33";material.SetColor("_BaseColor",new Color(.34f,.35f,.35f,1));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(material,path);AssetDatabase.SaveAssets();return material;
    }
    static void Tell(string message){EditorUtility.DisplayDialog("Piso externo V33",message,"OK");}
}
#endif
