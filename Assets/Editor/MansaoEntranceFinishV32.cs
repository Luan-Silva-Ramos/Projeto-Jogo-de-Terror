#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

// Instalacao manual pelo menu. Nenhuma alteracao automatica ao importar.
public static class MansaoEntranceFinishV32
{
    class Data
    {
        public List<Vector3> p=new List<Vector3>();public List<Vector2> uv=new List<Vector2>();public List<Face> f=new List<Face>();
        public void Tri(Vector3 a,Vector3 b,Vector3 c)
        {
            if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-12f)return;
            int first=p.Count;Vector3 n=Vector3.Cross(b-a,c-a).normalized;
            Vector3 u=Vector3.ProjectOnPlane(Vector3.right,n).normalized;
            if(u.sqrMagnitude<.5f)u=Vector3.forward;
            Vector3 v=Vector3.Cross(n,u).normalized;
            foreach(Vector3 point in new[]{a,b,c}){p.Add(point);uv.Add(new Vector2(Vector3.Dot(point,u),Vector3.Dot(point,v)));}
            Face face=new Face(new[]{first,first+1,first+2});face.manualUV=true;f.Add(face);
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
    }
    [MenuItem("Tools/Mansao/Acabamento da sobreporta V32")]
    static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("GroundFloor_V2/Facade_Details");
        if(parent==null){Tell("Selecione Mansion ou um objeto dentro dela.");return;}
        if(parent.Find("Entrance_Crown_Reference_V32")!=null){Tell("O conjunto V32 ja existe. Nenhuma copia foi criada.");return;}
        int group=-1;
        try
        {
            Transform door=Find(mansion,"Entrance_Door_Reference_V24"),relief=Find(mansion,"Central_Lower_PlasterRelief_V26"),belt=Find(mansion,"Central_FloorBandProjection_V26");
            Transform oldTrim=Find(door,"Entrance_FrameMouldings_V24");ProBuilderMesh source=oldTrim.GetComponent<ProBuilderMesh>();
            if(source==null)throw new InvalidOperationException("Os frisos V24 precisam continuar como malha ProBuilder.");
            Bounds trimBounds=source.GetComponent<Renderer>().bounds,reliefBounds=relief.GetComponent<Renderer>().bounds,beltBounds=belt.GetComponent<Renderer>().bounds;
            if(Mathf.Abs(trimBounds.max.y-3.04f)>.015f || Mathf.Abs(trimBounds.min.x+1.16f)>.015f || Mathf.Abs(trimBounds.max.x-1.16f)>.015f || Mathf.Abs(trimBounds.min.z+5.11f)>.015f)
                throw new InvalidOperationException("Os frisos da porta diferem das medidas aprovadas. Nenhuma peca foi movida.");
            if(Mathf.Abs(reliefBounds.min.z+4.94f)>.01f || Mathf.Abs(beltBounds.min.y-3.45f)>.01f)
                throw new InvalidOperationException("O ressalto ou a faixa central diferem das medidas registradas.");
            Material[] oldMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;
            if(oldMaterials.Length!=1 || oldMaterials[0]==null)throw new InvalidOperationException("Material unico dos frisos da porta nao encontrado.");
            Material wood=oldMaterials[0],stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Stone_Exterior.mat");
            if(stone==null)throw new InvalidOperationException("Material M_Stone_Exterior nao encontrado.");
            Data lower=CopyLower(source),cornice=new Data(),pediment=new Data(),caps=new Data(),supports=new Data(),oval=new Data(),inset=new Data();
            Box(cornice,-1.20f,1.20f,2.70f,2.80f,-5.12f,-4.935f);
            Box(cornice,-1.25f,1.25f,2.795f,2.84f,-5.18f,-4.935f);
            float endY=2.90f+.35f*(1-1.20f/1.30f);
            Extrude(pediment,new[]{new Vector2(-1.20f,2.835f),new Vector2(1.20f,2.835f),new Vector2(1.20f,endY),new Vector2(0,3.25f),new Vector2(-1.20f,endY)},-5.10f,-4.935f);
            Extrude(caps,new[]{new Vector2(-1.30f,2.90f),new Vector2(0,3.25f),new Vector2(0,3.35f),new Vector2(-1.30f,3.00f)},-5.20f,-5.065f);
            Extrude(caps,new[]{new Vector2(0,3.25f),new Vector2(1.30f,2.90f),new Vector2(1.30f,3.00f),new Vector2(0,3.35f)},-5.20f,-5.065f);
            foreach(float x in new[]{-1.15f,1.15f})
            {
                Box(supports,x-.07f,x+.07f,2.46f,2.80f,-5.045f,-4.935f);
                Corbel(supports,x);
            }
            Oval(oval,.095f,.13f,3.30f,-5.245f,-5.075f);
            Oval(inset,.062f,.095f,3.30f,-5.258f,-5.242f);
            foreach(Data d in new[]{cornice,pediment,caps,supports,oval,inset})foreach(Vector3 p in d.p)
                if(p.z> -4.934f || p.y>3.431f || p.y<2.459f || Mathf.Abs(p.x)>1.301f)
                    throw new InvalidOperationException("Uma peca excedeu o envelope externo previsto.");
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Acabamento da sobreporta V32");
            GameObject root=new GameObject("Entrance_Crown_Reference_V32");SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);root.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(root,"Criar acabamento da entrada");
            Build("Entrance_LowerMouldings_Preserved_V32",lower,root.transform,wood,source.gameObject.layer);
            Build("Entrance_Cornice_V32",cornice,root.transform,wood,source.gameObject.layer);
            Build("Entrance_ShallowPediment_V32",pediment,root.transform,wood,source.gameObject.layer);
            Build("Entrance_SlopingCrown_V32",caps,root.transform,wood,source.gameObject.layer);
            Build("Entrance_SmallCorbels_V32",supports,root.transform,wood,source.gameObject.layer);
            Build("Entrance_OvalFrame_V32",oval,root.transform,wood,source.gameObject.layer);
            Build("Entrance_OvalStoneInset_V32",inset,root.transform,stone,source.gameObject.layer);
            Undo.RecordObject(oldTrim.gameObject,"Preservar frisos V24 como reserva");oldTrim.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;
            Debug.Log("V32 criada: sobreporta com 2.60 m de largura, coroamento Y 3.35 e medalhao Y 3.43. Frisos inferiores copiados sem deslocamento. Frisos V24 guardados inativos. Folhas, batentes, interacao, colliders, janela e iluminacao preservados. Ctrl+Z desfaz o lote.",root);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi aplicado: "+e.Message);}
    }
    static Transform Find(Transform root,string name)
    {
        Transform found=null;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy){if(found!=null)throw new InvalidOperationException("Nome ativo duplicado: "+name);found=t;}
        if(found==null)throw new InvalidOperationException("Nao encontrei "+name+" ativo.");return found;
    }
    static Data CopyLower(ProBuilderMesh source)
    {
        Data d=new Data();IList<Vector2> uv=source.textures;
        foreach(Face face in source.faces){var ids=face.indexes;for(int i=0;i<ids.Count;i+=3){Vector3[] p=new Vector3[3];float min=float.PositiveInfinity,max=float.NegativeInfinity;
            for(int k=0;k<3;k++){p[k]=source.transform.TransformPoint(source.positions[ids[i+k]]);min=Mathf.Min(min,p[k].y);max=Mathf.Max(max,p[k].y);}
            if(min>=2.699f)continue;
            if(max>2.699f)throw new InvalidOperationException("Os frisos foram editados e cruzam o corte previsto em Y 2.699.");
            int first=d.p.Count;for(int k=0;k<3;k++){int id=ids[i+k];d.p.Add(p[k]);d.uv.Add(uv!=null && uv.Count==source.vertexCount?uv[id]:Vector2.zero);}
            var next=new Face(new[]{first,first+1,first+2});next.manualUV=true;next.smoothingGroup=face.smoothingGroup;d.f.Add(next);
        }}
        if(d.f.Count==0)throw new InvalidOperationException("Nao consegui preservar os frisos inferiores.");return d;
    }
    // Perfis convexos em ordem anti-horaria vistos no plano XY.
    static void Extrude(Data d,Vector2[] shape,float front,float back)
    {
        Vector2 center=Vector2.zero;foreach(Vector2 p in shape)center+=p;center/=shape.Length;
        for(int i=0;i<shape.Length;i++){Vector2 a=shape[i],b=shape[(i+1)%shape.Length];Vector3 af=new Vector3(a.x,a.y,front),bf=new Vector3(b.x,b.y,front),ab=new Vector3(a.x,a.y,back),bb=new Vector3(b.x,b.y,back);
            d.Tri(new Vector3(center.x,center.y,front),bf,af);d.Tri(new Vector3(center.x,center.y,back),ab,bb);d.Quad(af,bf,bb,ab);}
    }
    static void Box(Data d,float x0,float x1,float y0,float y1,float z0,float z1)
    {Extrude(d,new[]{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)},z0,z1);}
    static void Corbel(Data d,float x)
    {
        Vector2[] yz={new Vector2(2.50f,-4.935f),new Vector2(2.50f,-4.79f),new Vector2(2.975f,-4.79f),new Vector2(2.975f,-5.20f),new Vector2(2.925f,-5.20f),new Vector2(2.89f,-5.12f),new Vector2(2.83f,-5.045f),new Vector2(2.75f,-4.99f),new Vector2(2.65f,-4.95f)};
        for(int i=0;i<yz.Length;i++)yz[i]=new Vector2(2.48f+(yz[i].x-2.50f)*(.32f/.475f),-4.935f+(yz[i].y+4.79f)*(.265f/.41f));
        for(int i=1;i<yz.Length-1;i++){int a=2,b=(2+i)%yz.Length,c=(2+i+1)%yz.Length;
            d.Tri(new Vector3(x-.055f,yz[a].x,yz[a].y),new Vector3(x-.055f,yz[b].x,yz[b].y),new Vector3(x-.055f,yz[c].x,yz[c].y));
            d.Tri(new Vector3(x+.055f,yz[a].x,yz[a].y),new Vector3(x+.055f,yz[c].x,yz[c].y),new Vector3(x+.055f,yz[b].x,yz[b].y));}
        for(int i=0;i<yz.Length;i++){Vector2 a=yz[i],b=yz[(i+1)%yz.Length];d.Quad(new Vector3(x-.055f,a.x,a.y),new Vector3(x+.055f,a.x,a.y),new Vector3(x+.055f,b.x,b.y),new Vector3(x-.055f,b.x,b.y));}
    }
    static void Oval(Data d,float rx,float ry,float cy,float front,float back)
    {
        const int sides=32;var shape=new Vector2[sides];for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;shape[i]=new Vector2(rx*Mathf.Cos(angle),cy+ry*Mathf.Sin(angle));}Extrude(d,shape,front,back);
    }
    static void Build(string name,Data d,Transform parent,Material material,int layer)
    {
        var vertices=new List<Vertex>();var positions=new List<Vector3>();for(int i=0;i<d.p.Count;i++){Vector3 local=parent.InverseTransformPoint(d.p[i]);positions.Add(local);vertices.Add(new Vertex{position=local,uv0=d.uv[i]});}
        ProBuilderMesh pb=ProBuilderMesh.Create(vertices,d.f,SharedVertex.GetSharedVerticesWithPositions(positions),null,new[]{material});SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;pb.name=name;pb.gameObject.layer=layer;Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar sobreporta V32");pb.ToMesh();pb.Refresh();
        MeshCollider collider=pb.GetComponent<MeshCollider>();if(collider!=null)Undo.DestroyObjectImmediate(collider);
    }
    static void Tell(string text){EditorUtility.DisplayDialog("Sobreporta V32",text,"OK");}
}
#endif
