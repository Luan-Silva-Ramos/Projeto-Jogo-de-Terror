#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

public static class MansaoFrontGableMorphV30
{
    public class Job
    {
        public ProBuilderMesh source;
        public List<Vertex> vertices=new List<Vertex>();public List<Vector3> positions=new List<Vector3>();public List<Face> faces=new List<Face>();public Material[] materials;
    }
    private struct Point
    {
        public Vector3 p;public Vector2 uv;
        public Point(Vector3 p,Vector2 uv){this.p=p;this.uv=uv;}
    }
    // Fator do frontao: topo 9.24 -> 8.20, mantendo a base em 7.40.
    public static Vector3 Morph(Vector3 p)
    {
        float fx=1-Mathf.Clamp01((Mathf.Abs(p.x)-1.85f)/.30f);
        float fz=1-Mathf.Clamp01((p.z+1.5f)/3f);
        if(p.y>7.4f)p.y=7.4f+(p.y-7.4f)*(1-(1-.8f/1.84f)*fx*fz);
        return p;
    }
    private static readonly string[] Required={
        "Roof_WholeHouse_Joined_V05_Slate_V20","Fronton_Integrated_Roof_V08_Slate_V20",
        "Fronton_CurvedFrame_AndLegs_V08","Fronton_Plaster_Exterior_V09",
        "Roof_Junction_Closures_Slate_V21","Roof_WholeHouse_Joined_V05_Seams_V20"
    };
    public static List<Job> Prepare(Transform mansion)
    {
        var sources=new List<ProBuilderMesh>();
        foreach(string name in Required)
        {
            ProBuilderMesh found=null;foreach(ProBuilderMesh pb in mansion.GetComponentsInChildren<ProBuilderMesh>(true))if(pb.name==name && pb.gameObject.activeInHierarchy){found=pb;break;}
            if(found==null)throw new InvalidOperationException("Nao encontrei "+name+" ativo.");sources.Add(found);
        }
        // Linhas compartilhadas de corte evitam frestas onde os triangulos originais se encontram.
        var xCuts=new SortedSet<float>{-2.15f,-1.85f,0,1.85f,2.15f};
        var zCuts=new SortedSet<float>{-5.25f,-4.95f,-4.85f,-3.5f,-2.5f,-1.5f,-.5f,.5f,1.5f};
        foreach(ProBuilderMesh pb in sources)foreach(Vector3 v in pb.positions)
        {
            Vector3 p=pb.transform.TransformPoint(v);
            if(Mathf.Abs(p.x)<=2.15f && p.z>=-5.25f && p.z<=1.5f && p.y>7.4f)
            {xCuts.Add(Mathf.Round(p.x*10000)/10000);zCuts.Add(Mathf.Round(p.z*10000)/10000);}
        }
        var jobs=new List<Job>();
        foreach(ProBuilderMesh source in sources)
        {
            var job=new Job{source=source,materials=source.GetComponent<MeshRenderer>().sharedMaterials};
            IList<Vector2> uvs=source.textures;
            foreach(Face face in source.faces)
            {
                var ids=face.indexes;
                for(int i=0;i<ids.Count;i+=3)
                {
                    var polygon=new List<Point>();for(int k=0;k<3;k++){int id=ids[i+k];polygon.Add(new Point(source.transform.TransformPoint(source.positions[id]),uvs!=null && uvs.Count==source.vertexCount?uvs[id]:Vector2.zero));}
                    Bounds b=new Bounds(polygon[0].p,Vector3.zero);foreach(Point p in polygon)b.Encapsulate(p.p);
                    var polygons=new List<List<Point>>{polygon};
                    float minAbs=b.min.x<=0 && b.max.x>=0?0:Mathf.Min(Mathf.Abs(b.min.x),Mathf.Abs(b.max.x));
                    float maxAbs=Mathf.Max(Mathf.Abs(b.min.x),Mathf.Abs(b.max.x));
                    bool varyingX=maxAbs>1.85f && minAbs<2.15f;
                    bool varyingZ=b.max.z> -1.5f && b.min.z<1.5f;
                    if(b.max.y>7.4f && minAbs<2.15f && b.min.z<1.5f && (varyingX || varyingZ))
                    {
                        foreach(float x in xCuts)if(x>b.min.x+.00001f && x<b.max.x-.00001f)polygons=Split(polygons,0,x);
                        foreach(float z in zCuts)if(z>b.min.z+.00001f && z<b.max.z-.00001f)polygons=Split(polygons,2,z);
                    }
                    foreach(List<Point> poly in polygons)for(int k=1;k<poly.Count-1;k++)
                    {
                        Point[] triangle={poly[0],poly[k],poly[k+1]};Vector3 a=Morph(triangle[0].p),c=Morph(triangle[1].p),d=Morph(triangle[2].p);
                        if(Vector3.Cross(c-a,d-a).sqrMagnitude<.0000000001f)continue;
                        int start=job.vertices.Count;
                        foreach(Point p in triangle){Vector3 world=Morph(p.p);job.positions.Add(world);job.vertices.Add(new Vertex {position=world,uv0=p.uv});}
                        Face next=new Face(new[]{start,start+1,start+2});next.manualUV=true;next.smoothingGroup=face.smoothingGroup;next.submeshIndex=face.submeshIndex;job.faces.Add(next);
                    }
                }
            }
            if(job.faces.Count==0 || job.faces.Count>150000)throw new InvalidOperationException("A malha de teste excedeu o limite previsto em "+source.name+".");
            jobs.Add(job);
        }
        return jobs;
    }
    private static List<List<Point>> Split(List<List<Point>> input,int axis,float limit)
    {
        var output=new List<List<Point>>();foreach(List<Point> polygon in input)
        {
            float min=float.PositiveInfinity,max=float.NegativeInfinity;foreach(Point p in polygon){min=Mathf.Min(min,p.p[axis]);max=Mathf.Max(max,p.p[axis]);}
            if(min>=limit-.00001f || max<=limit+.00001f){output.Add(polygon);continue;}
            List<Point> a=Clip(polygon,axis,limit,true),b=Clip(polygon,axis,limit,false);if(a.Count>=3)output.Add(a);if(b.Count>=3)output.Add(b);
        }
        return output;
    }
    private static List<Point> Clip(List<Point> input,int axis,float limit,bool greater)
    {
        var output=new List<Point>();Point a=input[input.Count-1];float da=(greater?1:-1)*(a.p[axis]-limit);
        foreach(Point b in input){float db=(greater?1:-1)*(b.p[axis]-limit);if((da>=0)!=(db>=0)){float t=da/(da-db);output.Add(new Point(Vector3.Lerp(a.p,b.p,t),Vector2.Lerp(a.uv,b.uv,t)));}if(db>=0)output.Add(b);a=b;da=db;}return output;
    }
    public static void BuildAll(List<Job> jobs,Transform mansion)
    {
        Transform parent=mansion.Find("UpperFloor_V2/Exterior_Roofs");
        if(parent==null)throw new InvalidOperationException("Exterior_Roofs nao encontrado.");
        GameObject root=new GameObject("Central_Gable_Reduced_V30");SceneManager.MoveGameObjectToScene(root,parent.gameObject.scene);root.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(root,"Criar frontao reduzido");
        foreach(Job job in jobs)
        {
            for(int i=0;i<job.vertices.Count;i++){Vertex v=job.vertices[i];v.position=root.transform.InverseTransformPoint(job.positions[i]);job.vertices[i]=v;}
            var local=new List<Vector3>();foreach(Vertex v in job.vertices)local.Add(v.position);
            ProBuilderMesh pb=ProBuilderMesh.Create(job.vertices,job.faces,SharedVertex.GetSharedVerticesWithPositions(local),null,job.materials);
            SceneManager.MoveGameObjectToScene(pb.gameObject,parent.gameObject.scene);pb.transform.SetParent(root.transform,false);pb.transform.localPosition=Vector3.zero;pb.transform.localRotation=Quaternion.identity;pb.transform.localScale=Vector3.one;
            pb.name=job.source.name+"_Reduced_V30";pb.gameObject.layer=job.source.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(pb.gameObject,"Criar cobertura central ajustada");
            MeshRenderer old=job.source.GetComponent<MeshRenderer>(),renderer=pb.GetComponent<MeshRenderer>();renderer.shadowCastingMode=old.shadowCastingMode;renderer.receiveShadows=old.receiveShadows;
            MeshCollider original=job.source.GetComponent<MeshCollider>(),collider=pb.GetComponent<MeshCollider>();
            if(original!=null){if(collider==null)collider=Undo.AddComponent<MeshCollider>(pb.gameObject);collider.sharedMesh=pb.GetComponent<MeshFilter>().sharedMesh;collider.enabled=original.enabled;collider.sharedMaterial=original.sharedMaterial;}
            else if(collider!=null)Undo.DestroyObjectImmediate(collider);
            Undo.RecordObject(job.source.gameObject,"Preservar versao anterior da cobertura");job.source.gameObject.SetActive(false);
        }
    }
}
#endif
