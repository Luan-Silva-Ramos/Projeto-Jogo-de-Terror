#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

// Importar em Assets/Editor. Execucao somente pelo menu.
// Tampas das duas faixas junto ao jardim, sem modificar as lajes nem o vidro.
public static class MansaoRearMiranteCapsV12
{
    [MenuItem("Tools/Mansao/Arrematar faixas junto ao jardim V12")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Show("Saia do Play Mode primeiro."); return; }
        Transform mansion=Selection.activeTransform;
        while(mansion!=null && mansion.name!="Mansion") mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null) { Show("Selecione um objeto da mansao. Exterior_Roofs nao foi encontrado."); return; }
        if(parent.Find("RearMirante_Caps_V12")!=null) { Show("A V12 ja existe. Nenhuma copia foi criada."); return; }

        ProBuilderMesh right=null,left=null,glass=null;
        Renderer band=null;
        int rightCount=0,leftCount=0,glassCount=0;
        foreach(Transform t in mansion.GetComponentsInChildren<Transform>(true))
        {
            if(!t.gameObject.activeInHierarchy) continue;
            if(t.name=="Ceiling_Rear_Mirante_Right") { right=t.GetComponent<ProBuilderMesh>(); rightCount++; }
            if(t.name=="Ceiling_Rear_Mirante_Left") { left=t.GetComponent<ProBuilderMesh>(); leftCount++; }
            if(t.name=="WinterGarden_Roof_Glass") { glass=t.GetComponent<ProBuilderMesh>(); glassCount++; }
            if(t.name=="Timber_Band_Children_Rear") band=t.GetComponent<Renderer>();
        }
        if(rightCount!=1 || leftCount!=1 || glassCount!=1 || right==null || left==null || glass==null)
        { Show("Preciso encontrar uma laje ativa de cada nome e WinterGarden_Roof_Glass ativa. Nada foi alterado."); return; }
        Vector2[] expectedRight={new Vector2(2.75f,11.7f),new Vector2(4.8f,11.7f),new Vector2(4.8f,12f),new Vector2(3.05f,12f)};
        Vector2[] expectedLeft={new Vector2(-4.35f,11.7f),new Vector2(-.75f,11.7f),new Vector2(-1.05f,12f),new Vector2(-4.35f,12f)};
        if(!CheckSlab(right,expectedRight) || !CheckSlab(left,expectedLeft))
        { Show("O contorno real de uma das lajes difere das medidas consultadas. Nada foi alterado."); return; }
        if(band==null || Mathf.Abs(band.bounds.max.z-11.84f)>.015f ||
            Mathf.Abs(band.bounds.min.y-3.70f)>.015f || Mathf.Abs(band.bounds.max.y-3.98f)>.015f)
        { Show("A viga traseira nao esta na posicao conferida. Nada foi alterado."); return; }
        Transform pantry=parent.Find("Roof_Corrections_V06/Roof_Lower_Pantry_V06");
        Renderer pantryRenderer=pantry==null?null:pantry.GetComponent<Renderer>();
        if(pantryRenderer==null || !pantry.gameObject.activeInHierarchy ||
            Mathf.Abs(pantryRenderer.bounds.max.x+4.15f)>.015f)
        { Show("Nao encontrei o telhado baixo da despensa na posicao conferida. Nada foi alterado."); return; }
        Renderer glassRenderer=glass.GetComponent<Renderer>();
        if(glassRenderer==null || Mathf.Abs(glassRenderer.bounds.min.z-11.7f)>.02f ||
            Mathf.Abs(glassRenderer.bounds.max.y-4.35f)>.02f)
        { Show("A cobertura de vidro nao esta nas medidas conferidas. Nada foi alterado."); return; }
        List<Vector2> glassHull=Hull(glass);
        if(glassHull.Count<3) { Show("Nao consegui conferir o contorno da cobertura de vidro. Nada foi alterado."); return; }
        foreach(float[] xyz in new[]{RightCap,LeftCap,WoodEdges,Flashing})
            if(!CheckClearance(xyz,glassHull))
            { Show("Uma peca ultrapassaria o contorno reservado ao vidro ou o limite externo previsto. Nada foi alterado."); return; }

        Material wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Wood_Exterior_PBR.mat");
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        if(wood==null || shader==null || !AssetDatabase.IsValidFolder("Assets/_Game/Art/Materials"))
        { Show("Nao encontrei o material de madeira, a pasta de materiais ou o shader URP/Lit. Nada foi alterado."); return; }
        const string materialPath="Assets/_Game/Art/Materials/M_Roof_Flashing_Dark.mat";
        Material metal=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(metal==null)
        {
            metal=new Material(shader);
            metal.name="M_Roof_Flashing_Dark";
            metal.SetColor("_BaseColor",new Color(.16f,.17f,.18f,1f));
            metal.SetFloat("_Metallic",.45f);
            metal.SetFloat("_Smoothness",.35f);
            AssetDatabase.CreateAsset(metal,materialPath);
        }
        Undo.IncrementCurrentGroup();
        int group=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Arrematar faixas junto ao jardim V12");
        try
        {
            var root=new GameObject("RearMirante_Caps_V12");
            SceneManager.MoveGameObjectToScene(root,right.gameObject.scene);
            root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar arremates V12");
            Build("RearMirante_Cap_Right_V12",RightCap,root.transform,metal);
            Build("RearMirante_Cap_Left_V12",LeftCap,root.transform,metal);
            Build("RearMirante_EdgeTrim_V12",WoodEdges,root.transform,wood);
            Build("RearMirante_Flashing_V12",Flashing,root.transform,metal);
            EditorSceneManager.MarkSceneDirty(right.gameObject.scene);
            Selection.activeGameObject=root;
            Undo.CollapseUndoOperations(group);
            Debug.Log("V12 criada: tampas inclinadas, bordas de madeira e rufos. Lajes, viga, jardim de inverno e demais conjuntos preservados. Contorno do vidro conferido por amostragem.",root);
        }
        catch(System.Exception error)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(error);
            Show("A montagem foi desfeita devido a um erro. Consulte a Console.");
        }
    }

    private static bool CheckSlab(ProBuilderMesh mesh,Vector2[] expected)
    {
        Renderer r=mesh.GetComponent<Renderer>();
        if(r==null || Mathf.Abs(r.bounds.min.y-3.5f)>.01f || Mathf.Abs(r.bounds.max.y-3.7f)>.01f) return false;
        var top=new List<Vector2>();
        foreach(Vector3 p in mesh.positions)
        {
            Vector3 w=mesh.transform.TransformPoint(p);
            if(Mathf.Abs(w.y-3.7f)>.005f) continue;
            Vector2 q=new Vector2(w.x,w.z);
            if(!top.Exists(v=>Vector2.Distance(v,q)<.002f)) top.Add(q);
        }
        if(top.Count!=4) return false;
        foreach(Vector2 p in expected) if(!top.Exists(v=>Vector2.Distance(v,p)<.005f)) return false;
        return true;
    }

    private static float Cross(Vector2 a,Vector2 b,Vector2 c)
    { return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x); }

    private static List<Vector2> Hull(ProBuilderMesh mesh)
    {
        var points=new List<Vector2>();
        foreach(Vector3 p in mesh.positions)
        {
            Vector3 w=mesh.transform.TransformPoint(p);
            Vector2 q=new Vector2(w.x,w.z);
            if(!points.Exists(v=>Vector2.Distance(v,q)<.0001f)) points.Add(q);
        }
        points.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
        var lower=new List<Vector2>();
        foreach(Vector2 p in points)
        {
            while(lower.Count>=2 && Cross(lower[lower.Count-2],lower[lower.Count-1],p)<=.000001f) lower.RemoveAt(lower.Count-1);
            lower.Add(p);
        }
        var upper=new List<Vector2>();
        for(int i=points.Count-1;i>=0;i--)
        {
            Vector2 p=points[i];
            while(upper.Count>=2 && Cross(upper[upper.Count-2],upper[upper.Count-1],p)<=.000001f) upper.RemoveAt(upper.Count-1);
            upper.Add(p);
        }
        if(lower.Count>0) lower.RemoveAt(lower.Count-1);
        if(upper.Count>0) upper.RemoveAt(upper.Count-1);
        lower.AddRange(upper);
        return lower;
    }

    private static bool CheckClearance(float[] xyz,List<Vector2> glass)
    {
        for(int i=0;i<xyz.Length;i+=9)
        {
            Vector3 a=new Vector3(xyz[i],xyz[i+1],xyz[i+2]);
            Vector3 b=new Vector3(xyz[i+3],xyz[i+4],xyz[i+5]);
            Vector3 c=new Vector3(xyz[i+6],xyz[i+7],xyz[i+8]);
            for(int u=0;u<=4;u++) for(int v=0;v<=4-u;v++)
            {
                Vector3 p=a*(1f-(u+v)/4f)+b*(u/4f)+c*(v/4f);
                // Parede interna termina em Z 11.40; teto interno terreo em Y 3.50.
                if(p.z<11.695f || p.y<3.60f) return false;
                bool overGlass=true;
                for(int j=0;j<glass.Count;j++)
                    if(Cross(glass[j],glass[(j+1)%glass.Count],new Vector2(p.x,p.z))<=.0002f)
                    { overGlass=false; break; }
                if(overGlass) return false;
            }
        }
        return true;
    }

    private static void Build(string objectName,float[] xyz,Transform parent,Material material)
    {
        var vertices=new List<Vector3>();
        var faces=new List<Face>();
        for(int i=0;i<xyz.Length;i+=9)
        {
            int first=vertices.Count;
            for(int j=0;j<9;j+=3)
                vertices.Add(parent.InverseTransformPoint(new Vector3(xyz[i+j],xyz[i+j+1],xyz[i+j+2])));
            faces.Add(new Face(new[]{first,first+1,first+2}));
        }
        ProBuilderMesh mesh=ProBuilderMesh.Create(vertices,faces);
        SceneManager.MoveGameObjectToScene(mesh.gameObject,parent.gameObject.scene);
        mesh.name=objectName;
        mesh.transform.SetParent(parent,false);
        mesh.transform.localPosition=Vector3.zero;
        mesh.transform.localRotation=Quaternion.identity;
        mesh.transform.localScale=Vector3.one;
        Undo.RegisterCreatedObjectUndo(mesh.gameObject,"Criar arremate V12");
        mesh.GetComponent<MeshRenderer>().sharedMaterial=material;
        mesh.ToMesh();
        mesh.Refresh();
    }

    private static void Show(string message)
    { EditorUtility.DisplayDialog("Arremates junto ao jardim V12",message,"OK"); }

    private static readonly float[] RightCap = {
        3.05f, 3.72f, 12.0f, 4.8f, 3.72f, 12.0f, 4.8f, 3.76f, 11.7f,
        3.05f, 3.72f, 12.0f, 4.8f, 3.76f, 11.7f, 2.75f, 3.76f, 11.7f,
        2.75f, 3.748f, 11.7f, 4.8f, 3.748f, 11.7f, 4.8f, 3.708f, 12.0f,
        2.75f, 3.748f, 11.7f, 4.8f, 3.708f, 12.0f, 3.05f, 3.708f, 12.0f,
        4.8f, 3.76f, 11.7f, 4.8f, 3.748f, 11.7f, 2.75f, 3.748f, 11.7f,
        4.8f, 3.76f, 11.7f, 2.75f, 3.748f, 11.7f, 2.75f, 3.76f, 11.7f,
        4.8f, 3.72f, 12.0f, 4.8f, 3.708f, 12.0f, 4.8f, 3.748f, 11.7f,
        4.8f, 3.72f, 12.0f, 4.8f, 3.748f, 11.7f, 4.8f, 3.76f, 11.7f,
        3.05f, 3.72f, 12.0f, 3.05f, 3.708f, 12.0f, 4.8f, 3.708f, 12.0f,
        3.05f, 3.72f, 12.0f, 4.8f, 3.708f, 12.0f, 4.8f, 3.72f, 12.0f,
        2.75f, 3.76f, 11.7f, 2.75f, 3.748f, 11.7f, 3.05f, 3.708f, 12.0f,
        2.75f, 3.76f, 11.7f, 3.05f, 3.708f, 12.0f, 3.05f, 3.72f, 12.0f,
    };
    private static readonly float[] LeftCap = {
        -4.145f, 3.72f, 12.0f, -1.05f, 3.72f, 12.0f, -0.75f, 3.76f, 11.7f,
        -4.145f, 3.72f, 12.0f, -0.75f, 3.76f, 11.7f, -4.145f, 3.76f, 11.7f,
        -4.145f, 3.748f, 11.7f, -0.75f, 3.748f, 11.7f, -1.05f, 3.708f, 12.0f,
        -4.145f, 3.748f, 11.7f, -1.05f, 3.708f, 12.0f, -4.145f, 3.708f, 12.0f,
        -0.75f, 3.76f, 11.7f, -0.75f, 3.748f, 11.7f, -4.145f, 3.748f, 11.7f,
        -0.75f, 3.76f, 11.7f, -4.145f, 3.748f, 11.7f, -4.145f, 3.76f, 11.7f,
        -1.05f, 3.72f, 12.0f, -1.05f, 3.708f, 12.0f, -0.75f, 3.748f, 11.7f,
        -1.05f, 3.72f, 12.0f, -0.75f, 3.748f, 11.7f, -0.75f, 3.76f, 11.7f,
        -4.145f, 3.72f, 12.0f, -4.145f, 3.708f, 12.0f, -1.05f, 3.708f, 12.0f,
        -4.145f, 3.72f, 12.0f, -1.05f, 3.708f, 12.0f, -1.05f, 3.72f, 12.0f,
        -4.145f, 3.76f, 11.7f, -4.145f, 3.748f, 11.7f, -4.145f, 3.708f, 12.0f,
        -4.145f, 3.76f, 11.7f, -4.145f, 3.708f, 12.0f, -4.145f, 3.72f, 12.0f,
    };
    private static readonly float[] WoodEdges = {
        2.998f, 3.62f, 11.945f, 4.8f, 3.62f, 11.945f, 4.8f, 3.62f, 12.004f,
        2.998f, 3.62f, 11.945f, 4.8f, 3.62f, 12.004f, 3.057f, 3.62f, 12.004f,
        3.057f, 3.72f, 12.004f, 4.8f, 3.72f, 12.004f, 4.8f, 3.72f, 11.945f,
        3.057f, 3.72f, 12.004f, 4.8f, 3.72f, 11.945f, 2.998f, 3.72f, 11.945f,
        2.998f, 3.62f, 11.945f, 2.998f, 3.72f, 11.945f, 4.8f, 3.72f, 11.945f,
        2.998f, 3.62f, 11.945f, 4.8f, 3.72f, 11.945f, 4.8f, 3.62f, 11.945f,
        4.8f, 3.62f, 11.945f, 4.8f, 3.72f, 11.945f, 4.8f, 3.72f, 12.004f,
        4.8f, 3.62f, 11.945f, 4.8f, 3.72f, 12.004f, 4.8f, 3.62f, 12.004f,
        4.8f, 3.62f, 12.004f, 4.8f, 3.72f, 12.004f, 3.057f, 3.72f, 12.004f,
        4.8f, 3.62f, 12.004f, 3.057f, 3.72f, 12.004f, 3.057f, 3.62f, 12.004f,
        3.057f, 3.62f, 12.004f, 3.057f, 3.72f, 12.004f, 2.998f, 3.72f, 11.945f,
        3.057f, 3.62f, 12.004f, 2.998f, 3.72f, 11.945f, 2.998f, 3.62f, 11.945f,
        -4.145f, 3.62f, 11.945f, -0.998f, 3.62f, 11.945f, -1.057f, 3.62f, 12.004f,
        -4.145f, 3.62f, 11.945f, -1.057f, 3.62f, 12.004f, -4.145f, 3.62f, 12.004f,
        -4.145f, 3.72f, 12.004f, -1.057f, 3.72f, 12.004f, -0.998f, 3.72f, 11.945f,
        -4.145f, 3.72f, 12.004f, -0.998f, 3.72f, 11.945f, -4.145f, 3.72f, 11.945f,
        -4.145f, 3.62f, 11.945f, -4.145f, 3.72f, 11.945f, -0.998f, 3.72f, 11.945f,
        -4.145f, 3.62f, 11.945f, -0.998f, 3.72f, 11.945f, -0.998f, 3.62f, 11.945f,
        -0.998f, 3.62f, 11.945f, -0.998f, 3.72f, 11.945f, -1.057f, 3.72f, 12.004f,
        -0.998f, 3.62f, 11.945f, -1.057f, 3.72f, 12.004f, -1.057f, 3.62f, 12.004f,
        -1.057f, 3.62f, 12.004f, -1.057f, 3.72f, 12.004f, -4.145f, 3.72f, 12.004f,
        -1.057f, 3.62f, 12.004f, -4.145f, 3.72f, 12.004f, -4.145f, 3.62f, 12.004f,
        -4.145f, 3.62f, 12.004f, -4.145f, 3.72f, 12.004f, -4.145f, 3.72f, 11.945f,
        -4.145f, 3.62f, 12.004f, -4.145f, 3.72f, 11.945f, -4.145f, 3.62f, 11.945f,
    };
    private static readonly float[] Flashing = {
        2.901f, 3.73f, 11.845f, 4.8f, 3.73f, 11.845f, 4.8f, 3.73f, 11.855f,
        2.901f, 3.73f, 11.845f, 4.8f, 3.73f, 11.855f, 2.911f, 3.73f, 11.855f,
        2.911f, 3.9f, 11.855f, 4.8f, 3.9f, 11.855f, 4.8f, 3.9f, 11.845f,
        2.911f, 3.9f, 11.855f, 4.8f, 3.9f, 11.845f, 2.901f, 3.9f, 11.845f,
        2.901f, 3.73f, 11.845f, 2.901f, 3.9f, 11.845f, 4.8f, 3.9f, 11.845f,
        2.901f, 3.73f, 11.845f, 4.8f, 3.9f, 11.845f, 4.8f, 3.73f, 11.845f,
        4.8f, 3.73f, 11.845f, 4.8f, 3.9f, 11.845f, 4.8f, 3.9f, 11.855f,
        4.8f, 3.73f, 11.845f, 4.8f, 3.9f, 11.855f, 4.8f, 3.73f, 11.855f,
        4.8f, 3.73f, 11.855f, 4.8f, 3.9f, 11.855f, 2.911f, 3.9f, 11.855f,
        4.8f, 3.73f, 11.855f, 2.911f, 3.9f, 11.855f, 2.911f, 3.73f, 11.855f,
        2.911f, 3.73f, 11.855f, 2.911f, 3.9f, 11.855f, 2.901f, 3.9f, 11.845f,
        2.911f, 3.73f, 11.855f, 2.901f, 3.9f, 11.845f, 2.901f, 3.73f, 11.845f,
        2.996f, 3.746f, 11.94f, 4.8f, 3.746f, 11.94f, 4.8f, 3.757333f, 11.855f,
        2.996f, 3.746f, 11.94f, 4.8f, 3.757333f, 11.855f, 2.911f, 3.757333f, 11.855f,
        2.911f, 3.745333f, 11.855f, 4.8f, 3.745333f, 11.855f, 4.8f, 3.734f, 11.94f,
        2.911f, 3.745333f, 11.855f, 4.8f, 3.734f, 11.94f, 2.996f, 3.734f, 11.94f,
        4.8f, 3.757333f, 11.855f, 4.8f, 3.745333f, 11.855f, 2.911f, 3.745333f, 11.855f,
        4.8f, 3.757333f, 11.855f, 2.911f, 3.745333f, 11.855f, 2.911f, 3.757333f, 11.855f,
        4.8f, 3.746f, 11.94f, 4.8f, 3.734f, 11.94f, 4.8f, 3.745333f, 11.855f,
        4.8f, 3.746f, 11.94f, 4.8f, 3.745333f, 11.855f, 4.8f, 3.757333f, 11.855f,
        2.996f, 3.746f, 11.94f, 2.996f, 3.734f, 11.94f, 4.8f, 3.734f, 11.94f,
        2.996f, 3.746f, 11.94f, 4.8f, 3.734f, 11.94f, 4.8f, 3.746f, 11.94f,
        2.911f, 3.757333f, 11.855f, 2.911f, 3.745333f, 11.855f, 2.996f, 3.734f, 11.94f,
        2.911f, 3.757333f, 11.855f, 2.996f, 3.734f, 11.94f, 2.996f, 3.746f, 11.94f,
        -4.145f, 3.73f, 11.845f, -0.901f, 3.73f, 11.845f, -0.911f, 3.73f, 11.855f,
        -4.145f, 3.73f, 11.845f, -0.911f, 3.73f, 11.855f, -4.145f, 3.73f, 11.855f,
        -4.145f, 3.9f, 11.855f, -0.911f, 3.9f, 11.855f, -0.901f, 3.9f, 11.845f,
        -4.145f, 3.9f, 11.855f, -0.901f, 3.9f, 11.845f, -4.145f, 3.9f, 11.845f,
        -4.145f, 3.73f, 11.845f, -4.145f, 3.9f, 11.845f, -0.901f, 3.9f, 11.845f,
        -4.145f, 3.73f, 11.845f, -0.901f, 3.9f, 11.845f, -0.901f, 3.73f, 11.845f,
        -0.901f, 3.73f, 11.845f, -0.901f, 3.9f, 11.845f, -0.911f, 3.9f, 11.855f,
        -0.901f, 3.73f, 11.845f, -0.911f, 3.9f, 11.855f, -0.911f, 3.73f, 11.855f,
        -0.911f, 3.73f, 11.855f, -0.911f, 3.9f, 11.855f, -4.145f, 3.9f, 11.855f,
        -0.911f, 3.73f, 11.855f, -4.145f, 3.9f, 11.855f, -4.145f, 3.73f, 11.855f,
        -4.145f, 3.73f, 11.855f, -4.145f, 3.9f, 11.855f, -4.145f, 3.9f, 11.845f,
        -4.145f, 3.73f, 11.855f, -4.145f, 3.9f, 11.845f, -4.145f, 3.73f, 11.845f,
        -4.145f, 3.746f, 11.94f, -0.996f, 3.746f, 11.94f, -0.911f, 3.757333f, 11.855f,
        -4.145f, 3.746f, 11.94f, -0.911f, 3.757333f, 11.855f, -4.145f, 3.757333f, 11.855f,
        -4.145f, 3.745333f, 11.855f, -0.911f, 3.745333f, 11.855f, -0.996f, 3.734f, 11.94f,
        -4.145f, 3.745333f, 11.855f, -0.996f, 3.734f, 11.94f, -4.145f, 3.734f, 11.94f,
        -0.911f, 3.757333f, 11.855f, -0.911f, 3.745333f, 11.855f, -4.145f, 3.745333f, 11.855f,
        -0.911f, 3.757333f, 11.855f, -4.145f, 3.745333f, 11.855f, -4.145f, 3.757333f, 11.855f,
        -0.996f, 3.746f, 11.94f, -0.996f, 3.734f, 11.94f, -0.911f, 3.745333f, 11.855f,
        -0.996f, 3.746f, 11.94f, -0.911f, 3.745333f, 11.855f, -0.911f, 3.757333f, 11.855f,
        -4.145f, 3.746f, 11.94f, -4.145f, 3.734f, 11.94f, -0.996f, 3.734f, 11.94f,
        -4.145f, 3.746f, 11.94f, -0.996f, 3.734f, 11.94f, -0.996f, 3.746f, 11.94f,
        -4.145f, 3.757333f, 11.855f, -4.145f, 3.745333f, 11.855f, -4.145f, 3.734f, 11.94f,
        -4.145f, 3.757333f, 11.855f, -4.145f, 3.734f, 11.94f, -4.145f, 3.746f, 11.94f,
        -4.145f, 3.757f, 11.7f, -4.145f, 3.86f, 11.7f, -4.145f, 3.82f, 12.0f,
        -4.145f, 3.757f, 11.7f, -4.145f, 3.82f, 12.0f, -4.145f, 3.717f, 12.0f,
        -4.157f, 3.717f, 12.0f, -4.157f, 3.82f, 12.0f, -4.157f, 3.86f, 11.7f,
        -4.157f, 3.717f, 12.0f, -4.157f, 3.86f, 11.7f, -4.157f, 3.757f, 11.7f,
        -4.145f, 3.757f, 11.7f, -4.157f, 3.757f, 11.7f, -4.157f, 3.86f, 11.7f,
        -4.145f, 3.757f, 11.7f, -4.157f, 3.86f, 11.7f, -4.145f, 3.86f, 11.7f,
        -4.145f, 3.86f, 11.7f, -4.157f, 3.86f, 11.7f, -4.157f, 3.82f, 12.0f,
        -4.145f, 3.86f, 11.7f, -4.157f, 3.82f, 12.0f, -4.145f, 3.82f, 12.0f,
        -4.145f, 3.82f, 12.0f, -4.157f, 3.82f, 12.0f, -4.157f, 3.717f, 12.0f,
        -4.145f, 3.82f, 12.0f, -4.157f, 3.717f, 12.0f, -4.145f, 3.717f, 12.0f,
        -4.145f, 3.717f, 12.0f, -4.157f, 3.717f, 12.0f, -4.157f, 3.757f, 11.7f,
        -4.145f, 3.717f, 12.0f, -4.157f, 3.757f, 11.7f, -4.145f, 3.757f, 11.7f,
    };
}
#endif

