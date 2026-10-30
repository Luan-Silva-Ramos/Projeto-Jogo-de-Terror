#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoGardenAmberV35
{
    const string MatFolder="Assets/_Game/Art/Materials/";
    struct Point {public Vector3 p;public Vector2 uv;public Point(Vector3 p,Vector2 uv){this.p=p;this.uv=uv;}}
    class Job {public Transform panel;public ProBuilderMesh source;public float cx,amberArea;public string side;public Material clear;public List<Vertex> vertices=new List<Vertex>();public List<Vector3> positions=new List<Vector3>();public List<Face> faces=new List<Face>();}
    [MenuItem("Tools/Mansao/Concluir ambar dos dois losangos V35")]
    static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        Transform garden=mansion==null?null:mansion.Find("GroundFloor_V2/WinterGarden");if(garden==null){Tell("Selecione Mansion ou um objeto dentro dela.");return;}
        int group=-1;
        try
        {
            Job right=Prepare(garden,"FrontRight",.194125f,"Right"),left=Prepare(garden,"FrontLeft",-.194125f,"Left");
            Material amberSource=AssetDatabase.LoadAssetAtPath<Material>(MatFolder+"M_WinterGarden_Glass_Amber.mat");if(amberSource==null)throw new InvalidOperationException("Material de vidro ambar existente nao encontrado.");
            Material amber=PrivateMaterial(amberSource,"M_WinterGarden_Front_CentralAmber_V35",true);
            right.clear=PrivateMaterial(right.source.GetComponent<MeshRenderer>().sharedMaterial,"M_WinterGarden_FrontRight_Clear_V35",false);
            left.clear=PrivateMaterial(left.source.GetComponent<MeshRenderer>().sharedMaterial,"M_WinterGarden_FrontLeft_Clear_V35",false);
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Concluir dois losangos ambar V35");
            GameObject selected=Build(right,amber);Build(left,amber);
            EditorSceneManager.MarkSceneDirty(garden.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=selected;
            Debug.Log("V35: somente o centro dos losangos dos dois paineis encurtados recebeu vidro ambar. Regioes claras preservadas, materiais exclusivos e nenhuma placa sobreposta. Vidros originais guardados desativados. Barras, caixilhos, telhado e demais paineis nao foram alterados. Ctrl+Z desfaz o lote.",selected);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi aplicado: "+e.Message);}
    }
    static Job Prepare(Transform garden,string side,float cx,string diamond)
    {
        Transform panel=garden.Find("WinterGarden_"+side+"_Assembly");if(panel==null || !panel.gameObject.activeInHierarchy)throw new InvalidOperationException("Painel "+side+" nao encontrado ativo.");
        if(panel.Find("Central_Amber_Glass_V35")!=null)throw new InvalidOperationException("A V35 ja existe no painel "+side+".");
        Transform glass=panel.Find("WinterGarden_BackRight_Glass");ProBuilderMesh source=glass==null?null:glass.GetComponent<ProBuilderMesh>();
        if(source==null || !glass.gameObject.activeInHierarchy)throw new InvalidOperationException("Vidro original ativo nao encontrado em "+side);
        Quaternion expectedRotation=side=="FrontLeft"?Quaternion.Euler(0,180,0):Quaternion.identity;
        if(Vector3.Distance(glass.localPosition,new Vector3(cx,1.75f,0))>.002f || Quaternion.Angle(glass.localRotation,expectedRotation)>.1f || Vector3.Distance(glass.localScale,Vector3.one)>.001f)
            throw new InvalidOperationException("Transform do vidro encurtado difere das medidas aprovadas em "+side);
        Vector3 lo=Vector3.one*1000,hi=Vector3.one*-1000;
        foreach(Vector3 p in source.positions){Vector3 q=panel.InverseTransformPoint(glass.TransformPoint(p));lo=Vector3.Min(lo,q);hi=Vector3.Max(hi,q);}
        if(Vector3.Distance(hi-lo,new Vector3(1.20825f,3.32f,.008f))>.003f)throw new InvalidOperationException("O vidro "+side+" nao tem as dimensoes encurtadas esperadas.");
        Vector3 barCenter=Vector3.zero;string[] corners={"UL","UR","LL","LR"};
        foreach(string corner in corners){Transform bar=panel.Find("WinterGarden_BackRight_Diamond_"+diamond+"_"+corner);if(bar==null || !bar.gameObject.activeInHierarchy)throw new InvalidOperationException("Uma barra do losango aprovado esta ausente em "+side);barCenter+=bar.localPosition;}
        barCenter/=4;if(Vector3.Distance(barCenter,new Vector3(cx,2.95f,0))>.002f)throw new InvalidOperationException("O losango nao esta centralizado na posicao aprovada em "+side);
        foreach(Transform t in panel.GetComponentsInChildren<Transform>(true))if(t.gameObject.activeInHierarchy && (t.name=="WinterGarden_BackRight_Bar_V" || t.name=="WinterGarden_BackRight_UpperBar_Center"))throw new InvalidOperationException("Existe uma barra central ativa em "+side+". O instalador nao altera barras.");
        Material[] materials=source.GetComponent<MeshRenderer>().sharedMaterials;if(materials.Length!=1 || materials[0]==null || materials[0].name!="M_WinterGarden_Glass")throw new InvalidOperationException("O vidro "+side+" precisa usar o material claro aprovado.");
        Job job=new Job{panel=panel,source=source,cx=cx,side=side};IList<Vector2> uvs=source.textures;
        foreach(Face face in source.faces){var ids=face.indexes;for(int i=0;i<ids.Count;i+=3){var polygon=new List<Point>();for(int k=0;k<3;k++){int id=ids[i+k];polygon.Add(new Point(panel.InverseTransformPoint(glass.TransformPoint(source.positions[id])),uvs!=null && uvs.Count==source.vertexCount?uvs[id]:Vector2.zero));}
            var polygons=new List<List<Point>>{polygon};foreach(int sx in new[]{-1,1})foreach(int sy in new[]{-1,1})polygons=Split(polygons,sx,sy,cx);
            foreach(var part in polygons)for(int k=1;k<part.Count-1;k++){
                Point[] tri={part[0],part[k],part[k+1]};Vector3 cross=Vector3.Cross(tri[1].p-tri[0].p,tri[2].p-tri[0].p);if(cross.sqrMagnitude<1e-12f)continue;
                Vector3 center=(tri[0].p+tri[1].p+tri[2].p)/3;bool inside=Mathf.Abs(center.x-cx)+Mathf.Abs(center.y-2.95f)<=.150001f;
                int start=job.vertices.Count;foreach(Point p in tri){Vector3 local=glass.InverseTransformPoint(panel.TransformPoint(p.p));job.positions.Add(local);job.vertices.Add(new Vertex{position=local,uv0=p.uv});}
                Face next=new Face(new[]{start,start+1,start+2});next.manualUV=true;next.smoothingGroup=face.smoothingGroup;next.submeshIndex=inside?1:0;job.faces.Add(next);if(inside)job.amberArea+=cross.magnitude*.5f;
            }
        }}
        if(job.faces.Count==0 || job.faces.Count>800 || Mathf.Abs(job.amberArea-.09f)>.001f)throw new InvalidOperationException("A divisao do losango falhou em "+side+". Area: "+job.amberArea.ToString("F5"));return job;
    }
    static float Distance(Point p,int sx,int sy,float cx){return sx*(p.p.x-cx)+sy*(p.p.y-2.95f)-.15f;}
    static List<List<Point>> Split(List<List<Point>> input,int sx,int sy,float cx)
    {
        var output=new List<List<Point>>();foreach(var polygon in input){float lo=float.PositiveInfinity,hi=float.NegativeInfinity;foreach(Point p in polygon){float d=Distance(p,sx,sy,cx);lo=Mathf.Min(lo,d);hi=Mathf.Max(hi,d);}if(lo>=-.000001f || hi<=.000001f){output.Add(polygon);continue;}var a=Clip(polygon,sx,sy,cx,true);var b=Clip(polygon,sx,sy,cx,false);if(a.Count>=3)output.Add(a);if(b.Count>=3)output.Add(b);}return output;
    }
    static List<Point> Clip(List<Point> input,int sx,int sy,float cx,bool positive)
    {
        var output=new List<Point>();Point a=input[input.Count-1];float da=(positive?1:-1)*Distance(a,sx,sy,cx);foreach(Point b in input){float db=(positive?1:-1)*Distance(b,sx,sy,cx);if((da>=0)!=(db>=0)){float t=da/(da-db);output.Add(new Point(Vector3.Lerp(a.p,b.p,t),Vector2.Lerp(a.uv,b.uv,t)));}if(db>=0)output.Add(b);a=b;da=db;}return output;
    }
    static Material PrivateMaterial(Material source,string name,bool amber)
    {
        string path=MatFolder+name+".mat";Material material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
        material=new Material(source);material.name=name;
        if(amber){if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",null);if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",null);Color tint=new Color(201/255f,145/255f,61/255f,.45f);if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",tint);if(material.HasProperty("_Color"))material.SetColor("_Color",tint);if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",Color.black);material.DisableKeyword("_EMISSION");}
        AssetDatabase.CreateAsset(material,path);AssetDatabase.SaveAssets();return material;
    }
    static GameObject Build(Job job,Material amber)
    {
        Transform original=job.source.transform;GameObject root=new GameObject("Central_Amber_Glass_V35");SceneManager.MoveGameObjectToScene(root,original.gameObject.scene);root.transform.SetParent(job.panel,false);Undo.RegisterCreatedObjectUndo(root,"Criar vidro com ambar central");
        ProBuilderMesh pb=ProBuilderMesh.Create(job.vertices,job.faces,SharedVertex.GetSharedVerticesWithPositions(job.positions),null,new[]{job.clear,amber});SceneManager.MoveGameObjectToScene(pb.gameObject,original.gameObject.scene);pb.transform.SetParent(root.transform,false);pb.transform.localPosition=original.localPosition;pb.transform.localRotation=original.localRotation;pb.transform.localScale=original.localScale;pb.name="WinterGarden_"+job.side+"_Glass_AmberCenter_V35";pb.gameObject.layer=original.gameObject.layer;Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar malha do vidro V35");pb.ToMesh();pb.Refresh();
        foreach(Collider c in pb.GetComponents<Collider>())Undo.DestroyObjectImmediate(c);foreach(Collider c in original.GetComponents<Collider>()){Collider copied=(Collider)Undo.AddComponent(pb.gameObject,c.GetType());EditorUtility.CopySerialized(c,copied);if(copied is MeshCollider mesh)mesh.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;}
        MeshRenderer before=original.GetComponent<MeshRenderer>(),after=pb.GetComponent<MeshRenderer>();after.shadowCastingMode=before.shadowCastingMode;after.receiveShadows=before.receiveShadows;after.sortingLayerID=before.sortingLayerID;after.sortingOrder=before.sortingOrder;
        if(Vector3.Distance(before.bounds.min,after.bounds.min)>.001f || Vector3.Distance(before.bounds.max,after.bounds.max)>.001f)throw new InvalidOperationException("A copia nao preservou os limites do vidro em "+job.side);
        Undo.RecordObject(original.gameObject,"Preservar vidro claro original como reserva");original.gameObject.SetActive(false);return root;
    }
    static void Tell(string message){EditorUtility.DisplayDialog("Losangos ambar V35",message,"OK");}
}
#endif

