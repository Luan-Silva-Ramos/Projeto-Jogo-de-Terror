#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoEntranceCrownV23
{
    private class Data
    {
        public List<Vector3> vertices=new List<Vector3>();public List<Face> faces=new List<Face>();
        public void Tri(Vector3 a,Vector3 b,Vector3 c){int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);faces.Add(new Face(new[]{i,i+1,i+2}));}
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
    }
    [MenuItem("Tools/Mansao/Ornamentar sobreporta principal V23")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("GroundFloor_V2/Facade_Details");
        if(parent==null){Tell("Selecione um objeto da mansao.");return;}
        if(parent.Find("Entrance_Crown_V23")!=null){Tell("A V23 ja existe. Nada foi duplicado.");return;}
        Transform door=Find(mansion,"Door_Entrance_Assembly");
        Transform frame=door==null?null:Find(door,"PF_EntranceDoor_Classic");
        Renderer frameRenderer=frame==null?null:frame.GetComponent<Renderer>();
        if(frameRenderer==null){Tell("Nao encontrei a moldura ativa do prefab Door_Entrance_Assembly. Nada foi alterado.");return;}
        Bounds expected=new Bounds();expected.SetMinMax(new Vector3(-1.942f,.020f,-5.014f),new Vector3(1.942f,2.905f,-4.686f));
        if(Vector3.Distance(frameRenderer.bounds.min,expected.min)>.05f || Vector3.Distance(frameRenderer.bounds.max,expected.max)>.05f)
        {Tell("As medidas da moldura diferem das consultadas. Nada foi alterado. Min: "+frameRenderer.bounds.min.ToString("F3")+"; Max: "+frameRenderer.bounds.max.ToString("F3"));return;}
        Transform wall=Find(mansion,"Wall_Entrance_AboveDoor"),band=Find(mansion,"Facade_Atrium_FloorBand");
        Renderer wr=wall==null?null:wall.GetComponent<Renderer>(),br=band==null?null:band.GetComponent<Renderer>();
        if(wr==null || br==null || Mathf.Abs(wr.bounds.min.z+4.8f)>.02f || br.bounds.min.y<3.44f || br.bounds.min.y>3.48f)
        {Tell("A parede ou a faixa superior diferem das medidas previstas. Nada foi alterado.");return;}
        // A moldura antiga manual nao e usada como referencia e nao sera alterada.
        Material wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Wood_Exterior_PBR.mat");
        Material plaster=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Plaster_Exterior.mat");
        if(wood==null || plaster==null){Tell("Materiais de madeira ou reboco nao encontrados.");return;}
        Data crown=new Data(),supports=new Data(),medallion=new Data(),inset=new Data();
        Box(crown,-2.25f,2.25f,2.970f,3.030f,-5.120f,-4.790f);
        Box(crown,-2.32f,2.32f,3.028f,3.135f,-5.230f,-4.790f);
        Box(crown,-2.40f,2.40f,3.133f,3.190f,-5.290f,-4.790f);
        foreach(float x in new[]{-2.08f,2.08f})
        {
            Box(supports,x-.10f,x+.10f,2.42f,2.98f,-4.94f,-4.79f);
            Corbel(supports,x);
        }
        Oval(medallion,.13f,.14f,3.28f,-5.155f,-4.79f);
        Oval(inset,.083f,.099f,3.28f,-5.175f,-5.150f);
        foreach(Data data in new[]{crown,supports,medallion,inset})foreach(Vector3 p in data.vertices)
        {
            if(p.z> -4.789f || p.y>3.421f || p.y<2.419f || p.y<2.94f && Mathf.Abs(p.x)<1.97f)
            {Tell("Uma peca excedeu a area externa prevista. Nada foi alterado.");return;}
        }
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Ornamentar sobreporta V23");
        try
        {
            GameObject root=new GameObject("Entrance_Crown_V23");SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);root.transform.SetParent(parent,false);
            Undo.RegisterCreatedObjectUndo(root,"Criar sobreporta externa");
            Build("Entrance_Cornice_ThreeLevels_V23",crown,root.transform,wood);
            Build("Entrance_SideCorbels_V23",supports,root.transform,wood);
            Build("Entrance_Medallion_Frame_V23",medallion,root.transform,wood);
            Build("Entrance_Medallion_Inset_V23",inset,root.transform,plaster);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;
            Tell("Sobreporta criada. Porta, pivos, scripts, colliders e moldura existentes preservados. Nenhum collider novo adicionado. Confira o encaixe visual e a abertura antes de salvar. Ctrl+Z desfaz o lote.");
        }
        catch(System.Exception ex){Undo.RevertAllDownToGroup(group);Debug.LogException(ex);Tell("O lote foi desfeito devido a um erro. Consulte a Console.");}
    }
    private static Transform Find(Transform root,string name)
    {foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy)return t;return null;}
    private static void Box(Data d,float x0,float x1,float y0,float y1,float z0,float z1)
    {
        Vector3 a=new Vector3(x0,y0,z0),b=new Vector3(x1,y0,z0),c=new Vector3(x1,y1,z0),e=new Vector3(x0,y1,z0);
        Vector3 f=new Vector3(x0,y0,z1),g=new Vector3(x1,y0,z1),h=new Vector3(x1,y1,z1),j=new Vector3(x0,y1,z1);
        d.Quad(e,c,b,a);d.Quad(f,g,h,j);d.Quad(a,b,g,f);d.Quad(c,e,j,h);d.Quad(a,f,j,e);d.Quad(b,c,h,g);
    }
    private static void Corbel(Data d,float x)
    {
        Vector2[] yz={new Vector2(2.50f,-4.935f),new Vector2(2.50f,-4.79f),new Vector2(2.975f,-4.79f),
            new Vector2(2.975f,-5.20f),new Vector2(2.925f,-5.20f),new Vector2(2.89f,-5.12f),
            new Vector2(2.83f,-5.045f),new Vector2(2.75f,-4.99f),new Vector2(2.65f,-4.95f)};
        // Leque a partir do canto superior traseiro, dentro do perfil concavo.
        for(int i=1;i<yz.Length-1;i++)
        {
            int a=2,b=(2+i)%yz.Length,c=(2+i+1)%yz.Length;
            d.Tri(new Vector3(x-.085f,yz[a].x,yz[a].y),new Vector3(x-.085f,yz[b].x,yz[b].y),new Vector3(x-.085f,yz[c].x,yz[c].y));
            d.Tri(new Vector3(x+.085f,yz[a].x,yz[a].y),new Vector3(x+.085f,yz[c].x,yz[c].y),new Vector3(x+.085f,yz[b].x,yz[b].y));
        }
        for(int i=0;i<yz.Length;i++)
        {
            Vector2 a=yz[i],b=yz[(i+1)%yz.Length];
            d.Quad(new Vector3(x-.085f,a.x,a.y),new Vector3(x+.085f,a.x,a.y),new Vector3(x+.085f,b.x,b.y),new Vector3(x-.085f,b.x,b.y));
        }
    }
    private static void Oval(Data d,float rx,float ry,float cy,float front,float back)
    {
        const int sides=32;
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
            Vector3 af=new Vector3(rx*Mathf.Cos(a),cy+ry*Mathf.Sin(a),front),bf=new Vector3(rx*Mathf.Cos(b),cy+ry*Mathf.Sin(b),front);
            Vector3 ab=new Vector3(af.x,af.y,back),bb=new Vector3(bf.x,bf.y,back);
            d.Tri(new Vector3(0,cy,front),bf,af);d.Tri(new Vector3(0,cy,back),ab,bb);d.Quad(af,bf,bb,ab);
        }
    }
    private static void Build(string name,Data d,Transform parent,Material material)
    {
        var local=new List<Vector3>();foreach(Vector3 p in d.vertices)local.Add(parent.InverseTransformPoint(p));
        ProBuilderMesh pb=ProBuilderMesh.Create(local,d.faces);SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);
        pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;
        pb.name=name;Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar ornamento de entrada");pb.GetComponent<MeshRenderer>().sharedMaterial=material;pb.ToMesh();pb.Refresh();
        MeshCollider collider=pb.GetComponent<MeshCollider>();if(collider!=null)Undo.DestroyObjectImmediate(collider);
    }
    private static void Tell(string text){EditorUtility.DisplayDialog("Sobreporta V23",text,"OK");}
}
#endif
