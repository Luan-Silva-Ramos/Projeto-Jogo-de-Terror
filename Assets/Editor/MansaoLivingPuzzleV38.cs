#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MansaoLivingPuzzleV38
{
    const string Prototype="DEMO_TESTE_TEMPORARIO_V37",Room="SalaEstar_Puzzle_Temporario_V38";
    [MenuItem("Tools/Mansao/Demo/Transferir puzzle para sala de estar V38")]
    static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Mansion(),prototype=mansion==null?null:mansion.Find(Prototype);
        if(prototype==null){Tell("Selecione Mansion com o prototipo V37 instalado.");return;}
        if(prototype.Find(Room)!=null){Tell("A transferencia V38 ja existe. Nada foi duplicado.");return;}
        int group=-1;
        try
        {
            Check(mansion,"Floor_Living",new Vector3(-10.50072f,-.2f,-8.3022f),new Vector3(-4.00072f,0,1.1478f));
            Check(mansion,"Wall_Living_Back_Left",new Vector3(-10.2f,0,1),new Vector3(-6.75f,3.5f,1.2f));
            Check(mansion,"Wall_Living_Back_AboveDoor",new Vector3(-6.75f,2.2f,1),new Vector3(-5.65f,3.5f,1.2f));
            Check(mansion,"Wall_Atrium_Left_AboveLivingDoor",new Vector3(-4.2f,2.2f,-2.55f),new Vector3(-4,3.5f,-1.45f));
            bool westWallFound=false;foreach(Transform t in mansion.GetComponentsInChildren<Transform>(true)){if(!t.gameObject.activeInHierarchy || !t.name.StartsWith("Wall_"))continue;Renderer wall=t.GetComponent<Renderer>();if(wall==null)continue;Bounds b=wall.bounds;if(Mathf.Abs(b.max.x+10.2f)<.025f && b.min.x< -10.49f && b.min.y<.01f && b.max.y>3.49f && b.min.z< -1.85f && b.max.z>.4f)westWallFound=true;}
            if(!westWallFound)throw new InvalidOperationException("O trecho livre da parede oeste nao corresponde a verificacao.");
            if(Vector3.Distance(prototype.lossyScale,Vector3.one)>.001f)throw new InvalidOperationException("O prototipo deve manter escala mundial 1.");
            string[] people={"CHARLES","ELEANOR","DAVID","CLARA"};var originals=new List<GameObject>();
            foreach(string person in people){originals.Add(Required(prototype,"Retrato_Temporario_"+person).gameObject);originals.Add(Required(prototype,"Nome_Retrato_"+person).gameObject);}
            Transform fire=Required(prototype,"Lareira_Volume_Com_Colisao"),fireName=Required(prototype,"Nome_Lareira"),chair=Required(prototype,"Cadeira_Galeria_Teste"),access=Required(prototype,"Acesso_Galeria");
            originals.Add(fire.gameObject);originals.Add(fireName.gameObject);originals.Add(chair.gameObject);originals.Add(access.gameObject);
            Material route=access.GetComponent<Renderer>().sharedMaterial,furniture=fire.GetComponent<Renderer>().sharedMaterial;
            if(route==null || furniture==null)throw new InvalidOperationException("Materiais de teste V37 nao encontrados.");
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Transferir prototipo para sala de estar V38");
            GameObject root=new GameObject(Room);SceneManager.MoveGameObjectToScene(root,prototype.gameObject.scene);root.transform.SetParent(prototype,false);Undo.RegisterCreatedObjectUndo(root,"Criar teste do salao de retratos");
            var backup=Undo.AddComponent<MansaoLivingPuzzleBackupV38>(root);backup.previousObjects=originals.ToArray();backup.previousActive=new bool[originals.Count];for(int i=0;i<originals.Count;i++)backup.previousActive[i]=originals[i].activeSelf;
            for(int i=0;i<4;i++)
            {
                // Ordenacao do lado proximo as portas em direcao a lareira oeste.
                float x=-7.25f-.80f*i;
                Clone(Required(prototype,"Retrato_Temporario_"+people[i]),root.transform,"Retrato_Sala_"+people[i],new Vector3(x,1.65f,.955f),Quaternion.identity);
                Clone(Required(prototype,"Nome_Retrato_"+people[i]),root.transform,"Nome_Sala_"+people[i],new Vector3(x,1.65f,.920f),Quaternion.identity);
            }
            GameObject fireplace=Clone(fire,root.transform,"Lareira_Sala_Volume_Com_Colisao",new Vector3(-9.97f,.75f,-.50f),Quaternion.identity);fireplace.transform.localScale=new Vector3(.42f,1.50f,1.40f);
            Clone(fireName,root.transform,"Nome_Lareira_Sala",new Vector3(-9.745f,1.10f,-.50f),Quaternion.Euler(0,-90,0));
            Clone(chair,root.transform,"Cadeira_Susto_Sala_Teste",new Vector3(-7.6f,0,-3.4f),Quaternion.identity);
            Cube(root.transform,"Bordado_Reserva_Temporaria",new Vector3(-10.16f,1.75f,-1.60f),new Vector3(.04f,.45f,.50f),route);
            GameObject embroideryName=Clone(fireName,root.transform,"Nome_Reserva_Bordado",new Vector3(-10.125f,1.75f,-1.60f),Quaternion.Euler(0,-90,0));TextMesh text=embroideryName.GetComponent<TextMesh>();if(text!=null)text.text="RESERVA\nBORDADO";
            Vector3[] points={new Vector3(5.4f,.032f,2),new Vector3(3.65f,.032f,2),new Vector3(3.65f,.032f,2.85f),new Vector3(0,.032f,2.85f),new Vector3(0,.032f,-2),new Vector3(-4.65f,.032f,-2),new Vector3(-6.5f,.032f,-1.0f)};
            for(int i=0;i<points.Length-1;i++)Line(root.transform,"Percurso_Biblioteca_Sala_"+i,points[i],points[i+1],route);
            foreach(GameObject go in originals){Undo.RecordObject(go,"Guardar volumes traseiros como reserva");go.SetActive(false);}
            EditorUtility.SetDirty(backup);EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);Undo.CollapseUndoOperations(group);Selection.activeGameObject=root;
            Debug.Log("V38: prototipo do puzzle transferido para a sala de estar. Volumes traseiros guardados desativados; parede, piso, janelas, portas e iluminacao nao alterados. Cronometro e rota de fuga V37 preservados. A chamine ainda e apenas uma reserva de projeto.",root);
        }
        catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi aplicado: "+e.Message);}
    }
    [MenuItem("Tools/Mansao/Demo/Desfazer transferencia para sala V38")]
    static void Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Mansion(),prototype=mansion==null?null:mansion.Find(Prototype),room=prototype==null?null:prototype.Find(Room);var backup=room==null?null:room.GetComponent<MansaoLivingPuzzleBackupV38>();
        if(backup==null){Tell("Selecione Mansion com a transferencia V38 instalada.");return;}
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Desfazer transferencia V38");
        for(int i=0;i<backup.previousObjects.Length;i++){GameObject go=backup.previousObjects[i];if(go!=null){Undo.RecordObject(go,"Restaurar volume anterior");go.SetActive(backup.previousActive[i]);}}
        Undo.DestroyObjectImmediate(room.gameObject);EditorSceneManager.MarkSceneDirty(mansion.gameObject.scene);Undo.CollapseUndoOperations(group);
    }
    static GameObject Clone(Transform source,Transform parent,string name,Vector3 position,Quaternion rotation)
    {
        GameObject copy=UnityEngine.Object.Instantiate(source.gameObject,parent,true);copy.name=name;copy.transform.position=position;copy.transform.rotation=rotation;copy.SetActive(true);Undo.RegisterCreatedObjectUndo(copy,"Copiar volume temporario");return copy;
    }
    static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);go.transform.SetParent(parent,false);go.transform.position=position;go.transform.rotation=Quaternion.identity;go.transform.localScale=size;go.name=name;Undo.RegisterCreatedObjectUndo(go,"Criar reserva de bordado");go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Undo.DestroyObjectImmediate(go.GetComponent<BoxCollider>());return go;
    }
    static void Line(Transform parent,string name,Vector3 a,Vector3 b,Material material)
    {
        GameObject go=Cube(parent,name,(a+b)*.5f,new Vector3(.045f,.012f,Vector3.Distance(a,b)),material);go.transform.rotation=Quaternion.LookRotation((b-a).normalized,Vector3.up);
    }
    static Transform Required(Transform parent,string name){Transform t=parent.Find(name);if(t==null)throw new InvalidOperationException("Objeto temporario nao encontrado: "+name);return t;}
    static Transform Mansion(){Transform t=Selection.activeTransform;while(t!=null && t.name!="Mansion")t=t.parent;return t;}
    static void Check(Transform mansion,string name,Vector3 min,Vector3 max)
    {
        Renderer found=null;foreach(Transform t in mansion.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy){if(found!=null)throw new InvalidOperationException("Nome ativo duplicado: "+name);found=t.GetComponent<Renderer>();}
        if(found==null || Vector3.Distance(found.bounds.min,min)>.035f || Vector3.Distance(found.bounds.max,max)>.035f)throw new InvalidOperationException("As medidas de "+name+" diferem da cena conferida.");
    }
    static void Tell(string message){EditorUtility.DisplayDialog("Sala de estar - teste V38",message,"OK");}
}
#endif

