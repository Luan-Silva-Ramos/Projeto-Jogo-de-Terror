#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
public static class MansaoKitchenPlasterV41
{
    const string Path="Assets/_Game/Art/Materials/M_Plaster_Kitchen_Interior_V41.mat";
    class Item {public ProBuilderMesh mesh;public List<Face> faces;}
    [MenuItem("Tools/Mansao/Interiores/Aplicar reboco interno da cozinha V41")]
    static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode.");return;}
        Transform root=Selection.activeTransform;while(root!=null && root.name!="Mansion")root=root.parent;
        if(root==null){Tell("Selecione Mansion ou um objeto dentro dela.");return;}
        int group=-1;
        try {
            string[] names={"Wall_Kitchen_Front_Right","Wall_Kitchen_Front_Left","Wall_Kitchen_Front_AboveDoor","Wall_Kitchen_Back_Left","Wall_Kitchen_Back_Right","Wall_Kitchen_Back_AboveDoor","Wall_Kitchen_Left_Front","Wall_Kitchen_Left_Back","Wall_Kitchen_Left_AboveDoor","Wall_Kitchen_Right_Front","Wall_Kitchen_Right_Back","Wall_Kitchen_Right_AboveDoor"};
            var items=new List<Item>();int count=0;
            foreach(string name in names){
                Transform found=null;foreach(Transform t in root.GetComponentsInChildren<Transform>(true))if(t.name==name && t.gameObject.activeInHierarchy){if(found!=null)throw new Exception("Nome ativo duplicado: "+name);found=t;}
                if(found==null)throw new Exception("Parede ativa nao encontrada: "+name);
                var pb=found.GetComponent<ProBuilderMesh>();if(pb==null)throw new Exception("Parede sem ProBuilder: "+name);
                int axis=name.Contains("_Left_")?0:name.Contains("_Right_")?0:2;
                // Front_Right e Back_Right sao trechos frontal/traseiro, nao a parede direita.
                if(name.StartsWith("Wall_Kitchen_Front_") || name.StartsWith("Wall_Kitchen_Back_"))axis=2;
                float plane=axis==0?(name.StartsWith("Wall_Kitchen_Left_")?-10.2f:-4.2f):(name.StartsWith("Wall_Kitchen_Front_")?4f:10f);
                var faces=new List<Face>();foreach(Face face in pb.faces){bool match=true;foreach(int id in face.distinctIndexes)if(Mathf.Abs(found.TransformPoint(pb.positions[id])[axis]-plane)>.005f){match=false;break;}if(match)faces.Add(face);}
                if(faces.Count==0)throw new Exception("Face interna nao encontrada nas medidas registradas: "+name);
                items.Add(new Item{mesh=pb,faces=faces});count+=faces.Count;
            }
            Material source=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/M_Plaster_Exterior.mat");if(source==null)throw new Exception("Material de reboco de origem nao encontrado.");
            Material mat=AssetDatabase.LoadAssetAtPath<Material>(Path);
            if(mat==null){mat=new Material(source);mat.name="M_Plaster_Kitchen_Interior_V41";Color c;ColorUtility.TryParseHtmlString("#E5DCC8",out c);mat.SetColor("_BaseColor",c);if(mat.HasProperty("_Color"))mat.SetColor("_Color",c);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_BumpScale",.20f);AssetDatabase.CreateAsset(mat,Path);AssetDatabase.SaveAssets();}
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Aplicar reboco interno da cozinha V41");
            foreach(Item item in items){Undo.RecordObject(item.mesh,"Material das faces internas");Undo.RecordObject(item.mesh.GetComponent<MeshRenderer>(),"Material interno");item.mesh.SetMaterial(item.faces,mat);item.mesh.ToMesh();item.mesh.Refresh();EditorUtility.SetDirty(item.mesh);EditorUtility.SetDirty(item.mesh.GetComponent<MeshRenderer>());}
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);Undo.CollapseUndoOperations(group);
            Debug.Log("V41: reboco aplicado em "+count+" faces de 12 trechos de parede. Geometria, UVs existentes, faces externas e acesso ao porao preservados. Escala visual da textura ainda deve ser conferida na Unity.",root);
            Tell("Reboco aplicado nas faces internas de 12 trechos de parede. Confira o resultado antes de salvar a cena. Ctrl+Z desfaz a aplicacao.");
        }catch(Exception e){if(group>=0)Undo.RevertAllDownToGroup(group);Debug.LogException(e);Tell("O lote nao foi concluido: "+e.Message);}
    }
    static void Tell(string s){EditorUtility.DisplayDialog("Reboco interno da cozinha V41",s,"OK");}
}
#endif
