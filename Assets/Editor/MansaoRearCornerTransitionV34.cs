#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoRearCornerTransitionV34
{
    [MenuItem("Tools/Mansao/Arrematar degrau traseiro V34")]
    static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        Transform parent=mansion==null?null:mansion.Find("GroundFloor_V2/Exterior_Timber_Bands");
        if(parent==null){Tell("Selecione um objeto da Mansion.");return;}
        if(parent.Find("Rear_Corner_Transition_V34")!=null){Tell("A V34 ja existe. Nenhuma copia foi criada.");return;}
        int group=-1;
        try
        {
            // O nome legado tem dois L em Chilldren; e mantido para localizar a reserva correta.
            Transform joint=Find(mansion,"Timber_Joint_Chilldren_RearEast"),rear=Find(mansion,"Timber_Band_Children_Rear"),side=Find(mansion,"Timber_Band_Children_EastReturn"),glass=Find(mansion,"WinterGarden_Roof_Glass");
            Check(joint,new Vector3(4.66f,3.70f,11.56f),new Vector3(4.98f,4.02f,11.88f));
            Check(rear,new Vector3(-10.2f,3.70f,11.60f),new Vector3(4.8f,3.98f,11.84f));
            Check(side,new Vector3(4.70f,3.46f,8.30f),new Vector3(4.94f,3.74f,11.70f));
            Material wood=joint.GetComponent<Renderer>().sharedMaterial;if(wood==null)throw new InvalidOperationException("Material da juncao nao encontrado.");
            Bounds envelope=new Bounds();envelope.SetMinMax(new Vector3(4.64f,3.44f,11.54f),new Vector3(5.00f,4.04f,11.90f));
            if(glass.GetComponent<Renderer>()==null || envelope.min.x-glass.GetComponent<Renderer>().bounds.max.x<.50f)
                throw new InvalidOperationException("A cobertura de vidro esta mais proxima do que nas medidas conferidas.");
            // A nova peca permanece fora das faces internas superiores X 4.50 / Z 11.40.
            if(envelope.min.x<4.50f || envelope.min.z<11.40f)throw new InvalidOperationException("O arremate ultrapassaria o limite interno previsto.");
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Arrematar degrau traseiro V34");
            GameObject root=new GameObject("Rear_Corner_Transition_V34");SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);root.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(root,"Criar arremate traseiro");
            Box("Rear_Corner_TallJoint_V34",new Vector3(4.66f,3.46f,11.56f),new Vector3(4.98f,4.00f,11.88f),root.transform,wood,joint.gameObject.layer);
            Box("Rear_Corner_LowerCollar_V34",new Vector3(4.64f,3.44f,11.54f),new Vector3(5.00f,3.50f,11.90f),root.transform,wood,joint.gameObject.layer);
            Box("Rear_Corner_UpperCollar_V34",new Vector3(4.64f,3.98f,11.54f),new Vector3(5.00f,4.04f,11.90f),root.transform,wood,joint.gameObject.layer);
            Undo.RecordObject(joint.gameObject,"Preservar bloco anterior como reserva");joint.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);Selection.activeGameObject=root;Undo.CollapseUndoOperations(group);
            Debug.Log("V34 criada: juncao vertical localizada entre as faixas de alturas diferentes. Vigas, paredes, rufos, tampas e jardim preservados. Bloco anterior guardado desativado. Sem colliders novos. Ctrl+Z desfaz o lote.",root);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi aplicado: "+e.Message);}
    }
    static Transform Find(Transform mansion,string name)
    {
        Transform result=null;foreach(Transform t in mansion.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy){if(result!=null)throw new InvalidOperationException("Nome ativo duplicado: "+name);result=t;}
        if(result==null)throw new InvalidOperationException("Nao encontrei "+name+" ativo.");return result;
    }
    static void Check(Transform t,Vector3 min,Vector3 max)
    {
        Renderer r=t.GetComponent<Renderer>();if(r==null || Vector3.Distance(r.bounds.min,min)>.015f || Vector3.Distance(r.bounds.max,max)>.015f)
            throw new InvalidOperationException("As medidas de "+t.name+" diferem da cena conferida. Nada foi movido.");
    }
    static void Box(string name,Vector3 min,Vector3 max,Transform parent,Material material,int layer)
    {
        Vector3[] points={new Vector3(min.x,min.y,min.z),new Vector3(max.x,min.y,min.z),new Vector3(max.x,max.y,min.z),new Vector3(min.x,max.y,min.z),new Vector3(min.x,min.y,max.z),new Vector3(max.x,min.y,max.z),new Vector3(max.x,max.y,max.z),new Vector3(min.x,max.y,max.z)};
        int[][] quads={new[]{3,2,1,0},new[]{4,5,6,7},new[]{0,1,5,4},new[]{2,3,7,6},new[]{0,4,7,3},new[]{1,2,6,5}};
        var vertices=new List<Vertex>();var positions=new List<Vector3>();var faces=new List<Face>();
        foreach(int[] q in quads){Vector3 n=Vector3.Cross(points[q[1]]-points[q[0]],points[q[2]]-points[q[0]]).normalized;int start=vertices.Count;foreach(int id in q){Vector3 world=points[id],local=parent.InverseTransformPoint(world);Vector2 uv=Mathf.Abs(n.y)>.5f?new Vector2(world.x,world.z):new Vector2(Mathf.Abs(n.x)>.5f?world.z:world.x,world.y);vertices.Add(new Vertex{position=local,uv0=uv});positions.Add(local);}var face=new Face(new[]{start,start+1,start+2,start,start+2,start+3});face.manualUV=true;faces.Add(face);}
        ProBuilderMesh pb=ProBuilderMesh.Create(vertices,faces,SharedVertex.GetSharedVerticesWithPositions(positions),null,new[]{material});SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(parent,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;pb.gameObject.layer=layer;pb.name=name;Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar arremate de madeira");pb.ToMesh();pb.Refresh();MeshCollider collider=pb.GetComponent<MeshCollider>();if(collider!=null)Undo.DestroyObjectImmediate(collider);
    }
    static void Tell(string text){EditorUtility.DisplayDialog("Arremate traseiro V34",text,"OK");}
}
#endif
