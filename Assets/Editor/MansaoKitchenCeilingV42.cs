#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

// Delimita somente as faces inferiores. Nao reconstrui ProBuilderShape.
public static class MansaoKitchenCeilingV42
{
    const string CeilingPath = "Assets/_Game/Art/Materials/M_Plaster_Kitchen_Ceiling.mat";
    const string GridGuid = "c22777d6e868e4f2fb421913386b154e";
    const string StoneGuid = "8663514bc3b08684085516226054831a";
    const float Eps = .00001f;
    static readonly string[] Names = { "Floor_Upper_Child_01", "Floor_Upper_Child_02", "Floor_Upper_Gallery_Rear_Left_Link", "Floor_Upper_Bathroom", "Floor_Upper_LinenRoom", "Floor_Upper_Hall_Rear" };
    static readonly Vector3[] Centers = { new Vector3(-7.7f,3.6f,8.85f),new Vector3(-2.7f,3.6f,8.85f),new Vector3(-4.9f,3.6f,4),new Vector3(-7.2f,3.6f,2.9f),new Vector3(-9.5f,3.6f,2.9f),new Vector3(-2.3f,3.6f,5.3f) };
    class Plan { public ProBuilderMesh pb; public List<Vertex> vertices; public List<Face> faces; public Material[] materials; public Bounds bounds; public float area; }
    struct Point { public Vector3 world; public Vertex vertex; public Point(Vector3 w, Vertex v) { world=w; vertex=v; } }

    [MenuItem("Tools/Mansao/Interiores/Corrigir teto da cozinha V42 %&#k")]
    public static void Apply()
    {
        int undo=-1;
        try
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Saia do Play Mode.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/_Game/Scenes/Tests/Mansion_Facade_Chamfer_Study.unity") throw new Exception("Abra a cena Mansion_Facade_Chamfer_Study.");
            var mansion=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Mansion" && t.gameObject.activeInHierarchy).ToArray();
            if(mansion.Length!=1) throw new Exception("Mansion ativa deve ser unica.");
            Material ceiling=AssetDatabase.LoadAssetAtPath<Material>(CeilingPath);
            Material grid=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(GridGuid));
            Material stone=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(StoneGuid));
            if(ceiling==null || grid==null || stone==null) throw new Exception("Um dos materiais conferidos no projeto nao foi encontrado.");
            var plans=new List<Plan>();
            for(int i=0;i<Names.Length;i++)
            {
                var found=mansion[0].GetComponentsInChildren<ProBuilderMesh>(false).Where(p=>p.name==Names[i]).ToArray();
                if(found.Length!=1) throw new Exception("Laje ausente ou duplicada: "+Names[i]);
                var pb=found[0];
                if(Vector3.Distance(pb.transform.position,Centers[i])>.002f || Quaternion.Angle(pb.transform.rotation,Quaternion.identity)>.01f || Vector3.Distance(pb.transform.lossyScale,Vector3.one)>.001f) throw new Exception("Transform diferente do conferido: "+pb.name);
                plans.Add(Build(pb,ceiling,grid,stone));
            }
            float area=plans.Sum(p=>p.area);
            if(Mathf.Abs(area-36f)>.003f) throw new Exception("A cobertura encontrada nao corresponde aos 36 m2 da cozinha: "+area);
            if(plans.All(p=>IsCorrect(p.pb,ceiling))) { Tell("O teto ja esta delimitado. Nenhuma alteracao foi feita."); return; }
            // Salva uma copia do estado em memoria, inclusive alteracoes ainda nao salvas.
            string folder="Assets/_Game/Scenes/Archives";
            if(!AssetDatabase.IsValidFolder(folder)) throw new Exception("Pasta Archives nao encontrada; nenhuma alteracao foi feita.");
            string backup=AssetDatabase.GenerateUniqueAssetPath(folder+"/Mansion_Antes_Teto_Cozinha_V42_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");
            if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Nao foi possivel criar a copia de seguranca.");
            Undo.IncrementCurrentGroup(); undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Delimitar teto da cozinha V42");
            foreach(var p in plans)
            {
                var renderer=p.pb.GetComponent<MeshRenderer>();
                var objects=new List<UnityEngine.Object>{p.pb,renderer,p.pb.GetComponent<MeshFilter>()};
                objects.AddRange(p.pb.GetComponents<Collider>());
                var mesh=p.pb.GetComponent<MeshFilter>().sharedMesh;
                if(mesh!=null) objects.Add(mesh);
                Undo.RegisterCompleteObjectUndo(objects.ToArray(),"Corrigir faces inferiores do teto");
                p.pb.SetVertices(p.vertices); p.pb.faces=p.faces;
                p.pb.sharedVertices=SharedVertex.GetSharedVerticesWithPositions(p.vertices.Select(v=>v.position).ToList());
                renderer.sharedMaterials=p.materials;
                p.pb.ToMesh();
                // UV0 e cores sao copiados/interpolados; nao recalcular UV automaticamente.
                p.pb.Refresh(RefreshMask.Normals|RefreshMask.Tangents|RefreshMask.Collisions|RefreshMask.Bounds);
                if(Vector3.Distance(renderer.bounds.min,p.bounds.min)>.001f || Vector3.Distance(renderer.bounds.max,p.bounds.max)>.001f || !IsCorrect(p.pb,ceiling)) throw new Exception("A verificacao apos aplicar falhou: "+p.pb.name);
                EditorUtility.SetDirty(p.pb); EditorUtility.SetDirty(renderer);
            }
            EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(undo);
            SceneView.RepaintAll();
            Debug.Log("V42 concluido: teto da cozinha delimitado em 36 m2 / 6 lajes. Copia anterior: "+backup);
            Tell("Teto delimitado somente na cozinha (36 m2). Materiais indevidos nas seis lajes restaurados; pisos superiores, paredes e porao preservados.\n\nConfira antes de salvar. Ctrl+Z desfaz.\nCopia anterior: "+backup);
        }
        catch(Exception e) { if(undo>=0) Undo.RevertAllDownToGroup(undo); Debug.LogException(e); Tell("A correcao nao foi concluida: "+e.Message); }
    }
    static Plan Build(ProBuilderMesh pb,Material ceiling,Material grid,Material stone)
    {
        var renderer=pb.GetComponent<MeshRenderer>();
        if(renderer==null) throw new Exception("Laje sem MeshRenderer: "+pb.name);
        foreach(var c in pb.GetComponents<Collider>()) if(!(c is MeshCollider)) throw new Exception("Collider nao previsto em "+pb.name);
        var original=pb.GetVertices();
        var p=new Plan{pb=pb,vertices=original.Select(v=>new Vertex(v)).ToList(),faces=new List<Face>(),bounds=renderer.bounds};
        var mats=renderer.sharedMaterials.ToList();
        // Restauracao documentada no commit 4f0d65d; apenas slots contaminados.
        for(int i=0;i<mats.Count;i++) if(mats[i]==ceiling) mats[i]=(pb.name=="Floor_Upper_Child_02" && i==1)?stone:grid;
        int slot=mats.Count; mats.Add(ceiling); p.materials=mats.ToArray();
        foreach(var face in pb.faces)
        {
            if(face.submeshIndex<0 || face.submeshIndex>=slot) throw new Exception("Indice de material invalido em "+pb.name);
            var ix=face.indexes;
            bool bottom=face.distinctIndexes.All(id=>Mathf.Abs(pb.transform.TransformPoint(original[id].position).y-3.5f)<.001f);
            if(!bottom) { p.faces.Add(new Face(face)); continue; }
            for(int j=0;j<ix.Count;j+=3)
            {
                var poly=new List<Point>();
                for(int k=0;k<3;k++) {var v=new Vertex(original[ix[j+k]]); poly.Add(new Point(pb.transform.TransformPoint(v.position),v));}
                if(Vector3.Cross(poly[1].world-poly[0].world,poly[2].world-poly[0].world).normalized.y>-.99f) throw new Exception("Face inferior com orientacao inesperada em "+pb.name);
                var parts=new List<List<Point>>{poly};
                parts=Split(parts,0,-10.2f,pb.transform); parts=Split(parts,0,-4.2f,pb.transform);
                parts=Split(parts,2,4,pb.transform); parts=Split(parts,2,10,pb.transform);
                float sum=0;
                foreach(var part in parts)
                {
                    float a=Area(part); if(a<1e-9f) continue; sum+=a;
                    Vector3 center=Vector3.zero; foreach(var point in part) center+=point.world; center/=part.Count;
                    bool inside=Inside(center);
                    int start=p.vertices.Count; foreach(var point in part) p.vertices.Add(new Vertex(point.vertex));
                    var triangles=new List<int>(); for(int k=1;k<part.Count-1;k++) {triangles.Add(start);triangles.Add(start+k);triangles.Add(start+k+1);}
                    var next=new Face(face); next.SetIndexes(triangles); next.manualUV=true; next.submeshIndex=inside?slot:face.submeshIndex; p.faces.Add(next);
                    if(inside) p.area+=a;
                }
                if(Mathf.Abs(sum-Area(poly))>.0001f) throw new Exception("Area da laje nao foi preservada em "+pb.name);
            }
        }
        return p;
    }
    static bool Inside(Vector3 p) {return p.x>=-10.2f-Eps && p.x<=-4.2f+Eps && p.z>=4-Eps && p.z<=10+Eps;}
    static bool IsCorrect(ProBuilderMesh pb,Material ceiling)
    {
        var mats=pb.GetComponent<MeshRenderer>().sharedMaterials; float area=0;
        foreach(var f in pb.faces) if(mats[f.submeshIndex]==ceiling)
        {
            foreach(int id in f.distinctIndexes) {var w=pb.transform.TransformPoint(pb.positions[id]); if(Mathf.Abs(w.y-3.5f)>.001f || !Inside(w)) return false;}
            var ix=f.indexes; for(int i=0;i<ix.Count;i+=3) {var a=pb.transform.TransformPoint(pb.positions[ix[i]]);var b=pb.transform.TransformPoint(pb.positions[ix[i+1]]);var c=pb.transform.TransformPoint(pb.positions[ix[i+2]]);var cross=Vector3.Cross(b-a,c-a);if(cross.normalized.y>-.99f)return false;area+=cross.magnitude*.5f;}
        }
        return area>.001f;
    }
    static float Area(List<Point> p) {float a=0;for(int i=1;i<p.Count-1;i++)a+=Vector3.Cross(p[i].world-p[0].world,p[i+1].world-p[0].world).magnitude*.5f;return a;}
    static List<List<Point>> Split(List<List<Point>> input,int axis,float cut,Transform t)
    {
        var output=new List<List<Point>>();
        foreach(var poly in input) {float lo=poly.Min(p=>p.world[axis]),hi=poly.Max(p=>p.world[axis]);if(lo>=cut-Eps || hi<=cut+Eps) {output.Add(poly);continue;}var a=Clip(poly,axis,cut,true,t);var b=Clip(poly,axis,cut,false,t);if(a.Count>=3)output.Add(a);if(b.Count>=3)output.Add(b);}
        return output;
    }
    static List<Point> Clip(List<Point> input,int axis,float cut,bool positive,Transform t)
    {
        var output=new List<Point>(); Point a=input[input.Count-1];float da=(positive?1:-1)*(a.world[axis]-cut);
        foreach(var b in input) {float db=(positive?1:-1)*(b.world[axis]-cut);if((da>=0)!=(db>=0)) {float ratio=da/(da-db);Vector3 w=Vector3.Lerp(a.world,b.world,ratio);w[axis]=cut;var v=Vertex.Mix(a.vertex,b.vertex,ratio);v.position=t.InverseTransformPoint(w);output.Add(new Point(w,v));}if(db>=0)output.Add(b);a=b;da=db;}
        return output;
    }
    static void Tell(string text) {EditorUtility.DisplayDialog("Teto da cozinha V42",text,"OK");}
}
#endif
