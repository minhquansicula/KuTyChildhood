using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Articulates copies of the authored kitchen. Never overwrites a source FBX.</summary>
public static class Scene2SourceKitchenArticulation
{
    public const string Output = "Tools/ExistingAssetMinigames/SourceArticulation";
    public const string Art = "Assets/_Project/Art/Scene2/SourceKitchenArticulation";
    public static readonly Vector3 LidNormal = new Vector3(-.28181923f,.94260713f,.17908021f);
    public sealed class Parts : IDisposable
    {
        public Transform source;
        public Transform fuelSource;
        public Mesh[] meshes;
        public string report;
        public void Dispose() { foreach(var mesh in meshes) if(mesh && !AssetDatabase.Contains(mesh)) UnityEngine.Object.DestroyImmediate(mesh); }
    }
    static Scene Home()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!=Scene2MotherHomeSetup.Home)
            throw new InvalidOperationException("Saved Home Edit Mode required. Never discard user edits.");
        return scene;
    }
    public static Transform Source(string name) => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name==name);
    static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
    static int Compare(Vector3Int a,Vector3Int b) => a.x!=b.x?a.x.CompareTo(b.x):a.y!=b.y?a.y.CompareTo(b.y):a.z.CompareTo(b.z);
    static (Vector3Int,Vector3Int) EdgeKey(Vector3Int a,Vector3Int b) => Compare(a,b)<0?(a,b):(b,a);
    struct Edge { public int a,b,count,balance; }
    static Dictionary<(Vector3Int,Vector3Int),Edge> Edges(Mesh mesh,Transform source)
    {
        var keys=mesh.vertices.Select(v=>Key(source.TransformPoint(v))).ToArray();
        var edges=new Dictionary<(Vector3Int,Vector3Int),Edge>(); var tri=mesh.triangles;
        void Add(int a,int b)
        {
            if(keys[a]==keys[b]) return;
            var key=EdgeKey(keys[a],keys[b]);
            int sign=Compare(keys[a],keys[b])<0?1:-1;
            if(edges.TryGetValue(key,out var e))
            {
                e.count++;e.balance+=sign;
                if(e.balance*sign>0) {e.a=a;e.b=b;}
                edges[key]=e;
            }
            else edges.Add(key,new Edge { a=a,b=b,count=1,balance=sign });
        }
        for(int i=0;i<tri.Length;i+=3) { Add(tri[i],tri[i+1]); Add(tri[i+1],tri[i+2]); Add(tri[i+2],tri[i]); }
        return edges;
    }
    public static Parts Build()
    {
        Home(); var source=Source("beplua_naucom"); var mesh=source.GetComponent<MeshFilter>().sharedMesh;
        if(mesh.vertexCount!=551517 || mesh.subMeshCount!=1 || AssetDatabase.GetAssetPath(mesh)!="Assets/_Project/Art/Scene2/Models/Kitchen/SM_RiceCookingStove.fbx")
            throw new InvalidOperationException("Original source changed; resurvey first.");
        var v=mesh.vertices; var tri=mesh.triangles;
        var world=v.Select(source.TransformPoint).ToArray();
        var bounds=new Bounds(world[0],Vector3.zero); foreach(var p in world) bounds.Encapsulate(p);
        if(Vector3.Distance(bounds.center,new Vector3(-6.0745f,.5869f,-6.4259f))>.001f)
            throw new InvalidOperationException("Source pose changed. Do not move user props to match a survey.");
        var d=world.Select(p=>Vector3.Dot(LidNormal,p)-1.6015695f).ToArray();
        bool Above(int a,int b,int c) => (d[a]+d[b]+d[c])/3f>-.018f;
        var upper=new List<int>();
        for(int i=0;i<tri.Length;i+=3) if(Above(tri[i],tri[i+1],tri[i+2])) { upper.Add(tri[i]);upper.Add(tri[i+1]);upper.Add(tri[i+2]); }
        var groups=Scene2ExistingAssetReview.ConnectedParts(v,tri,source.localToWorldMatrix);
        var fuel=groups.Skip(2).Take(3).Select(g=>new HashSet<int>(g)).ToArray();
        var result=new Parts {source=source,meshes=new Mesh[5]}; var report=new StringBuilder();
        try
        {
            var top=Clip(mesh,source,fuel,true);var bottom=Clip(mesh,source,fuel,false);
            var topGroups=Scene2ExistingAssetReview.ConnectedParts(top.vertices,top.triangles,source.localToWorldMatrix);
            for(int gi=0;gi<Math.Min(topGroups.Length,8);gi++)
            {
                var set=new HashSet<int>(topGroups[gi]);
                var inspection=Scene2OriginalGeometry.Triangles(top,(a,b,c)=>set.Contains(a));
                report.AppendLine("Upper connected group "+gi+" vertices="+inspection.vertexCount+" bounds="+inspection.bounds);
                Scene2OriginalGeometry.Render(source,inspection,"surgery-upper-"+gi+".png");
                UnityEngine.Object.DestroyImmediate(inspection);
            }
            var lidSet=new HashSet<int>(topGroups[0]);
            result.meshes[1]=Scene2OriginalGeometry.Triangles(top,(a,b,c)=>lidSet.Contains(a));
            var other=Scene2OriginalGeometry.Triangles(top,(a,b,c)=>!lidSet.Contains(a));
            result.meshes[0]=new Mesh {indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            result.meshes[0].CombineMeshes(new[]{new CombineInstance {mesh=bottom,transform=Matrix4x4.identity},new CombineInstance {mesh=other,transform=Matrix4x4.identity}},true,false);
            UnityEngine.Object.DestroyImmediate(top);UnityEngine.Object.DestroyImmediate(bottom);UnityEngine.Object.DestroyImmediate(other);
            for(int label=0;label<5;label++)
            {
                if(label>=2) {int f=label-2;result.meshes[label]=Scene2OriginalGeometry.Triangles(mesh,(a,b,c)=>fuel[f].Contains(a));}
                var part=result.meshes[label];
                part.name=new[]{"SourceStoveBody","SourceCookingLid","SourceFuel01","SourceFuel02","SourceFuel03"}[label];
                report.AppendLine(part.name+" retained/clipped source triangles="+part.triangles.Length/3);
                if(label<2) CloseSeams(part,source,null,report);
            }
            report.AppendLine("Source surfaces clipped exactly at the contact plane; intersections interpolate source position/normal/tangent/UV. Original FBX/materials untouched.");
            report.AppendLine("Repairs triangulate only contact seam loops using boundary positions/UVs. No replacement lid, pot, stove, or textures.");
            result.report=report.ToString(); return result;
        }
        catch { result.Dispose(); throw; }
    }
    struct Vertex
    {
        public Vector3 p,n; public Vector2 uv; public Vector4 tangent; public float d;
        public static Vertex Intersection(Vertex a,Vertex b)
        {
            // Identical shared edges must calculate the identical point in either winding.
            bool swap=a.p.x!=b.p.x?a.p.x>b.p.x:a.p.y!=b.p.y?a.p.y>b.p.y:a.p.z>b.p.z;
            if(swap) {var temp=a;a=b;b=temp;}
            double t=(double)a.d/((double)a.d-b.d);float f=(float)t;
            var p=new Vector3((float)(a.p.x+((double)b.p.x-a.p.x)*t),(float)(a.p.y+((double)b.p.y-a.p.y)*t),(float)(a.p.z+((double)b.p.z-a.p.z)*t));
            return new Vertex {p=p,n=Vector3.LerpUnclamped(a.n,b.n,f).normalized,uv=Vector2.LerpUnclamped(a.uv,b.uv,f),tangent=Vector4.LerpUnclamped(a.tangent,b.tangent,f),d=0};
        }
    }
    static Mesh Clip(Mesh mesh,Transform source,HashSet<int>[] fuel,bool above,Vector3? cutNormal=null,float plane=1.5835695f)
    {
        var p=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;var t=mesh.tangents;var tri=mesh.triangles;
        var normal=cutNormal??LidNormal;
        var v=Enumerable.Range(0,p.Length).Select(i=>new Vertex {p=p[i],n=n[i],uv=uv[i],tangent=t.Length==p.Length?t[i]:Vector4.zero,d=Vector3.Dot(normal,source.TransformPoint(p[i]))-plane}).ToArray();
        var output=new List<Vertex>();var indices=new List<int>();
        var unique=new Dictionary<(Vector3,Vector3,Vector2,Vector4),int>();
        int Add(Vertex item)
        {
            var key=(item.p,item.n,item.uv,item.tangent);
            if(unique.TryGetValue(key,out int index)) return index;
            index=output.Count;output.Add(item);unique.Add(key,index);return index;
        }
        for(int i=0;i<tri.Length;i+=3)
        {
            if(fuel.Any(f=>f.Contains(tri[i]))) continue;
            var polygon=new List<Vertex>();
            for(int j=0;j<3;j++)
            {
                var a=v[tri[i+j]];var b=v[tri[i+(j+1)%3]];
                bool ain=above?a.d>=0:a.d<=0,bin=above?b.d>=0:b.d<=0;
                if(ain) polygon.Add(a);
                if(ain!=bin) polygon.Add(Vertex.Intersection(a,b));
            }
            for(int j=1;j+1<polygon.Count;j++)
            {
                indices.Add(Add(polygon[0]));indices.Add(Add(polygon[j]));indices.Add(Add(polygon[j+1]));
            }
        }
        var result=new Mesh {indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        result.vertices=output.Select(x=>x.p).ToArray();result.normals=output.Select(x=>x.n).ToArray();result.uv=output.Select(x=>x.uv).ToArray();result.tangents=output.Select(x=>x.tangent).ToArray();result.triangles=indices.ToArray();result.RecalculateBounds();return result;
    }
    static void CloseSeams(Mesh mesh,Transform source,Dictionary<(Vector3Int,Vector3Int),Edge> original,StringBuilder report)
    {
        var vertices=mesh.vertices; var keys=vertices.Select(v=>Key(source.TransformPoint(v))).ToArray();
        var edges=Edges(mesh,source);
        // The scanned source itself has small open contacts. Include their boundary
        // continuation when a cut meets one; otherwise a plane-only chain cannot close.
        var seam=edges.Where(e=>e.Value.balance!=0).Select(e=>e.Value).ToList();
        var outgoing=new Dictionary<Vector3Int,Edge>();
        foreach(var e in seam)
        {
            if(outgoing.ContainsKey(keys[e.a])) throw new InvalidOperationException("Branched seam: requires topology review, not a fan cap.");
            outgoing.Add(keys[e.a],e);
        }
        var triangles=mesh.triangles.ToList(); int loops=0,added=0;
        var positions=vertices.ToList();var normals=mesh.normals.ToList();var uv=mesh.uv.ToList();var tangents=mesh.tangents.ToList();
        // Reuse a small original metal texel swatch rather than stretching an atlas
        // across disconnected UV islands (which can otherwise pull baked fire onto metal).
        int metal=Enumerable.Range(0,vertices.Length).OrderByDescending(i=>source.TransformPoint(vertices[i]).y).First();
        Vector2 repairUv=uv[metal];
        while(outgoing.Count>0)
        {
            var first=outgoing.First(); var current=first.Key; var loop=new List<int>();
            do
            {
                if(!outgoing.TryGetValue(current,out var edge))
                {
                    var incoming=outgoing.Where(e=>keys[e.Value.b]==current).ToArray();
                    var nearest=outgoing.Keys.OrderBy(k=>((Vector3)(k-current)).sqrMagnitude).FirstOrDefault();
                    throw new InvalidOperationException("Open seam chain on "+mesh.name+" at "+current+" after "+loop.Count+" vertices; incoming="+incoming.Length+" nearest="+nearest+" first="+first.Key);
                }
                outgoing.Remove(current);loop.Add(edge.a);current=keys[edge.b];
            } while(current!=first.Key);
            // Opposite to the retained surface boundary: cap normals face outward.
            loop.Reverse();
            var caps=Triangulate(loop,vertices,source);
            for(int ci=0;ci<caps.Count;ci+=3)
            {
                var normal=Vector3.Cross(vertices[caps[ci+1]]-vertices[caps[ci]],vertices[caps[ci+2]]-vertices[caps[ci]]).normalized;
                for(int j=0;j<3;j++)
                {
                    triangles.Add(positions.Count);positions.Add(vertices[caps[ci+j]]);normals.Add(normal);uv.Add(repairUv);tangents.Add(new Vector4(1,0,0,1));
                }
            }
            added+=caps.Count/3;loops++;
        }
        mesh.vertices=positions.ToArray();mesh.normals=normals.ToArray();mesh.uv=uv.ToArray();mesh.tangents=tangents.ToArray();
        mesh.triangles=triangles.ToArray();mesh.RecalculateBounds();
        int remaining=Edges(mesh,source).Count(e=>e.Value.balance!=0);
        if(remaining!=0) throw new InvalidOperationException("Repair left newly exposed source edges: "+remaining);
        report.AppendLine("  repaired loops="+loops+" seam edges="+seam.Count+" added boundary-only triangles="+added+" remaining new open edges="+remaining);
    }
    public static Parts BuildClosed()
    {
        Home();var source=Source("StovePotVisual");var mesh=source.GetComponent<MeshFilter>().sharedMesh;
        if(mesh.vertexCount!=31211) throw new InvalidOperationException("Closed original source changed; review first.");
        var result=new Parts {source=source,fuelSource=Source("beplua_naucom"),meshes=new Mesh[5]};var report=new StringBuilder();
        try
        {
            var top=Clip(mesh,source,new HashSet<int>[0],true,Vector3.up,1.000f);
            var bottom=Clip(mesh,source,new HashSet<int>[0],false,Vector3.up,1.000f);
            var groups=Scene2ExistingAssetReview.ConnectedParts(top.vertices,top.triangles,source.localToWorldMatrix);
            var lidSet=new HashSet<int>(groups[0]);
            result.meshes[1]=Scene2OriginalGeometry.Triangles(top,(a,b,c)=>lidSet.Contains(a));
            var other=Scene2OriginalGeometry.Triangles(top,(a,b,c)=>!lidSet.Contains(a));
            result.meshes[0]=new Mesh {indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            result.meshes[0].CombineMeshes(new[]{new CombineInstance {mesh=bottom},new CombineInstance {mesh=other}},true,false);
            UnityEngine.Object.DestroyImmediate(top);UnityEngine.Object.DestroyImmediate(bottom);UnityEngine.Object.DestroyImmediate(other);
            for(int i=0;i<2;i++)
            {
                result.meshes[i].name=i==0?"OriginalClosedStoveBody":"OriginalClosedStoveLid";
                report.AppendLine(result.meshes[i].name+" retained/clipped triangles="+result.meshes[i].triangles.Length/3);
                CloseSeams(result.meshes[i],source,null,report);
            }
            var fuelMesh=result.fuelSource.GetComponent<MeshFilter>().sharedMesh;
            var fuels=Scene2ExistingAssetReview.ConnectedParts(fuelMesh.vertices,fuelMesh.triangles,result.fuelSource.localToWorldMatrix);
            for(int i=0;i<3;i++)
            {
                var set=new HashSet<int>(fuels[i+2]);result.meshes[i+2]=Scene2OriginalGeometry.Triangles(fuelMesh,(a,b,c)=>set.Contains(a));
                result.meshes[i+2].name="OriginalConnectedFuel"+(i+1);
            }
            report.AppendLine("Original closed StovePot source selected instead of tilted RiceCookingStove lid. Fuel uses exact connected source triangles.");
            report.AppendLine("Surface intersections interpolate original attributes. Only contact-boundary repairs are added; metal repair UV samples original texture. No source FBX/material overwrite.");
            result.report=report.ToString();return result;
        }
        catch {result.Dispose();throw;}
    }
    static List<int> Triangulate(List<int> loop,Vector3[] vertices,Transform source)
    {
        var points=loop.Select(i=>source.TransformPoint(vertices[i])).ToArray();
        Vector3 normal=Vector3.zero;
        for(int i=0;i<points.Length;i++) normal+=Vector3.Cross(points[i]-points[0],points[(i+1)%points.Length]-points[0]);
        if(normal.sqrMagnitude<1e-12f) throw new InvalidOperationException("Degenerate seam loop.");
        var u=Vector3.Cross(normal.normalized,Mathf.Abs(normal.normalized.y)<.9f?Vector3.up:Vector3.right).normalized;
        var w=Vector3.Cross(normal.normalized,u);
        var p=points.Select(v=>new Vector2(Vector3.Dot(v-points[0],u),Vector3.Dot(v-points[0],w))).ToArray();
        float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        float area=0;for(int i=0;i<p.Length;i++) area+=Cross(p[i],p[(i+1)%p.Length]);
        float sign=Mathf.Sign(area);var active=Enumerable.Range(0,loop.Count).ToList();var result=new List<int>();
        while(active.Count>3)
        {
            bool cut=false;
            for(int j=0;j<active.Count;j++)
            {
                int a=active[(j+active.Count-1)%active.Count],b=active[j],c=active[(j+1)%active.Count];
                if(Cross(p[b]-p[a],p[c]-p[b])*sign<1e-12f) continue;
                bool contains=active.Any(k=>k!=a&&k!=b&&k!=c && Cross(p[b]-p[a],p[k]-p[a])*sign>1e-11f && Cross(p[c]-p[b],p[k]-p[b])*sign>1e-11f && Cross(p[a]-p[c],p[k]-p[c])*sign>1e-11f);
                if(contains) continue;
                result.Add(loop[a]);result.Add(loop[b]);result.Add(loop[c]);active.RemoveAt(j);cut=true;break;
            }
            if(!cut) throw new InvalidOperationException("Non-simple seam projection. Manual source topology repair required.");
        }
        if(active.Count==3) {result.Add(loop[active[0]]);result.Add(loop[active[1]]);result.Add(loop[active[2]]);}
        return result;
    }
    [MenuItem("KuTy/Existing assets/Review repaired source kitchen articulation")]
    public static void Review()
    {
        Directory.CreateDirectory(Output);
        using(var parts=BuildClosed())
        {
            File.WriteAllText(Output+"/topology-checks.txt",parts.report);
            foreach(var side in new[]{("front",new Vector3(1.3f,1.1f,1.5f)),("rear",new Vector3(-1.3f,.7f,-1.5f)),("under",new Vector3(1,-1,1))})
                foreach(int i in new[]{0,1})
                {
                    string temp="repaired-"+i+"-"+side.Item1+".png";
                    Scene2OriginalGeometry.Render(parts.source,parts.meshes[i],temp,side.Item2);
                    File.Copy(Scene2ExistingAssetReview.Output+"/"+temp,Output+"/"+temp,true);
                }
        }
        Debug.Log("[SourceKitchen] Repaired source copies reviewed; scene and FBX untouched.");
    }
}
