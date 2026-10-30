#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoCentralBayV26
{
    private class Data
    {
        public List<Vector3> p=new List<Vector3>(); public List<Face> f=new List<Face>();public float area;
        public void T(Vector3 a,Vector3 b,Vector3 c){int i=p.Count;p.Add(a);p.Add(b);p.Add(c);f.Add(new Face(new[]{i,i+1,i+2}));}
        public void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d){T(a,b,c);T(a,c,d);}
    }
    [MenuItem("Tools/Mansao/Criar ressalto e pilares centrais V26")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("GroundFloor_V2/Facade_Details");
        if(parent==null){Tell("Selecione um objeto da mansao.");return;}
        if(parent.Find("Central_Facade_Relief_V26")!=null){Tell("A V26 ja existe. Nada foi duplicado.");return;}
        Transform door=Find(mansion,"Entrance_Door_Reference_V24"),window=Find(mansion,"PF_Window_Front_Ornate");
        Transform fronton=Find(mansion,"Fronton_Plaster_Exterior_V09"),band=Find(mansion,"Facade_Atrium_FloorBand");
        Renderer fr=fronton==null?null:fronton.GetComponent<Renderer>(),br=band==null?null:band.GetComponent<Renderer>();
        if(door==null || window==null || fr==null || br==null || Mathf.Abs(fr.bounds.min.z+4.94f)>.01f ||
            Mathf.Abs(br.bounds.min.y-3.45f)>.02f || Mathf.Abs(br.bounds.max.y-3.73f)>.02f)
        {Tell("Mantenha a porta V24, a janela, o frontao V09 e a faixa central ativos nas medidas aprovadas. Nada foi alterado.");return;}
        string[] upperNames={"Wall_Upper_Atrium_Front_Left","Wall_Upper_Atrium_Front_Right","Wall_Upper_Atrium_Window_Below","Wall_Upper_Atrium_Window_Above_New"};
        var upperMeshes=new List<ProBuilderMesh>();
        foreach(string name in upperNames){Transform t=Find(mansion,name);ProBuilderMesh pb=t==null?null:t.GetComponent<ProBuilderMesh>();if(pb==null){Tell("Nao encontrei "+name+" ativo. Nada foi alterado.");return;}upperMeshes.Add(pb);}
        Transform header=Find(mansion,"Wall_Entrance_AboveDoor"),infill=Find(door,"Entrance_FixedPlasterSides_V24");
        ProBuilderMesh head=header==null?null:header.GetComponent<ProBuilderMesh>(),sides=infill==null?null:infill.GetComponent<ProBuilderMesh>();
        if(head==null || sides==null){Tell("Nao encontrei o fechamento superior e os preenchimentos V24. Nada foi alterado.");return;}
        Material plaster=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Plaster_Exterior.mat"),
            wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Wood_Exterior_PBR.mat"),
            stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Stone_Exterior.mat");
        if(plaster==null || wood==null || stone==null){Tell("Faltam materiais existentes de reboco, madeira ou pedra.");return;}
        Data lower=new Data(),upper=new Data(),timber=new Data(),bases=new Data(),belt=new Data();
        Rect[] upperMasks={Rect.MinMaxRect(-1.65f,3.73f,-1.38f,6.85f),Rect.MinMaxRect(-1.38f,3.73f,1.38f,7.02f),Rect.MinMaxRect(1.38f,3.73f,1.65f,6.85f)};
        foreach(ProBuilderMesh pb in upperMeshes)foreach(Rect mask in upperMasks)FromWall(upper,pb,mask);
        Rect[] lowMasks={Rect.MinMaxRect(-1.65f,0,-1.055f,3.45f),Rect.MinMaxRect(1.055f,0,1.65f,3.45f),Rect.MinMaxRect(-1.055f,2.74f,1.055f,3.45f)};
        foreach(Rect mask in lowMasks)
        {
            FromWall(lower,head,mask);
            Rect clipped=Rect.MinMaxRect(mask.xMin,mask.yMin,mask.xMax,Mathf.Min(mask.yMax,2.65f));
            if(clipped.yMax>clipped.yMin)FromWall(lower,sides,clipped);
        }
        if(lower.area<2 || upper.area<3){Tell("Nao consegui recuperar todas as faces externas esperadas. Nada foi alterado. Areas: "+lower.area.ToString("F2")+" / "+upper.area.ToString("F2")+" m2.");return;}
        foreach(float x in new[]{-1.65f,1.65f})
        {
            Box(timber,x-.09f,x+.09f,.30f,3.46f,-5.11f,-4.795f);
            Box(timber,x-.14f,x+.14f,3.27f,3.45f,-5.15f,-4.795f);
            Box(timber,x-.09f,x+.09f,3.73f,6.86f,-5.11f,-4.795f);
            Box(timber,x-.12f,x+.12f,3.73f,3.94f,-5.14f,-4.795f);
            Box(timber,x-.13f,x+.13f,6.73f,6.86f,-5.15f,-4.795f);
            Box(bases,x-.18f,x+.18f,0,.10f,-5.19f,-4.795f);
            Box(bases,x-.145f,x+.145f,.10f,.30f,-5.15f,-4.795f);
        }
        Box(belt,-1.80f,1.80f,3.45f,3.73f,-5.10f,-4.975f);
        Box(belt,-1.84f,1.84f,3.70f,3.76f,-5.16f,-4.975f);
        foreach(Data d in new[]{lower,upper,timber,bases,belt})foreach(Vector3 p in d.p)
            if(p.z> -4.794f || p.y<-.001f || p.y>7.021f){Tell("Uma peca excedeu a area externa prevista. Nada foi alterado.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Criar ressalto central V26");
        try
        {
            GameObject root=new GameObject("Central_Facade_Relief_V26");SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);root.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(root,"Criar ressalto central");
            Build("Central_Lower_PlasterRelief_V26",lower,root.transform,plaster,true);
            Build("Central_Upper_ArchedPlasterRelief_V26",upper,root.transform,plaster,true);
            Build("Central_TimberPilasters_V26",timber,root.transform,wood,false);
            Build("Central_StonePlinths_V26",bases,root.transform,stone,false);
            Build("Central_FloorBandProjection_V26",belt,root.transform,wood,false);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;
            Tell("Ressalto central de 14 cm criado, com contorno do vao arqueado preservado. Porta, janela, frontao, paredes e tetos originais permaneceram ativos. Confira por fora e por dentro antes de salvar. Ctrl+Z desfaz o lote.");
        }
        catch(System.Exception ex){Undo.RevertAllDownToGroup(group);Debug.LogException(ex);Tell("O lote foi desfeito devido a um erro. Consulte a Console.");}
    }
    private static Transform Find(Transform root,string name)
    {foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy)return t;return null;}
    private static void FromWall(Data output,ProBuilderMesh source,Rect mask)
    {
        foreach(Face face in source.faces)
        {
            var ids=face.indexes;
            for(int i=0;i<ids.Count;i+=3)
            {
                Vector3 a=source.transform.TransformPoint(source.positions[ids[i]]),b=source.transform.TransformPoint(source.positions[ids[i+1]]),c=source.transform.TransformPoint(source.positions[ids[i+2]]);
                if(Mathf.Abs(a.z+4.8f)>.002f || Mathf.Abs(b.z+4.8f)>.002f || Mathf.Abs(c.z+4.8f)>.002f || Vector3.Cross(b-a,c-a).normalized.z>-.99f)continue;
                var poly=new List<Vector2>{new Vector2(a.x,a.y),new Vector2(b.x,b.y),new Vector2(c.x,c.y)};
                poly=Clip(poly,0,mask.xMin,true);poly=Clip(poly,0,mask.xMax,false);poly=Clip(poly,1,mask.yMin,true);poly=Clip(poly,1,mask.yMax,false);
                if(poly.Count<3)continue;float area=0;for(int j=0;j<poly.Count;j++)area+=Cross(poly[j],poly[(j+1)%poly.Count]);
                if(Mathf.Abs(area)<.000001f)continue;if(area<0)poly.Reverse();output.area+=Mathf.Abs(area)*.5f;
                Extrude(output,poly,-4.94f,-4.795f);
            }
        }
    }
    private static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
    private static List<Vector2> Clip(List<Vector2> points,int axis,float limit,bool greater)
    {
        var output=new List<Vector2>();if(points.Count==0)return output;
        Vector2 a=points[points.Count-1];float da=(greater?1:-1)*(a[axis]-limit);
        foreach(Vector2 b in points)
        {
            float db=(greater?1:-1)*(b[axis]-limit);
            if((da>=0)!=(db>=0))output.Add(Vector2.Lerp(a,b,da/(da-db)));
            if(db>=0)output.Add(b);a=b;da=db;
        }
        return output;
    }
    private static void Extrude(Data d,List<Vector2> p,float front,float back)
    {
        for(int i=1;i<p.Count-1;i++)
        {
            d.T(new Vector3(p[0].x,p[0].y,front),new Vector3(p[i+1].x,p[i+1].y,front),new Vector3(p[i].x,p[i].y,front));
            d.T(new Vector3(p[0].x,p[0].y,back),new Vector3(p[i].x,p[i].y,back),new Vector3(p[i+1].x,p[i+1].y,back));
        }
        for(int i=0;i<p.Count;i++){Vector2 a=p[i],b=p[(i+1)%p.Count];if(Vector2.Distance(a,b)<.00001f)continue;d.Q(new Vector3(a.x,a.y,front),new Vector3(b.x,b.y,front),new Vector3(b.x,b.y,back),new Vector3(a.x,a.y,back));}
    }
    private static void Box(Data d,float x0,float x1,float y0,float y1,float z0,float z1)
    {Extrude(d,new List<Vector2>{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)},z0,z1);}
    private static void Build(string name,Data d,Transform parent,Material material,bool collider)
    {
        var local=new List<Vector3>();var uv=new List<Vector2>();foreach(Vector3 p in d.p){local.Add(parent.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.y));}foreach(Face f in d.f)f.manualUV=true;
        ProBuilderMesh pb=ProBuilderMesh.Create(local,d.f);SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;pb.name=name;
        Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar acabamento central");pb.GetComponent<MeshRenderer>().sharedMaterial=material;pb.textures=uv;pb.ToMesh();pb.Refresh();
        MeshCollider mc=pb.GetComponent<MeshCollider>();if(collider){if(mc==null)mc=Undo.AddComponent<MeshCollider>(pb.gameObject);mc.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;}else if(mc!=null)Undo.DestroyObjectImmediate(mc);
    }
    private static void Tell(string text){EditorUtility.DisplayDialog("Ressalto central V26",text,"OK");}
}
#endif
