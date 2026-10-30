#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoBasementKitchenV40
{
    const string Root="TESTE_PORAO_COZINHA_V40",Folder="Assets/_Game/Art/Materials/BasementTestV40/";
    const float X0=-10.15f,X1=-9.05f,Z0=5.00f,Z1=7.70f;
    struct P {public Vector3 p;public Vector2 uv;public P(Vector3 p,Vector2 uv){this.p=p;this.uv=uv;}}
    class FloorData {public List<Vertex> vertices=new List<Vertex>();public List<Vector3> positions=new List<Vector3>();public List<Face> faces=new List<Face>();public float removedArea;}
    [MenuItem("Tools/Mansao/Demo/Testar acesso ao porao pela cozinha V40")]
    static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Mansion();if(mansion==null){Tell("Selecione Mansion ou um objeto dentro dela.");return;}
        if(mansion.Find(Root)!=null){Tell("O teste V40 ja existe. Nenhuma copia foi criada.");return;}
        int group=-1;
        try
        {
            Transform original=Find(mansion,"Floor_Kitchen");var pbSource=original.GetComponent<ProBuilderMesh>();var renderer=original.GetComponent<MeshRenderer>();
            if(pbSource==null || renderer==null)throw new InvalidOperationException("Floor_Kitchen precisa continuar como malha ProBuilder ativa.");
            Check(renderer,new Vector3(-10.4f,-.2f,4),new Vector3(-4.2f,0,10.2f));
            Check(Find(mansion,"Wall_Kitchen_Front_AboveDoor").GetComponent<Renderer>(),new Vector3(-6.45f,2.2f,3.8f),new Vector3(-5.35f,3.5f,4));
            Check(Find(mansion,"Wall_Kitchen_Left_AboveDoor").GetComponent<Renderer>(),new Vector3(-10.4f,2.1f,8.05f),new Vector3(-10.2f,3.5f,8.95f));
            if(Vector3.Distance(mansion.lossyScale,Vector3.one)>.001f)throw new InvalidOperationException("Mansion precisa manter escala mundial 1 para este teste.");
            Camera camera=null;foreach(GameObject go in mansion.gameObject.scene.GetRootGameObjects())foreach(Camera c in go.GetComponentsInChildren<Camera>())if(c.enabled && c.CompareTag("MainCamera"))camera=c;
            if(camera==null)throw new InvalidOperationException("Main Camera ativa nao encontrada.");
            FloorData data=PrepareFloor(pbSource);EnsureFolder(Folder.TrimEnd('/'));
            Material step=Mat("Steps",new Color(.32f,.38f,.40f)),wall=Mat("Walls",new Color(.22f,.26f,.28f)),dark=Mat("Shaft",new Color(.10f,.12f,.14f)),door=Mat("Door",new Color(.46f,.29f,.13f)),amber=Mat("Route",new Color(.95f,.63f,.13f));
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Criar acesso temporario ao porao V40");
            GameObject root=new GameObject(Root);SceneManager.MoveGameObjectToScene(root,mansion.gameObject.scene);root.transform.SetParent(mansion,false);Undo.RegisterCreatedObjectUndo(root,"Criar teste reversivel do porao");
            MansaoBasementTestV40 state=Undo.AddComponent<MansaoBasementTestV40>(root);state.originalKitchenFloor=original.gameObject;state.originalFloorActive=original.gameObject.activeSelf;state.playerCamera=camera;
            BuildFloor(pbSource,data,root.transform);MarkHeadroom(root.transform);
            // Divisoria nova de teste no lado da cozinha, com vao de .90 x 2.10 m.
            Box(root.transform,"Divisoria_Topo_Porta",new Vector3(-9.05f,2.10f,4.00f),new Vector3(-8.95f,3.48f,5.00f),wall);
            Box(root.transform,"Divisoria_Frente_Batente",new Vector3(-9.05f,0,4.00f),new Vector3(-8.95f,2.10f,4.05f),wall);
            Box(root.transform,"Divisoria_Tras_Batente",new Vector3(-9.05f,0,4.95f),new Vector3(-8.95f,3.48f,7.70f),wall);
            // Paredes e fundo independentes abaixo do piso: nao existe subsolo anterior a ser reaproveitado.
            Box(root.transform,"Poco_Parede_Oeste",new Vector3(-10.25f,-2.70f,5.00f),new Vector3(X0,0,8.60f),dark);
            Box(root.transform,"Poco_Parede_Leste",new Vector3(X1,-2.70f,5.00f),new Vector3(-8.95f,0,8.60f),dark);
            Box(root.transform,"Poco_Fundo",new Vector3(-10.25f,-2.80f,5.00f),new Vector3(-8.95f,-2.65f,8.60f),dark);
            Box(root.transform,"Poco_Fechamento_Final",new Vector3(-10.25f,-2.70f,8.50f),new Vector3(-8.95f,-.21f,8.60f),dark);
            for(int i=0;i<12;i++)
            {
                float top=-.20f*(i+1),near=Z0+.225f*i;
                Box(root.transform,"Degrau_Porao_"+(i+1).ToString("00"),new Vector3(-10.10f,-2.65f,near),new Vector3(-9.10f,top,near+.225f),step);
            }
            Box(root.transform,"Patamar_Inferior",new Vector3(X0,-2.60f,Z1),new Vector3(X1,-2.40f,8.50f),step);
            Transform hinge=Organizer(root.transform,"Porta_Teste_Pivo",new Vector3(-8.94f,.01f,4.05f));
            Cube(hinge,"Porta_Teste_Folha",new Vector3(-8.94f,1.05f,4.50f),new Vector3(.055f,2.08f,.89f),door,true);
            Cube(hinge,"Porta_Teste_Puxador",new Vector3(-8.89f,1.10f,4.84f),new Vector3(.05f,.18f,.04f),amber,false);
            state.hinge=hinge;state.interactionPoint=Organizer(root.transform,"Porta_Teste_Interacao",new Vector3(-8.65f,1.2f,4.5f));
            Vector3[] route={new Vector3(0,.028f,2.60f),new Vector3(-4.65f,.028f,2.60f),new Vector3(-5.9f,.028f,2.60f),new Vector3(-5.9f,.028f,4.55f),new Vector3(-8.35f,.028f,4.55f)};
            for(int i=0;i<route.Length-1;i++)Line(root.transform,"Linha_Ambar_"+i,route[i],route[i+1],amber);
            Undo.RecordObject(original.gameObject,"Guardar piso original intacto durante o teste");original.gameObject.SetActive(false);
            EditorUtility.SetDirty(state);EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;AssetDatabase.SaveAssets();
            Debug.Log("V40c criado: copia do piso da cozinha com abertura de 1.10 x 2.70 m; piso original intacto e temporariamente desativado. Porta E, 12 degraus de .20/.225 m e patamar em Y -2.40. Remover pelo menu restaura o piso original. Nenhum evento narrativo, luz ou parede existente foi modificado.",root);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi aplicado: "+e.Message);}
    }
    [MenuItem("Tools/Mansao/Demo/Remover acesso ao porao da cozinha V40")]
    static void Remove()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Mansion(),root=mansion==null?null:mansion.Find(Root);MansaoBasementTestV40 state=root==null?null:root.GetComponent<MansaoBasementTestV40>();
        if(state==null || state.originalKitchenFloor==null){Tell("Selecione Mansion com o teste V40 e sua referencia do piso original.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Remover teste do porao V40");Undo.RecordObject(state.originalKitchenFloor,"Restaurar piso original");state.originalKitchenFloor.SetActive(state.originalFloorActive);Undo.DestroyObjectImmediate(root.gameObject);EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);Undo.CollapseUndoOperations(group);
    }
    [MenuItem("Tools/Mansao/Demo/Corrigir folga de cabeca do porao V40c")]
    static void FixHeadroom()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Mansion(),root=mansion==null?null:mansion.Find(Root);
        MansaoBasementTestV40 state=root==null?null:root.GetComponent<MansaoBasementTestV40>();
        if(state==null || state.originalKitchenFloor==null){Tell("Selecione Mansion com o teste V40b instalado.");return;}
        if(root.Find("Folga_Cabeca_Corrigida_V40c")!=null){Tell("O ajuste V40c ja foi aplicado.");return;}
        int group=-1;
        try
        {
            Transform wall=root.Find("Divisoria_Tras_Batente"),oldFloor=root.Find("Floor_Kitchen_Copia_Abertura_Teste_V40");
            if(wall==null || oldFloor==null || Mathf.Abs(wall.GetComponent<Renderer>().bounds.max.z-7.70f)>.01f)
                throw new InvalidOperationException("O conjunto nao corresponde ao teste compacto V40b. Nenhum piso foi alterado.");
            if(state.originalKitchenFloor.activeInHierarchy)throw new InvalidOperationException("O piso original esta ativo junto da copia; confira o estado do teste antes de prosseguir.");
            ProBuilderMesh source=state.originalKitchenFloor.GetComponent<ProBuilderMesh>();
            if(source==null)throw new InvalidOperationException("Reserva original do piso nao encontrada.");
            FloorData data=PrepareFloor(source);
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Corrigir folga inferior da laje V40c");
            ProBuilderMesh replacement=BuildFloor(source,data,root);Undo.DestroyObjectImmediate(oldFloor.gameObject);MarkHeadroom(root);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=replacement.gameObject;
            Debug.Log("V40c: somente a copia do piso foi reconstruida com chanfro inferior local na borda do vao. Porta, escada, piso original, circulacao superior e vao lateral preservados. Testar descida e subida antes de salvar.",root);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O ajuste nao foi aplicado: "+e.Message);}
    }
    // O topo permanece Y 0. O canto inferior recua de Y -.20 para -.05 na borda,
    // voltando a espessura de .20 m ao longo de .40 m; faixas laterais de .10 m fazem a transicao.
    static Vector3 HeadroomWorld(Vector3 p)
    {
        if(Mathf.Abs(p.y+.20f)>.0005f || p.z<Z1-.00001f || p.z>Z1+.40f+.00001f)return p;
        float fx=Mathf.Clamp01(Mathf.Max(0,Mathf.Min(p.x-X0,X1-p.x))/.10f);
        float fz=1-Mathf.Clamp01((p.z-Z1)/.40f);
        p.y+=.15f*fx*fz;return p;
    }
    static void MarkHeadroom(Transform root)
    {
        GameObject marker=new GameObject("Folga_Cabeca_Corrigida_V40c");SceneManager.MoveGameObjectToScene(marker,root.gameObject.scene);marker.transform.SetParent(root,false);Undo.RegisterCreatedObjectUndo(marker,"Marcar ajuste local do piso");
    }
    static FloorData PrepareFloor(ProBuilderMesh source)
    {
        var data=new FloorData();var uv=source.textures;
        foreach(Face face in source.faces){var ids=face.indexes;for(int i=0;i<ids.Count;i+=3){var poly=new List<P>();for(int k=0;k<3;k++){int id=ids[i+k];poly.Add(new P(source.transform.TransformPoint(source.positions[id]),uv!=null && uv.Count==source.vertexCount?uv[id]:Vector2.zero));}
            var parts=new List<List<P>>{poly};parts=Split(parts,0,X0);parts=Split(parts,0,X1);parts=Split(parts,2,Z0);parts=Split(parts,2,Z1);parts=Split(parts,2,Z1+.40f);parts=Split(parts,0,X0+.10f);parts=Split(parts,0,X1-.10f);
            foreach(var part in parts)for(int k=1;k<part.Count-1;k++){
                P[] tri={part[0],part[k],part[k+1]};Vector3 cross=Vector3.Cross(tri[1].p-tri[0].p,tri[2].p-tri[0].p);if(cross.sqrMagnitude<1e-12f)continue;
                Vector3 center=(tri[0].p+tri[1].p+tri[2].p)/3;bool inside=center.x>X0 && center.x<X1 && center.z>Z0 && center.z<Z1;
                if(inside){if(cross.normalized.y>.99f)data.removedArea+=cross.magnitude*.5f;continue;}
                Add(data,source,tri,face.submeshIndex,face.smoothingGroup);
            }
        }}
        if(Mathf.Abs(data.removedArea-(X1-X0)*(Z1-Z0))>.003f)throw new InvalidOperationException("A abertura nao coube integralmente no piso: "+data.removedArea.ToString("F4")+" m2.");
        Reveal(data,source,new Vector3(X0,-.2f,Z0),new Vector3(X0,0,Z0),new Vector3(X0,0,Z1),new Vector3(X0,-.2f,Z1));
        Reveal(data,source,new Vector3(X1,-.2f,Z1),new Vector3(X1,0,Z1),new Vector3(X1,0,Z0),new Vector3(X1,-.2f,Z0));
        Reveal(data,source,new Vector3(X1,-.2f,Z0),new Vector3(X1,0,Z0),new Vector3(X0,0,Z0),new Vector3(X0,-.2f,Z0));
        float[] lip={X0,X0+.10f,X1-.10f,X1};for(int j=0;j<lip.Length-1;j++)Reveal(data,source,new Vector3(lip[j],-.2f,Z1),new Vector3(lip[j],0,Z1),new Vector3(lip[j+1],0,Z1),new Vector3(lip[j+1],-.2f,Z1));return data;
    }
    static void Add(FloorData d,ProBuilderMesh source,P[] tri,int material,int smoothing=0)
    {
        int start=d.vertices.Count;foreach(P p in tri){Vector3 local=source.transform.InverseTransformPoint(HeadroomWorld(p.p));d.positions.Add(local);d.vertices.Add(new Vertex{position=local,uv0=p.uv});}Face f=new Face(new[]{start,start+1,start+2});f.manualUV=true;f.submeshIndex=material;f.smoothingGroup=smoothing;d.faces.Add(f);
    }
    static void Reveal(FloorData d,ProBuilderMesh source,Vector3 a,Vector3 b,Vector3 c,Vector3 e){Add(d,source,new[]{new P(a,Vector2.zero),new P(b,Vector2.up),new P(c,Vector2.one)},0);Add(d,source,new[]{new P(a,Vector2.zero),new P(c,Vector2.one),new P(e,Vector2.right)},0);}
    static List<List<P>> Split(List<List<P>> input,int axis,float cut)
    {
        var output=new List<List<P>>();foreach(var p in input){float lo=float.PositiveInfinity,hi=float.NegativeInfinity;foreach(P v in p){lo=Mathf.Min(lo,v.p[axis]);hi=Mathf.Max(hi,v.p[axis]);}if(lo>=cut-.000001f || hi<=cut+.000001f){output.Add(p);continue;}var a=Clip(p,axis,cut,true);var b=Clip(p,axis,cut,false);if(a.Count>=3)output.Add(a);if(b.Count>=3)output.Add(b);}return output;
    }
    static List<P> Clip(List<P> input,int axis,float cut,bool greater)
    {
        var output=new List<P>();P a=input[input.Count-1];float da=(greater?1:-1)*(a.p[axis]-cut);foreach(P b in input){float db=(greater?1:-1)*(b.p[axis]-cut);if((da>=0)!=(db>=0)){float t=da/(da-db);output.Add(new P(Vector3.Lerp(a.p,b.p,t),Vector2.Lerp(a.uv,b.uv,t)));}if(db>=0)output.Add(b);a=b;da=db;}return output;
    }
    static ProBuilderMesh BuildFloor(ProBuilderMesh source,FloorData data,Transform parent)
    {
        ProBuilderMesh pb=ProBuilderMesh.Create(data.vertices,data.faces,SharedVertex.GetSharedVerticesWithPositions(data.positions),null,source.GetComponent<MeshRenderer>().sharedMaterials);SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(parent,false);pb.transform.position=source.transform.position;pb.transform.rotation=source.transform.rotation;pb.transform.localScale=source.transform.lossyScale;pb.name="Floor_Kitchen_Copia_Abertura_Teste_V40";pb.gameObject.layer=source.gameObject.layer;Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar copia recortada do piso");pb.ToMesh();pb.Refresh();
        foreach(Collider c in pb.GetComponents<Collider>())Undo.DestroyObjectImmediate(c);
        foreach(Collider original in source.GetComponents<Collider>()){if(!(original is MeshCollider))throw new InvalidOperationException("O piso original tem collider diferente de MeshCollider; nada foi alterado.");MeshCollider next=Undo.AddComponent<MeshCollider>(pb.gameObject);EditorUtility.CopySerialized(original,next);next.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;}
        if(pb.GetComponent<MeshCollider>()==null){MeshCollider c=Undo.AddComponent<MeshCollider>(pb.gameObject);c.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;}
        MeshRenderer old=source.GetComponent<MeshRenderer>(),r=pb.GetComponent<MeshRenderer>();r.shadowCastingMode=old.shadowCastingMode;r.receiveShadows=old.receiveShadows;
        if(Vector3.Distance(old.bounds.min,r.bounds.min)>.001f || Vector3.Distance(old.bounds.max,r.bounds.max)>.001f)throw new InvalidOperationException("Os limites externos do piso foram alterados.");return pb;
    }
    static Transform Organizer(Transform parent,string name,Vector3 position){GameObject go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);go.transform.SetParent(parent,false);go.transform.position=position;go.transform.rotation=Quaternion.identity;Undo.RegisterCreatedObjectUndo(go,"Criar referencia de teste");return go.transform;}
    static void Box(Transform parent,string name,Vector3 min,Vector3 max,Material material){Cube(parent,name,(min+max)*.5f,max-min,material,true);}
    static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collider)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);go.transform.SetParent(parent,false);go.transform.position=position;go.transform.rotation=Quaternion.identity;go.transform.localScale=size;go.name=name;Undo.RegisterCreatedObjectUndo(go,"Criar volume de teste");go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;if(!collider)Undo.DestroyObjectImmediate(go.GetComponent<BoxCollider>());return go;
    }
    static void Line(Transform parent,string name,Vector3 a,Vector3 b,Material material){GameObject go=Cube(parent,name,(a+b)*.5f,new Vector3(.055f,.012f,Vector3.Distance(a,b)),material,false);go.transform.rotation=Quaternion.LookRotation((b-a).normalized,Vector3.up);}
    static Material Mat(string name,Color color){string path=Folder+"M_Basement_"+name+"_V40.mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;Shader shader=Shader.Find("Universal Render Pipeline/Unlit");if(shader==null)throw new InvalidOperationException("Shader de teste nao encontrado.");m=new Material(shader);m.name="M_Basement_"+name+"_V40";m.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(m,path);return m;}
    static Transform Find(Transform root,string name){Transform found=null;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy){if(found!=null)throw new InvalidOperationException("Nome ativo duplicado: "+name);found=t;}if(found==null)throw new InvalidOperationException("Nao encontrei "+name+" ativo.");return found;}
    static void Check(Renderer r,Vector3 min,Vector3 max){if(r==null || Vector3.Distance(r.bounds.min,min)>.025f || Vector3.Distance(r.bounds.max,max)>.025f)throw new InvalidOperationException("As medidas de uma parede ou piso diferem da verificacao.");}
    static Transform Mansion(){Transform t=Selection.activeTransform;while(t!=null && t.name!="Mansion")t=t.parent;return t;}
    static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');EnsureFolder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
    static void Tell(string message){EditorUtility.DisplayDialog("Teste do porao pela cozinha V40",message,"OK");}
}
#endif

