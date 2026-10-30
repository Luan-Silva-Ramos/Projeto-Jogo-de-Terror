#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MansaoDemoPrototypeV37
{
    const string Root="DEMO_TESTE_TEMPORARIO_V37",Folder="Assets/_Game/Art/Materials/DemoBlockoutV37/";
    [MenuItem("Tools/Mansao/Demo/Criar teste de percurso e galeria V37")]
    static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Mansion();if(mansion==null){Tell("Selecione Mansion ou um objeto dentro dela.");return;}
        if(mansion.Find(Root)!=null){Tell("O teste V37 ja existe. Nenhuma copia foi criada.");return;}
        int group=-1;
        try
        {
            Check(mansion,"Floor_Atrium",new Vector3(-4.2f,-.2f,-4.7f),new Vector3(4.2f,0,4.7f));
            Check(mansion,"Floor_Library",new Vector3(4.2f,-.2f,-1f),new Vector3(10.4f,0,6.4f));
            Check(mansion,"Floor_Gallery_Rear",new Vector3(4.2f,-.2f,6.4f),new Vector3(12.2f,0,8.2f));
            Check(mansion,"Wall_Office_Back_AboveDoor",new Vector3(6.65f,2.2f,-1),new Vector3(7.75f,3.5f,-.8f));
            if(Vector3.Distance(mansion.lossyScale,Vector3.one)>.001f)throw new InvalidOperationException("Mansion precisa manter escala mundial 1 para este teste.");
            Camera camera=null;foreach(GameObject root in mansion.gameObject.scene.GetRootGameObjects())foreach(Camera c in root.GetComponentsInChildren<Camera>())if(c.enabled && c.CompareTag("MainCamera"))camera=c;
            if(camera==null)throw new InvalidOperationException("Main Camera ativa nao encontrada.");
            Font font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(font==null)throw new InvalidOperationException("Fonte de teste nao encontrada.");
            EnsureFolder(Folder.TrimEnd('/'));
            Material orange=Mat("Route",new Color(.9f,.32f,.08f)),purple=Mat("Gallery",new Color(.48f,.35f,.8f)),red=Mat("Shortcut",new Color(.75f,.06f,.08f)),gold=Mat("Staging",new Color(.75f,.52f,.12f)),gray=Mat("Furniture",new Color(.25f,.29f,.33f));
            Material[] portraits={Mat("Charles",new Color(.35f,.48f,.65f)),Mat("Eleanor",new Color(.35f,.60f,.48f)),Mat("David",new Color(.64f,.47f,.28f)),Mat("Clara",new Color(.58f,.36f,.58f))};
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Criar prototipo temporario V37");
            GameObject rootObject=new GameObject(Root);SceneManager.MoveGameObjectToScene(rootObject,mansion.gameObject.scene);rootObject.transform.SetParent(mansion,false);Undo.RegisterCreatedObjectUndo(rootObject,"Criar conjunto temporario");
            Transform rootT=rootObject.transform;
            Cube(rootT,"Atalho_Bloqueado_Somente_No_Teste",new Vector3(7.2f,1.05f,-.9f),new Vector3(1.10f,2.10f,.20f),red,true);
            Text(rootT,"Legenda_Atalho","ATALHO BLOQUEADO\nSOMENTE NO TESTE",new Vector3(7.2f,1.45f,-1.015f),Quaternion.identity,.025f,font);
            Vector3[] route={new Vector3(7.6f,.018f,-5),new Vector3(5,.018f,-2),new Vector3(3.55f,.018f,-2),new Vector3(0,.018f,-2),new Vector3(0,.018f,2.85f),new Vector3(3.6f,.018f,2.85f),new Vector3(3.6f,.018f,2),new Vector3(4.65f,.018f,2)};
            for(int i=0;i<route.Length-1;i++)Line(rootT,"Percurso_Fuga_"+i,route[i],route[i+1],orange);
            Line(rootT,"Acesso_Galeria",new Vector3(7.2f,.020f,3.2f),new Vector3(7.2f,.020f,6.9f),purple);
            Cube(rootT,"Reserva_Soleira_Sal",new Vector3(4.10f,.025f,2),new Vector3(.06f,.018f,1.0f),gold,false);
            Cube(rootT,"Chegada_Biblioteca",new Vector3(4.65f,.026f,2),new Vector3(.65f,.016f,1f),purple,false);
            string[] names={"CHARLES","ELEANOR","DAVID","CLARA"};
            for(int i=0;i<4;i++)
            {
                float x=5.0f+.90f*i;
                Cube(rootT,"Retrato_Temporario_"+names[i],new Vector3(x,1.65f,7.955f),new Vector3(.65f,.95f,.05f),portraits[i],false);
                Text(rootT,"Nome_Retrato_"+names[i],names[i],new Vector3(x,1.65f,7.925f),Quaternion.identity,.022f,font);
            }
            Cube(rootT,"Lareira_Volume_Com_Colisao",new Vector3(9.34f,.72f,7.20f),new Vector3(.30f,1.44f,1.15f),gray,true);
            Text(rootT,"Nome_Lareira","LAREIRA\nVOLUME DE TESTE",new Vector3(9.175f,1.10f,7.20f),Quaternion.Euler(0,90,0),.021f,font);
            Chair(rootT,"Cadeira_Galeria_Teste",new Vector3(5.10f,0,6.82f),gray);
            Chair(rootT,"Reserva_Cadeira_Bess_Hall",new Vector3(-3.25f,0,-3.4f),gold);
            Text(rootT,"Nome_Bess","RESERVA BESS",new Vector3(-3.25f,1.15f,-3.70f),Quaternion.identity,.023f,font);
            Cube(rootT,"Reserva_Diario_Terceiro_Degrau",new Vector3(3.10f,.758f,-.61f),new Vector3(.24f,.03f,.18f),gold,false);
            var timer=Undo.AddComponent<MansaoDemoWalkTestV37>(rootObject);timer.playerCamera=camera;
            EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=rootObject;
            Debug.Log("Prototipo V37 criado. Objetos temporarios, sem alteracoes na construcao ou iluminacao. F6 inicia o cronometro dentro do escritorio; entrada marcada da biblioteca encerra a medicao. Use o menu Remover teste V37 ao terminar.",rootObject);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi aplicado: "+e.Message);}
    }
    [MenuItem("Tools/Mansao/Demo/Remover teste de percurso e galeria V37")]
    static void Remove()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Mansion(),root=mansion==null?null:mansion.Find(Root);if(root==null){Tell("Selecione Mansion com o conjunto temporario V37.");return;}
        Undo.DestroyObjectImmediate(root.gameObject);EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);
    }
    static void Chair(Transform root,string name,Vector3 basePoint,Material material)
    {
        GameObject unit=new GameObject(name);SceneManager.MoveGameObjectToScene(unit,root.gameObject.scene);unit.transform.SetParent(root,false);unit.transform.position=basePoint;unit.transform.rotation=Quaternion.identity;Undo.RegisterCreatedObjectUndo(unit,"Criar cadeira temporaria");
        Cube(unit.transform,"Assento",basePoint+new Vector3(0,.45f,0),new Vector3(.6f,.08f,.6f),material,false);
        Cube(unit.transform,"Encosto",basePoint+new Vector3(0,.75f,.26f),new Vector3(.6f,.55f,.08f),material,false);
        foreach(float x in new[]{-.23f,.23f})foreach(float z in new[]{-.23f,.23f})Cube(unit.transform,"Pe",basePoint+new Vector3(x,.225f,z),new Vector3(.06f,.45f,.06f),material,false);
        BoxCollider c=Undo.AddComponent<BoxCollider>(unit);c.center=new Vector3(0,.51f,0);c.size=new Vector3(.6f,1.02f,.6f);
    }
    static GameObject Cube(Transform parent,string name,Vector3 world,Vector3 size,Material material,bool collider)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);go.name=name;go.transform.SetParent(parent,false);go.transform.position=world;go.transform.rotation=Quaternion.identity;go.transform.localScale=size;Undo.RegisterCreatedObjectUndo(go,"Criar volume de teste");go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;if(!collider)Undo.DestroyObjectImmediate(go.GetComponent<BoxCollider>());return go;
    }
    static void Line(Transform parent,string name,Vector3 a,Vector3 b,Material material)
    {
        GameObject go=Cube(parent,name,(a+b)*.5f,new Vector3(.045f,.012f,Vector3.Distance(a,b)),material,false);go.transform.rotation=Quaternion.LookRotation((b-a).normalized,Vector3.up);
    }
    static void Text(Transform parent,string name,string text,Vector3 world,Quaternion rotation,float size,Font font)
    {
        GameObject go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);go.transform.SetParent(parent,false);go.transform.position=world;go.transform.rotation=rotation;Undo.RegisterCreatedObjectUndo(go,"Criar legenda temporaria");TextMesh t=Undo.AddComponent<TextMesh>(go);t.font=font;t.text=text;t.fontSize=48;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=Color.white;MeshRenderer r=go.GetComponent<MeshRenderer>();r.sharedMaterial=font.material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        Bounds bounds=r.bounds;float width=Mathf.Max(bounds.size.x,bounds.size.z);if(width>.00001f && bounds.size.y>.00001f)go.transform.localScale*=Mathf.Min(size*30/width,size*14/bounds.size.y);
    }
    static void Check(Transform root,string name,Vector3 min,Vector3 max)
    {
        Renderer result=null;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy){if(result!=null)throw new InvalidOperationException("Nome ativo duplicado: "+name);result=t.GetComponent<Renderer>();}
        if(result==null || Vector3.Distance(result.bounds.min,min)>.03f || Vector3.Distance(result.bounds.max,max)>.03f)throw new InvalidOperationException("As medidas de "+name+" diferem da cena conferida.");
    }
    static Material Mat(string name,Color color)
    {
        string path=Folder+"M_Demo_"+name+"_V37.mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;Shader shader=Shader.Find("Universal Render Pipeline/Unlit");if(shader==null)throw new InvalidOperationException("Shader URP/Unlit nao encontrado.");m=new Material(shader);m.name="M_Demo_"+name+"_V37";m.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(m,path);return m;
    }
    static Transform Mansion(){Transform t=Selection.activeTransform;while(t!=null && t.name!="Mansion")t=t.parent;return t;}
    static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');EnsureFolder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
    static void Tell(string message){EditorUtility.DisplayDialog("Prototipo temporario V37",message,"OK");}
}
#endif

