#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class MansaoCandleLanternsV29
{
    private const string Folder="Assets/_Game/Art/Models/VictorianLantern_lpfeffer/";
    private const string Materials="Assets/_Game/Art/Materials/";
    private class Data
    {
        public List<Vector3> p=new List<Vector3>(); public List<Face> f=new List<Face>();
        public void T(Vector3 a,Vector3 b,Vector3 c){int i=p.Count;p.Add(a);p.Add(b);p.Add(c);f.Add(new Face(new[]{i,i+1,i+2}));}
        public void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d){T(a,b,c);T(a,c,d);}
    }
    [MenuItem("Tools/Mansao/Instalar lanternas com velas V29")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("GroundFloor_V2/Facade_Details");
        if(parent==null){Tell("Selecione um objeto da mansao.");return;}
        if(parent.Find("Exterior_CandleLanterns_V29")!=null){Tell("A V29 ja existe. Nada foi duplicado.");return;}
        if(Find(mansion,"Central_Facade_Relief_V26")==null || Find(mansion,"Entrance_Door_Reference_V24")==null)
        {Tell("Mantenha a porta V24 e o ressalto V26 ativos.");return;}
        if(Shader.Find("Universal Render Pipeline/Lit")==null || Shader.Find("Universal Render Pipeline/Unlit")==null || !AssetDatabase.IsValidFolder(Materials.TrimEnd('/')))
        {Tell("Shaders URP ou pasta de materiais nao encontrados.");return;}
        string modelPath=Folder+"model.dae";
        ModelImporter importer=AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if(importer==null){Tell("Importe a pasta Assets completa do pacote V29 primeiro.");return;}
        importer.isReadable=true;importer.addCollider=false;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
        importer.SaveAndReimport();
        GameObject asset=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if(asset==null){Tell("A Unity nao importou model.dae. Consulte a Console.");return;}
        Material steel,glass,wax,wick,fire;
        try {PrepareMaterials(out steel,out glass,out wax,out wick,out fire);}
        catch(Exception ex){Debug.LogException(ex);Tell("Nao consegui preparar os materiais. A cena nao foi alterada.");return;}
        float[] xs={-1.35f,1.35f,-2.55f,2.55f};
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Instalar lanternas de vela V29");
        try
        {
            Transform root=Organizer("Exterior_CandleLanterns_V29",parent);
            for(int i=0;i<4;i++)
            {
                bool inner=i<2;float wallZ=inner?-4.94f:-4.80f;
                Transform unit=Organizer((inner?"Lantern_Entry_":"Lantern_Outer_")+(xs[i]<0?"Left":"Right")+"_V29",root);
                GameObject instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,unit);
                Undo.RegisterCreatedObjectUndo(instance,"Instanciar lanterna licenciada");instance.name="VictorianLantern_lpfeffer_V29";
                Bounds metalBounds=BoundsFor(instance.transform,false),glassBounds=BoundsFor(instance.transform,true);
                if(metalBounds.size.y<=.001f || glassBounds.size.y<=.001f)throw new InvalidOperationException("Nao identifiquei as partes steel e glass no modelo importado.");
                // O lado com maior prolongamento alem do vidro e o suporte traseiro.
                float rear=metalBounds.max.z-glassBounds.max.z,front=glassBounds.min.z-metalBounds.min.z;
                if(front>rear)instance.transform.Rotate(Vector3.up,180,Space.World);
                Bounds bounds=AllBounds(instance.transform);
                instance.transform.localScale*=.55f/bounds.size.y;
                bounds=AllBounds(instance.transform);
                instance.transform.position+=new Vector3(xs[i]-bounds.center.x,2.05f-bounds.center.y,wallZ+.003f-bounds.max.z);
                glassBounds=BoundsFor(instance.transform,true);bounds=AllBounds(instance.transform);
                if(bounds.max.z>wallZ+.005f || bounds.max.y>2.34f || bounds.min.y<1.76f)
                    throw new InvalidOperationException("A lanterna excedeu a area externa prevista.");
                CheckOpenings(mansion,bounds,instance.transform);
                foreach(Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    Material[] originals=renderer.sharedMaterials;var mapped=new Material[originals.Length];bool onlyGlass=true;
                    for(int j=0;j<originals.Length;j++)
                    {
                        int kind=PartKind(renderer,j);
                        if(kind==1)mapped[j]=glass;
                        else if(kind==0){mapped[j]=steel;onlyGlass=false;}
                        else throw new InvalidOperationException("Material inesperado no modelo: "+(originals[j]==null?"vazio":originals[j].name));
                    }
                    Undo.RecordObject(renderer,"Aplicar materiais URP");renderer.sharedMaterials=mapped;
                    renderer.shadowCastingMode=onlyGlass?ShadowCastingMode.Off:ShadowCastingMode.On;
                    renderer.receiveShadows=!onlyGlass;
                }
                float candleHeight=Mathf.Min(.13f,glassBounds.size.y*.55f);
                Vector3 basePoint=new Vector3(glassBounds.center.x,glassBounds.min.y+.012f,glassBounds.center.z);
                float top=basePoint.y+candleHeight;
                if(top+.045f>glassBounds.max.y-.01f)throw new InvalidOperationException("A vela nao cabe dentro do vidro.");
                Data holder=new Data(),body=new Data(),thread=new Data();
                Lathe(holder,basePoint,new[]{new Vector2(0,.041f),new Vector2(.010f,.041f),new Vector2(.014f,.031f)},16);
                Lathe(body,basePoint+Vector3.up*.013f,new[]{new Vector2(0,.026f),new Vector2(candleHeight-.013f,.026f),new Vector2(candleHeight-.009f,.023f)},20);
                Lathe(thread,new Vector3(basePoint.x,top-.002f,basePoint.z),new[]{new Vector2(0,.0014f),new Vector2(.008f,.0014f)},8);
                Build("Candle_Holder_V29",holder,unit,steel);Build("Candle_Wax_V29",body,unit,wax);Build("Candle_Wick_V29",thread,unit,wick);
                Transform flame=Organizer("Candle_Flame_V29",unit);flame.position=new Vector3(basePoint.x,top+.003f,basePoint.z);
                Data flameData=new Data();Lathe(flameData,Vector3.zero,new[]{new Vector2(0,.002f),new Vector2(.009f,.008f),new Vector2(.020f,.0065f),new Vector2(.032f,.003f),new Vector2(.041f,0)},12);
                BuildLocal("Flame_Surface_V29",flameData,flame,fire);
                Transform lightObject=Organizer("Candle_PointLight_V29",unit);lightObject.position=flame.position+Vector3.up*.014f;
                Light light=Undo.AddComponent<Light>(lightObject.gameObject);light.type=LightType.Point;light.lightUnit=LightUnit.Lumen;
                light.intensity=12f;light.range=2.4f;light.useColorTemperature=true;light.colorTemperature=2000;light.color=Color.white;
                light.shadows=LightShadows.Hard;light.shadowCustomResolution=256;light.shadowBias=.02f;light.shadowNormalBias=.015f;
                Type extra=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalLightData, Unity.RenderPipelines.Universal.Runtime");
                if(extra!=null){Component component=light.GetComponent(extra);if(component==null)component=Undo.AddComponent(light.gameObject,extra);SerializedObject data=new SerializedObject(component);SerializedProperty tier=data.FindProperty("m_AdditionalLightsShadowResolutionTier");if(tier!=null){tier.intValue=0;data.ApplyModifiedProperties();}}
                MansaoCandleFlickerV29 flicker=Undo.AddComponent<MansaoCandleFlickerV29>(unit.gameObject);
                flicker.candleLight=light;flicker.flame=flame;flicker.intensityLumens=12;flicker.seed=11.3f+i*17.7f;
            }
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root.gameObject;
            Tell("Quatro lanternas com velas instaladas. Luzes de 12 lumens / 2000 K, com oscilacao somente em Play Mode. Luz solar, Bloom e demais objetos preservados. Confira encaixes e vidro antes de salvar. Ctrl+Z desfaz o lote; assets permanecem.");
        }
        catch(Exception ex){Undo.RevertAllDownToGroup(group);Debug.LogException(ex);Tell("O lote foi desfeito devido a um erro. Consulte a Console.");}
    }
    private static Bounds AllBounds(Transform root)
    {Renderer[] renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new InvalidOperationException("Modelo sem Renderer.");Bounds b=renderers[0].bounds;foreach(Renderer r in renderers)b.Encapsulate(r.bounds);return b;}
    private static Bounds BoundsFor(Transform root,bool glass)
    {
        bool has=false;Bounds result=new Bounds();
        foreach(MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            Renderer renderer=filter.GetComponent<Renderer>();if(renderer==null || filter.sharedMesh==null)continue;
            Mesh mesh=filter.sharedMesh;Vector3[] vertices=mesh.vertices;Material[] materials=renderer.sharedMaterials;
            for(int s=0;s<Mathf.Min(materials.Length,mesh.subMeshCount);s++)
            {
                if(PartKind(renderer,s)!=(glass?1:0))continue;
                foreach(int index in mesh.GetTriangles(s)){Vector3 point=filter.transform.TransformPoint(vertices[index]);if(!has){result=new Bounds(point,Vector3.zero);has=true;}else result.Encapsulate(point);}
            }
        }
        return result;
    }
    private static int PartKind(Renderer renderer,int submesh)
    {
        Material[] materials=renderer.sharedMaterials;
        string name=submesh<materials.Length && materials[submesh]!=null?materials[submesh].name.ToLowerInvariant():"";
        if(name.Contains("glass"))return 1;if(name.Contains("steel"))return 0;
        // O DAE fornecido tem 8 triangulos no vidro e 634 no corpo metalico.
        MeshFilter filter=renderer.GetComponent<MeshFilter>();
        if(filter==null || filter.sharedMesh==null || submesh>=filter.sharedMesh.subMeshCount)return -1;
        long count=(long)filter.sharedMesh.GetIndexCount(submesh)/3;
        return count==8?1:count>=600 && count<=700?0:-1;
    }
    private static void CheckOpenings(Transform mansion,Bounds lamp,Transform own)
    {
        foreach(Renderer r in mansion.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.gameObject.activeInHierarchy || r.transform.IsChildOf(own))continue;
            bool opening=false;for(Transform t=r.transform;t!=mansion && t!=null;t=t.parent)
                if(t.name.IndexOf("Window",StringComparison.OrdinalIgnoreCase)>=0 || t.name.IndexOf("Door",StringComparison.OrdinalIgnoreCase)>=0 || t.name.StartsWith("WinterGarden")){opening=true;break;}
            if(opening && lamp.Intersects(r.bounds))throw new InvalidOperationException("Lanterna encontraria uma abertura ou moldura: "+r.name);
        }
    }
    private static void ImportTexture(string path,bool srgb,bool normal,bool readable=false,int size=2048)
    {
        TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new FileNotFoundException(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=srgb;importer.isReadable=readable;
        importer.maxTextureSize=size;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;
        importer.textureCompression=readable?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
    }
    private static Material New(string name,string shader)
    {Material m=AssetDatabase.LoadAssetAtPath<Material>(Materials+name+".mat");if(m!=null)return m;m=new Material(Shader.Find(shader));m.name=name;AssetDatabase.CreateAsset(m,Materials+name+".mat");return m;}
    private static void PrepareMaterials(out Material steel,out Material glass,out Material wax,out Material wick,out Material fire)
    {
        string dir=Folder+"textures/";
        foreach(string name in new[]{"steel_albedo.jpg","glass_albedo.jpg"})ImportTexture(dir+name,true,false);
        foreach(string name in new[]{"steel_normal.png","glass_normal.png"})ImportTexture(dir+name,false,true);
        ImportTexture(dir+"steel_metallic.jpg",false,false,true,1024);ImportTexture(dir+"steel_roughness.jpg",false,false,true,1024);
        string packed=Folder+"steel_metallic_smoothness_V29.png";
        if(!File.Exists(packed))
        {
            Texture2D metallic=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"steel_metallic.jpg"),rough=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"steel_roughness.jpg");
            if(metallic.width!=rough.width || metallic.height!=rough.height)throw new InvalidOperationException("Mapas de metal e rugosidade com tamanhos diferentes.");
            Color32[] a=metallic.GetPixels32(),b=rough.GetPixels32();for(int i=0;i<a.Length;i++)a[i]=new Color32(a[i].r,0,0,(byte)(255-b[i].r));
            Texture2D map=new Texture2D(metallic.width,metallic.height,TextureFormat.RGBA32,false,true);
            try{map.SetPixels32(a);map.Apply();File.WriteAllBytes(packed,map.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(map);}
            AssetDatabase.ImportAsset(packed);
        }
        ImportTexture(packed,false,false,false,1024);
        ImportTexture(dir+"steel_metallic.jpg",false,false,false,1024);ImportTexture(dir+"steel_roughness.jpg",false,false,false,1024);
        steel=New("M_Lantern_Steel_V29","Universal Render Pipeline/Lit");
        steel.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"steel_albedo.jpg"));steel.SetColor("_BaseColor",new Color(.62f,.62f,.62f,1));
        steel.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"steel_normal.png"));steel.SetFloat("_BumpScale",.55f);steel.EnableKeyword("_NORMALMAP");
        steel.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(packed));steel.EnableKeyword("_METALLICSPECGLOSSMAP");steel.SetFloat("_Smoothness",.35f);
        glass=New("M_Lantern_Glass_V29","Universal Render Pipeline/Lit");
        glass.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"glass_albedo.jpg"));glass.SetColor("_BaseColor",new Color(.94f,.97f,1,.12f));
        glass.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"glass_normal.png"));glass.SetFloat("_BumpScale",.12f);glass.EnableKeyword("_NORMALMAP");
        glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0);glass.SetFloat("_Metallic",0);glass.SetFloat("_Smoothness",.18f);glass.SetFloat("_SpecularHighlights",0);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=(int)RenderQueue.Transparent;glass.SetShaderPassEnabled("ShadowCaster",false);
        wax=New("M_Candle_Wax_V29","Universal Render Pipeline/Lit");wax.SetColor("_BaseColor",new Color(.72f,.63f,.44f,1));wax.SetFloat("_Smoothness",.10f);
        wick=New("M_Candle_Wick_V29","Universal Render Pipeline/Lit");wick.SetColor("_BaseColor",new Color(.015f,.012f,.008f,1));wick.SetFloat("_Smoothness",0);
        fire=New("M_Candle_Flame_V29","Universal Render Pipeline/Unlit");fire.SetColor("_BaseColor",new Color(1,.60f,.12f,1));
        foreach(Material m in new[]{steel,glass,wax,wick,fire})EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();
    }
    private static void Lathe(Data d,Vector3 origin,Vector2[] rings,int sides)
    {
        for(int k=0;k<rings.Length-1;k++)for(int j=0;j<sides;j++)
        {float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;d.Q(origin+new Vector3(Mathf.Cos(a)*rings[k].y,rings[k].x,Mathf.Sin(a)*rings[k].y),origin+new Vector3(Mathf.Cos(a)*rings[k+1].y,rings[k+1].x,Mathf.Sin(a)*rings[k+1].y),origin+new Vector3(Mathf.Cos(b)*rings[k+1].y,rings[k+1].x,Mathf.Sin(b)*rings[k+1].y),origin+new Vector3(Mathf.Cos(b)*rings[k].y,rings[k].x,Mathf.Sin(b)*rings[k].y));}
        for(int j=0;j<sides;j++)
        {float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;Vector2 lo=rings[0],hi=rings[rings.Length-1];d.T(origin+Vector3.up*lo.x,origin+new Vector3(Mathf.Cos(a)*lo.y,lo.x,Mathf.Sin(a)*lo.y),origin+new Vector3(Mathf.Cos(b)*lo.y,lo.x,Mathf.Sin(b)*lo.y));if(hi.y>0)d.T(origin+Vector3.up*hi.x,origin+new Vector3(Mathf.Cos(b)*hi.y,hi.x,Mathf.Sin(b)*hi.y),origin+new Vector3(Mathf.Cos(a)*hi.y,hi.x,Mathf.Sin(a)*hi.y));}
    }
    private static void Build(string name,Data d,Transform parent,Material material)
    {Data local=new Data();foreach(Vector3 p in d.p)local.p.Add(parent.InverseTransformPoint(p));local.f=d.f;BuildLocal(name,local,parent,material);}
    private static void BuildLocal(string name,Data d,Transform parent,Material material)
    {
        ProBuilderMesh pb=ProBuilderMesh.Create(d.p,d.f);SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;pb.name=name;
        Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar vela");pb.GetComponent<MeshRenderer>().sharedMaterial=material;pb.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;pb.ToMesh();pb.Refresh();
        MeshCollider collider=pb.GetComponent<MeshCollider>();if(collider!=null)Undo.DestroyObjectImmediate(collider);
    }
    private static Transform Organizer(string name,Transform parent)
    {GameObject go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);go.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(go,"Criar conjunto de lanterna");return go.transform;}
    private static Transform Find(Transform root,string name)
    {foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy)return t;return null;}
    private static void Tell(string text){EditorUtility.DisplayDialog("Lanternas com velas V29",text,"OK");}
}
#endif
