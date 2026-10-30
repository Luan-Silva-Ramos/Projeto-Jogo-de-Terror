#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoOgivalWindowV27
{
    private const float R=.85f, Spring=5.14f, Rise=1.40f, Bottom=4.10f;
    private class Data
    {
        public List<Vector3> p=new List<Vector3>();public List<Face> f=new List<Face>();
        public void T(Vector3 a,Vector3 b,Vector3 c){int i=p.Count;p.Add(a);p.Add(b);p.Add(c);f.Add(new Face(new[]{i,i+1,i+2}));}
        public void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d){T(a,b,c);T(a,c,d);}
    }
    private static float Arch(float x,float r,float rise,float spring)
    {float radius=(rise*rise+r*r)/(2*r),center=radius-r;return spring+Mathf.Sqrt(Mathf.Max(0,radius*radius-Mathf.Pow(Mathf.Abs(x)+center,2)));}
    private static float Height(float x){return Arch(x,R,Rise,Spring);}
    [MenuItem("Tools/Mansao/Criar janela ogival central V27")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("UpperFloor_V2/Facade_Details");
        if(parent==null){Tell("Selecione um objeto da mansao.");return;}
        if(Find(mansion,"Central_OgivalWindow_V27",false)!=null){Tell("A V27 ja existe. Nada foi duplicado.");return;}
        Transform old=Find(mansion,"PF_Window_Front_Ornate",true),ceiling=Find(mansion,"Ceiling_Upper_Atrium",true),relief=Find(mansion,"Central_Facade_Relief_V26",true);
        Renderer cr=ceiling==null?null:ceiling.GetComponent<Renderer>();
        if(old==null || cr==null || relief==null || Mathf.Abs(cr.bounds.min.y-7.2f)>.015f)
        {Tell("Mantenha a janela original, o teto do atrio em Y 7.20 e o ressalto V26 ativos. Nada foi alterado.");return;}
        // Confere o contorno real do vao antes de usar os pontos registrados.
        Transform above=Find(mansion,"Wall_Upper_Atrium_Window_Above_New",true),below=Find(mansion,"Wall_Upper_Atrium_Window_Below",true);
        ProBuilderMesh mesh=above==null?null:above.GetComponent<ProBuilderMesh>();Renderer belowRenderer=below==null?null:below.GetComponent<Renderer>();
        Vector2[] oldArch={new Vector2(-.95f,6f),new Vector2(-.8f,6.35f),new Vector2(-.45f,6.58f),new Vector2(0,6.65f),new Vector2(.45f,6.58f),new Vector2(.8f,6.35f),new Vector2(.95f,6f)};
        if(mesh==null || belowRenderer==null || Mathf.Abs(belowRenderer.bounds.max.y-4.05f)>.015f)
        {Tell("Nao confirmei o peitoril e o topo do vao existente. Nada foi alterado.");return;}
        foreach(Vector2 p in oldArch)
        {
            bool found=false;foreach(Vector3 v in mesh.positions)if(Vector3.Distance(mesh.transform.TransformPoint(v),new Vector3(p.x,p.y,-4.8f))<.015f){found=true;break;}
            if(!found){Tell("O arco original difere das coordenadas consultadas. Nada foi alterado.");return;}
        }
        Material wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Entrance_Reference_Wood_V24.mat"),
            plaster=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Plaster_Exterior.mat"),
            glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Glass_Atrium.mat"),
            lead=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Roof_Seam_V20.mat");
        if(wood==null || plaster==null || glass==null || lead==null){Tell("Faltam os materiais existentes de madeira V24, reboco, vidro ou metal V20.");return;}
        var shape=new List<Vector2>{new Vector2(-R,Bottom),new Vector2(R,Bottom),new Vector2(R,Spring)};
        for(int i=1;i<=40;i++){float x=R-2*R*i/40;shape.Add(new Vector2(x,Height(x)));}
        Data frame=new Data(),panes=new Data(),tracery=new Data(),leads=new Data(),fill=new Data();
        Ring(frame,shape,0,.095f,-5.08f,-4.93f);Ring(frame,shape,.09f,.125f,-5.12f,-5.075f);
        Box(frame,-1.05f,1.05f,3.98f,4.10f,-5.18f,-4.93f);
        Solid(panes,shape,-4.965f,-4.955f);
        foreach(float x in new[]{-.425f,0,.425f})Box(tracery,x-.018f,x+.018f,Bottom,Height(x)-.018f,-5.025f,-4.95f);
        foreach(float center in new[]{-.425f,.425f})
        {
            var curve=new List<Vector2>();for(int i=0;i<=32;i++){float x=-.425f+.85f*i/32;curve.Add(new Vector2(center+x,Arch(x,.425f,.96f,Spring)));}
            Strip(tracery,curve,.026f,-5.03f,-4.95f);
        }
        foreach(float x in new[]{-.6375f,-.2125f,.2125f,.6375f})Box(leads,x-.006f,x+.006f,Bottom,Height(x)-.015f,-4.99f,-4.955f);
        foreach(float y in new[]{4.65f,5.14f,5.65f})
        {
            float radius=(Rise*Rise+R*R)/(2*R),center=radius-R;
            float half=y<=Spring?R:Mathf.Sqrt(radius*radius-(y-Spring)*(y-Spring))-center;
            Box(leads,-half+.012f,half-.012f,y-.006f,y+.006f,-4.99f,-4.955f);
        }
        // Fecha os cantos do antigo vao arredondado; nenhuma parede original e cortada.
        Box(fill,-.95f,.95f,4.05f,Bottom,-4.94f,-4.5f);
        var xs=new List<float>{-.95f,-.85f,-.8f,-.45f,0,.45f,.8f,.85f,.95f};
        for(int i=1;i<40;i++)xs.Add(R-2*R*i/40);xs.Sort();
        for(int i=0;i<xs.Count-1;i++)
        {
            float a=xs[i],b=xs[i+1];if(b-a<.00001f)continue;
            bool side=(a+b)*.5f< -R || (a+b)*.5f>R;
            float ya=side?Bottom:Height(a),yb=side?Bottom:Height(b);
            float topA=OldHeight(a,oldArch),topB=OldHeight(b,oldArch);
            if(ya>topA+.001f || yb>topB+.001f){Tell("A nova janela excederia o vao original. Nada foi alterado.");return;}
            Solid(fill,new List<Vector2>{new Vector2(a,ya),new Vector2(b,yb),new Vector2(b,topB),new Vector2(a,topA)},-4.94f,-4.5f);
        }
        foreach(Data d in new[]{frame,panes,tracery,leads,fill})foreach(Vector3 p in d.p)
            if(p.y>=6.80f || p.z> -4.499f){Tell("Uma peca excedeu os limites de altura ou profundidade. Nada foi alterado.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Criar janela ogival V27");
        try
        {
            GameObject root=new GameObject("Central_OgivalWindow_V27");SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);root.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(root,"Criar janela ogival");
            Build("Ogival_TimberFrame_V27",frame,root.transform,wood,false);
            Build("Ogival_ClearGlass_V27",panes,root.transform,glass,false);
            Build("Ogival_BranchingMullions_V27",tracery,root.transform,wood,false);
            Build("Ogival_ThinLeads_V27",leads,root.transform,lead,false);
            Build("Ogival_OriginalOpeningInfill_V27",fill,root.transform,plaster,true);
            // Collider unico transparente cobre toda a abertura e bloqueia passagem.
            GameObject block=new GameObject("Ogival_WindowCollider_V27");block.transform.SetParent(root.transform,false);Undo.RegisterCreatedObjectUndo(block,"Criar collider da janela");
            BoxCollider collider=Undo.AddComponent<BoxCollider>(block);Vector3 center=root.transform.InverseTransformPoint(new Vector3(0,5.35f,-4.72f));collider.center=center;collider.size=new Vector3(1.9f,2.6f,.44f);
            Undo.RecordObject(old.gameObject,"Preservar janela anterior");old.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;
            Tell("Janela ogival criada abaixo do teto. Janela antiga preservada desativada; paredes, teto e V26 continuam ativos. Confira de fora e de dentro. Vidro sem emissao: iluminacao por velas fica para a etapa de luz. Ctrl+Z desfaz o lote.");
        }
        catch(System.Exception ex){Undo.RevertAllDownToGroup(group);Debug.LogException(ex);Tell("O lote foi desfeito devido a um erro. Consulte a Console.");}
    }
    private static float OldHeight(float x,Vector2[] arc)
    {for(int i=0;i<arc.Length-1;i++)if(x>=arc[i].x-.00001f && x<=arc[i+1].x+.00001f)return Mathf.Lerp(arc[i].y,arc[i+1].y,(x-arc[i].x)/(arc[i+1].x-arc[i].x));return 6;}
    private static Transform Find(Transform root,string name,bool active)
    {foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && (!active || t.gameObject.activeInHierarchy))return t;return null;}
    private static Vector2[] Offset(List<Vector2> p,float distance)
    {
        var result=new Vector2[p.Count];for(int i=0;i<p.Count;i++){Vector2 a=(p[i]-p[(i+p.Count-1)%p.Count]).normalized,b=(p[(i+1)%p.Count]-p[i]).normalized;Vector2 na=new Vector2(a.y,-a.x),nb=new Vector2(b.y,-b.x);result[i]=p[i]+(na+nb)*distance/(1+Vector2.Dot(na,nb));}return result;
    }
    private static void Ring(Data d,List<Vector2> shape,float start,float end,float front,float back)
    {
        Vector2[] a=Offset(shape,start),b=Offset(shape,end);
        for(int i=0;i<shape.Count;i++){int j=(i+1)%shape.Count;Solid(d,new List<Vector2>{a[i],b[i],b[j],a[j]},front,back);}
    }
    private static void Strip(Data d,List<Vector2> curve,float width,float front,float back)
    {
        for(int i=0;i<curve.Count-1;i++){Vector2 a=curve[i],b=curve[i+1],v=(b-a).normalized,n=new Vector2(-v.y,v.x)*width*.5f;Solid(d,new List<Vector2>{a-n,b-n,b+n,a+n},front,back);}
    }
    private static void Box(Data d,float x0,float x1,float y0,float y1,float z0,float z1)
    {Solid(d,new List<Vector2>{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)},z0,z1);}
    private static void Solid(Data d,List<Vector2> polygon,float front,float back)
    {
        // Os perfis de vidro sao convexos; as pequenas faixas sao quadrilateros.
        float area=0;for(int i=0;i<polygon.Count;i++)area+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);if(Mathf.Abs(area)<.0000001f)return;if(area<0)polygon.Reverse();
        for(int i=1;i<polygon.Count-1;i++){d.T(V(polygon[0],front),V(polygon[i+1],front),V(polygon[i],front));d.T(V(polygon[0],back),V(polygon[i],back),V(polygon[i+1],back));}
        for(int i=0;i<polygon.Count;i++){int j=(i+1)%polygon.Count;d.Q(V(polygon[i],front),V(polygon[j],front),V(polygon[j],back),V(polygon[i],back));}
    }
    private static Vector3 V(Vector2 p,float z){return new Vector3(p.x,p.y,z);}
    private static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
    private static void Build(string name,Data d,Transform parent,Material material,bool collider)
    {
        var local=new List<Vector3>();var uv=new List<Vector2>();foreach(Vector3 p in d.p){local.Add(parent.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.y));}foreach(Face face in d.f)face.manualUV=true;
        ProBuilderMesh pb=ProBuilderMesh.Create(local,d.f);SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;pb.name=name;
        Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar parte da janela");pb.GetComponent<MeshRenderer>().sharedMaterial=material;pb.textures=uv;pb.ToMesh();pb.Refresh();
        MeshCollider mc=pb.GetComponent<MeshCollider>();if(collider){if(mc==null)mc=Undo.AddComponent<MeshCollider>(pb.gameObject);mc.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;}else if(mc!=null)Undo.DestroyObjectImmediate(mc);
    }
    private static void Tell(string text){EditorUtility.DisplayDialog("Janela ogival V27",text,"OK");}
}
#endif
