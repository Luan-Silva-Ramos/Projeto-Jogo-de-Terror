#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoReferenceDoorV24
{
    private class Data
    {
        public List<Vector3> p=new List<Vector3>();public List<Face> f=new List<Face>();
        public void Tri(Vector3 a,Vector3 b,Vector3 c){int i=p.Count;p.Add(a);p.Add(b);p.Add(c);f.Add(new Face(new[]{i,i+1,i+2}));}
        public void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
    }
    [MenuItem("Tools/Mansao/Criar porta dupla da referencia V24")]
    private static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        if(mansion==null){Tell("Selecione um objeto da mansao.");return;}
        if(Find(mansion,"Entrance_Door_Reference_V24",false)!=null){Tell("A V24 ja existe. Nada foi duplicado.");return;}
        Transform old=Find(mansion,"Door_Entrance_Assembly",true),wall=Find(mansion,"Wall_Entrance_AboveDoor",true);
        if(old==null || wall==null || old.parent==null){Tell("Nao encontrei a porta original e a parede da entrada ativas.");return;}
        Renderer wr=wall.GetComponent<Renderer>();
        if(wr==null || Mathf.Abs(wr.bounds.min.y-2.65f)>.025f || Mathf.Abs(wr.bounds.min.z+4.8f)>.025f ||
            Mathf.Abs(wr.bounds.max.z+4.5f)>.025f || Vector3.Distance(old.lossyScale,Vector3.one)>.001f)
        {Tell("A entrada difere das medidas consultadas. Nada foi alterado.");return;}
        MonoBehaviour interaction=null;
        foreach(MonoBehaviour c in old.GetComponents<MonoBehaviour>())if(c!=null && c.GetType().Name=="DoubleDoorInteraction")interaction=c;
        if(interaction==null){Tell("Nao encontrei DoubleDoorInteraction na porta original.");return;}
        SerializedObject previous=new SerializedObject(interaction);
        string[] references={"player","interactionPoint","leftHinge","rightHinge"};
        foreach(string field in references)if(previous.FindProperty(field)==null || previous.FindProperty(field).objectReferenceValue==null)
        {Tell("A referencia "+field+" da porta original esta vazia. Nada foi alterado.");return;}
        Transform leftOld=(Transform)previous.FindProperty("leftHinge").objectReferenceValue;
        Transform rightOld=(Transform)previous.FindProperty("rightHinge").objectReferenceValue;
        if(Vector3.Distance(leftOld.position,new Vector3(-.80625f,1.30996f,-4.85f))>.035f ||
            Vector3.Distance(rightOld.position,new Vector3(.80625f,1.30996f,-4.85f))>.035f ||
            Quaternion.Angle(leftOld.rotation,Quaternion.identity)>.1f || Quaternion.Angle(rightOld.rotation,Quaternion.identity)>.1f)
        {Tell("Os pivos originais nao estao nas posicoes fechadas consultadas. Nada foi alterado.");return;}
        Material wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Wood_Exterior_PBR.mat");
        Material plaster=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Plaster_Exterior.mat");
        Material glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Glass_Atrium.mat");
        Shader lit=Shader.Find("Universal Render Pipeline/Lit");
        if(wood==null || plaster==null || glass==null || lit==null){Tell("Faltam materiais existentes de madeira, reboco, vidro ou o shader URP.");return;}
        Material doorWood=MaterialCopy("M_Entrance_Reference_Wood_V24",wood,new Color(.63f,.59f,.53f,1),0,.23f);
        Material brass=MaterialCopy("M_Entrance_AgedBrass_V24",null,new Color(.32f,.23f,.10f,1),.72f,.30f);
        Material black=MaterialCopy("M_Entrance_Keyhole_V24",null,new Color(.018f,.014f,.010f,1),0,.10f);
        Data infill=new Data(),frame=new Data(),trim=new Data();
        Box(infill,-1.705f,-1.015f,0,2.655f,-4.80f,-4.505f);
        Box(infill,1.015f,1.705f,0,2.655f,-4.80f,-4.505f);
        foreach(float x in new[]{-.95f,.95f})
        {
            Box(frame,x-.09f,x+.09f,0,2.72f,-4.94f,-4.505f);
            Box(trim,x-.14f,x+.14f,0,.20f,-5.015f,-4.79f);
            Box(trim,x-.13f,x+.13f,2.47f,2.67f,-5.005f,-4.79f);
        }
        // Batentes frontais: ficam a frente das folhas e nao bloqueiam o giro para dentro.
        Box(trim,-.90f,-.80f,.015f,2.62f,-4.96f,-4.895f);
        Box(trim,.80f,.90f,.015f,2.62f,-4.96f,-4.895f);
        Box(frame,-1.04f,1.04f,2.60f,2.72f,-4.96f,-4.505f);
        Box(trim,-1.10f,1.10f,2.70f,2.78f,-5.055f,-4.79f);
        Box(trim,-1.16f,1.16f,2.778f,2.84f,-5.11f,-4.79f);
        // Medalhao pequeno: silhueta simples, sem simular entalhe complexo.
        Disc(trim,new Vector3(0,2.91f,-4.79f),.10f,.13f,-.29f);
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Criar porta de referencia V24");
        try
        {
            Transform root=Organizer("Entrance_Door_Reference_V24",old.parent);
            root.position=Vector3.zero;root.rotation=Quaternion.identity;root.localScale=Vector3.one;
            Build("Entrance_FixedPlasterSides_V24",infill,root,plaster);
            Build("Entrance_Frame_V24",frame,root,doorWood);
            Build("Entrance_FrameMouldings_V24",trim,root,doorWood);
            // Laterais fecham o antigo vao lateral, sem acrescentar volume alem da face interna da parede.
            ColliderBox("FixedInfill_Left_Collider_V24",root,new Vector3(-1.36f,1.325f,-4.6525f),new Vector3(.69f,2.65f,.295f));
            ColliderBox("FixedInfill_Right_Collider_V24",root,new Vector3(1.36f,1.325f,-4.6525f),new Vector3(.69f,2.65f,.295f));
            ColliderBox("Jamb_Left_Collider_V24",root,new Vector3(-.95f,1.325f,-4.72f),new Vector3(.18f,2.65f,.43f));
            ColliderBox("Jamb_Right_Collider_V24",root,new Vector3(.95f,1.325f,-4.72f),new Vector3(.18f,2.65f,.43f));
            Transform left=Leaf(root,true,doorWood,glass,brass,black);
            Transform right=Leaf(root,false,doorWood,glass,brass,black);
            Transform point=Organizer("Entrance_InteractionPoint_V24",root);
            point.position=((Transform)previous.FindProperty("interactionPoint").objectReferenceValue).position;
            Component copied=Undo.AddComponent(root.gameObject,interaction.GetType());EditorUtility.CopySerialized(interaction,copied);
            SerializedObject settings=new SerializedObject(copied);
            settings.FindProperty("leftHinge").objectReferenceValue=left;
            settings.FindProperty("rightHinge").objectReferenceValue=right;
            settings.FindProperty("interactionPoint").objectReferenceValue=point;
            settings.ApplyModifiedProperties();
            Transform crown=Find(mansion,"Entrance_Crown_V23",true);
            if(crown!=null){Undo.RecordObject(crown.gameObject,"Preservar sobreporta anterior");crown.gameObject.SetActive(false);}
            Undo.RecordObject(old.gameObject,"Preservar porta original");old.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root.gameObject;
            Tell("Porta V24 criada. Porta antiga e sobreporta V23 preservadas desativadas. Interacao copiada, com os novos pivos. Confira aparencia e teste E no Play Mode: abrir, passar e fechar. Ctrl+Z desfaz a cena; materiais permanecem.");
        }
        catch(Exception ex){Undo.RevertAllDownToGroup(group);Debug.LogException(ex);Tell("O lote foi desfeito devido a um erro. Consulte a Console.");}
    }
    private static Transform Leaf(Transform parent,bool left,Material wood,Material glass,Material brass,Material black)
    {
        Transform hinge=Organizer(left?"Door_Left_Hinge_V24":"Door_Right_Hinge_V24",parent);
        hinge.position=new Vector3(left?-.80625f:.80625f,.02f,-4.85f);hinge.rotation=Quaternion.identity;
        Data body=new Data(),glazing=new Data(),metal=new Data(),holes=new Data();
        Box(body,0,.09f,0,2.58f,-.035f,.035f);Box(body,.71f,.80f,0,2.58f,-.035f,.035f);
        Box(body,.09f,.71f,0,.14f,-.035f,.035f);Box(body,.09f,.71f,1.00f,1.13f,-.035f,.035f);
        Box(body,.09f,.71f,1.96f,2.07f,-.035f,.035f);Box(body,.09f,.71f,2.46f,2.58f,-.035f,.035f);
        Panel(body,.09f,.71f,.14f,1.00f);Panel(body,.09f,.71f,1.13f,1.96f);
        Box(glazing,.09f,.71f,2.07f,2.46f,-.006f,.006f);
        foreach(float x in new[]{.29667f,.50333f})Box(body,x-.012f,x+.012f,2.07f,2.46f,-.025f,.025f);
        Box(body,.09f,.71f,2.253f,2.277f,-.025f,.025f);
        foreach(float sign in new[]{-1f,1f})
        {
            float z=sign*.053f;
            Box(metal,.620f,.700f,.91f,1.25f,z-.012f,z+.012f);
            Disc(metal,new Vector3(.66f,1.16f,sign*.066f),.033f,.033f,sign*.027f);
            Box(metal,.646f,.674f,1.05f,1.23f,Mathf.Min(sign*.105f,sign*.128f),Mathf.Max(sign*.105f,sign*.128f));
            foreach(float y in new[]{1.06f,1.22f})Box(metal,.648f,.672f,y-.012f,y+.012f,Mathf.Min(sign*.060f,sign*.116f),Mathf.Max(sign*.060f,sign*.116f));
            if(left)
            {
                Disc(holes,new Vector3(.66f,.985f,sign*.0655f),.012f,.012f,sign*.002f);
                Box(holes,.653f,.667f,.956f,.985f,Mathf.Min(sign*.0655f,sign*.0675f),Mathf.Max(sign*.0655f,sign*.0675f));
            }
        }
        if(!left){Mirror(body);Mirror(glazing);Mirror(metal);Mirror(holes);}
        Build("Door_WoodPanels_V24",body,hinge,wood);
        Build("Door_UpperLights_V24",glazing,hinge,glass);
        Build("Door_HandlesAndPlates_V24",metal,hinge,brass);
        if(holes.p.Count>0)Build("Door_Keyholes_V24",holes,hinge,black);
        ColliderBox("Door_LeafCollider_V24",hinge,new Vector3(left?.40f:-.40f,1.29f,0),new Vector3(.80f,2.58f,.07f));
        return hinge;
    }
    private static void Panel(Data d,float x0,float x1,float y0,float y1)
    {
        Box(d,x0,x1,y0,y1,-.017f,.017f);
        foreach(float sign in new[]{-1f,1f})
        {
            float za=sign*.018f,zb=sign*.038f;
            Vector3[] a={new Vector3(x0+.018f,y0+.018f,za),new Vector3(x1-.018f,y0+.018f,za),new Vector3(x1-.018f,y1-.018f,za),new Vector3(x0+.018f,y1-.018f,za)};
            Vector3[] b={new Vector3(x0+.060f,y0+.060f,zb),new Vector3(x1-.060f,y0+.060f,zb),new Vector3(x1-.060f,y1-.060f,zb),new Vector3(x0+.060f,y1-.060f,zb)};
            for(int i=0;i<4;i++){int j=(i+1)%4;if(sign<0)d.Q(a[i],b[i],b[j],a[j]);else d.Q(a[i],a[j],b[j],b[i]);}
            if(sign<0)d.Q(b[3],b[2],b[1],b[0]);else d.Q(b[0],b[1],b[2],b[3]);
        }
    }
    private static void Mirror(Data d)
    {
        for(int i=0;i<d.p.Count;i++){Vector3 p=d.p[i];p.x=-p.x;d.p[i]=p;}
        foreach(Face f in d.f){var ids=f.indexes;f.SetIndexes(new[]{ids[0],ids[2],ids[1]});}
    }
    private static void Box(Data d,float x0,float x1,float y0,float y1,float z0,float z1)
    {
        Vector3 a=new Vector3(x0,y0,z0),b=new Vector3(x1,y0,z0),c=new Vector3(x1,y1,z0),e=new Vector3(x0,y1,z0);
        Vector3 f=new Vector3(x0,y0,z1),g=new Vector3(x1,y0,z1),h=new Vector3(x1,y1,z1),j=new Vector3(x0,y1,z1);
        d.Q(e,c,b,a);d.Q(f,g,h,j);d.Q(a,b,g,f);d.Q(c,e,j,h);d.Q(a,f,j,e);d.Q(b,c,h,g);
    }
    private static void Disc(Data d,Vector3 center,float rx,float ry,float depth)
    {
        float z0=Mathf.Min(center.z,center.z+depth),z1=Mathf.Max(center.z,center.z+depth);
        for(int i=0;i<24;i++)
        {
            float a=i*Mathf.PI*2/24,b=(i+1)*Mathf.PI*2/24;
            Vector3 af=new Vector3(center.x+rx*Mathf.Cos(a),center.y+ry*Mathf.Sin(a),z0),bf=new Vector3(center.x+rx*Mathf.Cos(b),center.y+ry*Mathf.Sin(b),z0);
            Vector3 ab=new Vector3(af.x,af.y,z1),bb=new Vector3(bf.x,bf.y,z1);
            d.Tri(new Vector3(center.x,center.y,z0),bf,af);d.Tri(new Vector3(center.x,center.y,z1),ab,bb);d.Q(af,bf,bb,ab);
        }
    }
    private static Transform Organizer(string name,Transform parent)
    {GameObject go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);go.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(go,"Criar porta V24");return go.transform;}
    private static void ColliderBox(string name,Transform parent,Vector3 center,Vector3 size)
    {Transform t=Organizer(name,parent);t.localPosition=center;BoxCollider c=Undo.AddComponent<BoxCollider>(t.gameObject);c.size=size;}
    private static void Build(string name,Data d,Transform parent,Material material)
    {
        var uv=new List<Vector2>();for(int i=0;i<d.p.Count;i++)uv.Add(Vector2.zero);
        foreach(Face face in d.f)
        {
            var ids=face.indexes;Vector3 n=Vector3.Cross(d.p[ids[1]]-d.p[ids[0]],d.p[ids[2]]-d.p[ids[0]]).normalized;
            foreach(int id in ids){Vector3 p=d.p[id];uv[id]=Mathf.Abs(n.z)>.5f?new Vector2(p.x/.8f,p.y/2.58f):Mathf.Abs(n.x)>.5f?new Vector2(p.z/.8f,p.y/2.58f):new Vector2(p.x/.8f,p.z/2.58f);}
            face.manualUV=true;
        }
        ProBuilderMesh pb=ProBuilderMesh.Create(d.p,d.f);SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(parent,false);
        pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;pb.name=name;
        Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar malha V24");pb.GetComponent<MeshRenderer>().sharedMaterial=material;pb.textures=uv;pb.ToMesh();pb.Refresh();
        MeshCollider c=pb.GetComponent<MeshCollider>();if(c!=null)Undo.DestroyObjectImmediate(c);
    }
    private static Material MaterialCopy(string name,Material source,Color color,float metallic,float smooth)
    {
        string path="Assets/_Game/Art/Materials/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
        m=source!=null?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,path);AssetDatabase.SaveAssets();return m;
    }
    private static Transform Find(Transform root,string name,bool active)
    {foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && (!active || t.gameObject.activeInHierarchy))return t;return null;}
    private static void Tell(string text){EditorUtility.DisplayDialog("Porta de referencia V24",text,"OK");}
}
#endif
