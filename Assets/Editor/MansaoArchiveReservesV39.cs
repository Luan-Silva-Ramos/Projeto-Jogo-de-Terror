#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Arquiva apenas reservas explicitamente conferidas. Nao executa ao importar.
public static class MansaoArchiveReservesV39
{
    const string ArchivePath="Assets/_Game/Scenes/Archives/Mansion_Reserves_BeforeDemo_V39.unity";
    static readonly string[] Targets={
        "UpperFloor_V2/Exterior_Roofs/Central_Gable_Reduced_V30/Roof_WholeHouse_Joined_V05_Slate_V20_Reduced_V30",
        "UpperFloor_V2/Exterior_Roofs/Central_Gable_Reduced_V30/Fronton_Integrated_Roof_V08_Slate_V20_Reduced_V30",
        "UpperFloor_V2/Exterior_Roofs/Central_Gable_Reduced_V30/Roof_WholeHouse_Joined_V05_Seams_V20_Reduced_V30",
        "UpperFloor_V2/Facade_Details/Central_OgivalWindow_V27",
        "UpperFloor_V2/Facade_Details/Central_OgivalWindow_V28",
        "UpperFloor_V2/Exterior_Roofs/Roof_Front_Assembly_V04",
        "UpperFloor_V2/Exterior_Roofs/Fronton_Central_Curved_V07",
        "UpperFloor_V2/Exterior_Roofs/Slate_Roofs_V19"
    };
    [MenuItem("Tools/Mansao/Organizacao/Arquivar reservas antigas V39")]
    static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Saia do Play Mode primeiro.");return;}
        Transform mansion=Selection.activeTransform;while(mansion!=null && mansion.name!="Mansion")mansion=mansion.parent;
        if(mansion==null){Tell("Selecione Mansion ou um objeto dentro dela.");return;}
        Scene main=mansion.gameObject.scene,archive=default(Scene);int group=-1;bool copied=false;
        try
        {
            if(SceneManager.sceneCount!=1)throw new InvalidOperationException("Mantenha somente a cena da mansao aberta para executar este arquivamento.");
            if(main.path!="Assets/_Game/Scenes/Tests/Mansion_Facade_Chamfer_Study.unity")throw new InvalidOperationException("A cena aberta nao e a cena conferida da mansao.");
            if(main.isDirty)throw new InvalidOperationException("Salve a cena com Ctrl+S antes de arquivar. O comando nao salva automaticamente a cena principal.");
            if(File.Exists(ArchivePath))throw new InvalidOperationException("O arquivo V39 ja existe. Nada foi substituido; nao apague esse arquivo para repetir a operacao.");
            var originals=new List<Transform>();var paths=new List<string>();var hashes=new List<string>();
            foreach(string path in Targets)
            {
                Transform target=mansion.Find(path);if(target==null)throw new InvalidOperationException("Reserva nao encontrada: "+path);
                foreach(Renderer r in target.GetComponentsInChildren<Renderer>(true))if(r.enabled && r.gameObject.activeInHierarchy)throw new InvalidOperationException("Uma reserva esta renderizando: "+path);
                foreach(Collider c in target.GetComponentsInChildren<Collider>(true))if(c.enabled && c.gameObject.activeInHierarchy)throw new InvalidOperationException("Uma reserva tem collider ativo: "+path);
                foreach(Behaviour b in target.GetComponentsInChildren<Behaviour>(true))if(b.enabled && b.gameObject.activeInHierarchy)throw new InvalidOperationException("Uma reserva tem componente ativo: "+path);
                originals.Add(target);paths.Add(FullPath(target));hashes.Add(Signature(target));
            }
            // Uma copia completa de cena preserva as malhas nativas embutidas no proprio arquivo.
            // Depois podam-se somente os objetos dessa COPIA; nao se compartilham Mesh de outra cena.
            EnsureFolder("Assets/_Game/Scenes/Archives");
            if(!EditorSceneManager.SaveScene(main,ArchivePath,true))throw new InvalidOperationException("Nao foi possivel criar a copia de arquivo.");copied=true;
            archive=EditorSceneManager.OpenScene(ArchivePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(main);
            var keep=new HashSet<GameObject>();
            for(int i=0;i<paths.Count;i++)
            {
                Transform target=FindPath(archive,paths[i]);if(target==null || Signature(target)!=hashes[i])throw new InvalidOperationException("A copia inicial nao confere em "+paths[i]);
                foreach(Transform t in target.GetComponentsInChildren<Transform>(true))keep.Add(t.gameObject);
                for(Transform t=target;t!=null;t=t.parent)keep.Add(t.gameObject);
            }
            foreach(GameObject root in archive.GetRootGameObjects())Prune(root,keep);
            if(!EditorSceneManager.SaveScene(archive))throw new InvalidOperationException("Nao foi possivel salvar o arquivo de reservas reduzido.");
            EditorSceneManager.CloseScene(archive,true);archive=EditorSceneManager.OpenScene(ArchivePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(main);
            for(int i=0;i<paths.Count;i++)
            {
                Transform target=FindPath(archive,paths[i]);if(target==null || Signature(target)!=hashes[i])throw new InvalidOperationException("A verificacao apos reabrir falhou em "+paths[i]);
            }
            EditorSceneManager.CloseScene(archive,true);archive=default(Scene);SceneManager.SetActiveScene(main);
            // Somente apos salvar, reabrir e conferir todos os alvos, removem-se os originais da cena principal.
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Arquivar reservas antigas V39");
            foreach(Transform t in originals)Undo.DestroyObjectImmediate(t.gameObject);
            EditorSceneManager.MarkSceneDirty(main);Undo.CollapseUndoOperations(group);Selection.activeGameObject=mansion.gameObject;AssetDatabase.SaveAssets();
            double size=new FileInfo(ArchivePath).Length/1048576d;
            Debug.Log("V39: oito reservas arquivadas e verificadas por hash de hierarquia, geometria, UVs e materiais. Arquivo: "+ArchivePath+" ("+size.ToString("F2")+" MiB). Confira a mansao e salve com Ctrl+S para atualizar o tamanho da cena principal. Objetos ativos e prototipos V37/V38 preservados. Ctrl+Z desfaz a remocao na cena principal; o arquivo de reservas permanece.",mansion);
            Tell("Reservas arquivadas e verificadas.\n\nArquivo: "+ArchivePath+"\nTamanho do arquivo: "+size.ToString("F2")+" MiB.\n\nConfira a mansao. Depois salve com Ctrl+S para atualizar o arquivo principal. Nao apague a cena de reservas.");
        }
        catch(Exception e)
        {
            if(group>=0)Undo.RevertAllDownToGroup(group);
            if(archive.IsValid() && archive.isLoaded)EditorSceneManager.CloseScene(archive,true);
            if(main.IsValid() && main.isLoaded)SceneManager.SetActiveScene(main);
            Debug.LogException(e);Tell("Arquivamento interrompido: "+e.Message+(copied?"\nA copia criada foi mantida para inspecao; nenhuma reserva foi descartada. Consulte a Console.":"\nNenhuma reserva foi removida."));
        }
    }
    static string Signature(Transform root)
    {
        using(var memory=new MemoryStream())using(var w=new BinaryWriter(memory))
        {
            foreach(Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                w.Write(AnimationUtility.CalculateTransformPath(t,root));w.Write(t.name);w.Write(t.gameObject.activeSelf);w.Write(t.gameObject.layer);
                Write(w,t.localPosition);Write(w,t.localScale);Quaternion q=t.localRotation;w.Write(q.x);w.Write(q.y);w.Write(q.z);w.Write(q.w);
                foreach(Component c in t.GetComponents<Component>())w.Write(c==null?"MISSING":c.GetType().FullName);
                foreach(MeshFilter filter in t.GetComponents<MeshFilter>())
                {
                    Mesh mesh=filter.sharedMesh;if(mesh==null){w.Write(-1);continue;}if(!mesh.isReadable)throw new InvalidOperationException("Malha nao legivel em "+t.name);
                    Vector3[] vertices=mesh.vertices;w.Write(vertices.Length);foreach(Vector3 p in vertices)Write(w,p);
                    Vector2[] uv=mesh.uv;w.Write(uv.Length);foreach(Vector2 p in uv){w.Write(p.x);w.Write(p.y);}
                    w.Write(mesh.subMeshCount);for(int s=0;s<mesh.subMeshCount;s++){int[] ids=mesh.GetIndices(s);w.Write(ids.Length);foreach(int id in ids)w.Write(id);}
                }
                foreach(Renderer r in t.GetComponents<Renderer>()){w.Write(r.enabled);foreach(Material material in r.sharedMaterials){w.Write(material==null?"NULL":material.name);w.Write(material==null?"":AssetDatabase.GetAssetPath(material));}}
            }
            w.Flush();using(SHA256 hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(memory.ToArray()));
        }
    }
    static void Write(BinaryWriter writer,Vector3 v){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}
    static void Prune(GameObject go,HashSet<GameObject> keep)
    {
        if(!keep.Contains(go)){UnityEngine.Object.DestroyImmediate(go);return;}
        var children=new List<GameObject>();foreach(Transform child in go.transform)children.Add(child.gameObject);foreach(GameObject child in children)Prune(child,keep);
    }
    static string FullPath(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
    static Transform FindPath(Scene scene,string path)
    {
        foreach(GameObject root in scene.GetRootGameObjects()){if(path==root.name)return root.transform;if(path.StartsWith(root.name+"/",StringComparison.Ordinal))return root.transform.Find(path.Substring(root.name.Length+1));}return null;
    }
    static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');EnsureFolder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
    static void Tell(string message){EditorUtility.DisplayDialog("Arquivo de reservas V39",message,"OK");}
}
#endif
