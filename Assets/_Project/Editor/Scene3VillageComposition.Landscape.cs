using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class Scene3VillageComposition
{
    private sealed class Geometry
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        public readonly List<int> first=new List<int>(), second=new List<int>();
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,bool foliage=false,bool doubleSided=false)
        {
            var list=foliage?second:first; var start=vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            list.Add(start);list.Add(start+1);list.Add(start+2);
            if(doubleSided) {list.Add(start+2);list.Add(start+1);list.Add(start);}
        }
        public void Cylinder(Vector3 from,Vector3 to,float radius,bool foliage=false)
        {
            var axis=(to-from).normalized;
            var tangent=Vector3.Cross(axis,Mathf.Abs(axis.y)<.9f?Vector3.up:Vector3.right).normalized;
            var bitangent=Vector3.Cross(axis,tangent);
            for(var i=0;i<7;i++)
            {
                var angle=i*Mathf.PI*2/7; var next=(i+1)*Mathf.PI*2/7;
                var side=(tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle))*radius;
                var side2=(tangent*Mathf.Cos(next)+bitangent*Mathf.Sin(next))*radius;
                Triangle(from+side,from+side2,to+side2,foliage); Triangle(from+side,to+side2,to+side,foliage);
            }
        }
        public void Lobe(Vector3 center,Vector3 size,bool foliage=true)
        {
            var points=new[] {center+Vector3.up*size.y,center-Vector3.up*size.y,center+Vector3.right*size.x,
                center-Vector3.right*size.x,center+Vector3.forward*size.z,center-Vector3.forward*size.z};
            foreach(var triangle in new[] {new[]{0,2,4},new[]{0,4,3},new[]{0,3,5},new[]{0,5,2},
                new[]{1,4,2},new[]{1,3,4},new[]{1,5,3},new[]{1,2,5}})
                Triangle(points[triangle[0]],points[triangle[1]],points[triangle[2]],foliage);
        }
        public Mesh Save(string name)
        {
            EnsureFolder(DetailModels);
            var mesh=new Mesh{name=name}; mesh.SetVertices(vertices); mesh.subMeshCount=second.Count>0?2:1;
            mesh.SetTriangles(first,0); if(second.Count>0) mesh.SetTriangles(second,1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(DetailModels+"/"+name+".asset");
            if(existing) {EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing;}
            else AssetDatabase.CreateAsset(mesh,DetailModels+"/"+name+".asset");
            return mesh;
        }
    }
    private static GameObject MeshProp(Transform parent,string name,Vector3 position,Mesh mesh,params Material[] materials)
    {
        var obj=Group(name,parent);obj.transform.position=position;
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.AddComponent<MeshRenderer>(); renderer.sharedMaterials=materials;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        return obj;
    }
    private static Mesh BambooMesh()
    {
        var path=DetailModels+"/BuiTre_VietNam.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(existing) return existing;
        var geometry=new Geometry();var random=new System.Random(1526);
        for(var stem=0;stem<7;stem++)
        {
            var angle=stem*Mathf.PI*2/7;
            var root=new Vector3(Mathf.Cos(angle)*.58f,0,Mathf.Sin(angle)*.58f);
            var height=4.8f+(float)random.NextDouble()*1.6f;
            var top=root+new Vector3(Mathf.Cos(angle)*.5f,height,Mathf.Sin(angle)*.5f);
            geometry.Cylinder(root,top,.045f);
            for(var branch=0;branch<4;branch++)
            {
                var branchAngle=angle+branch*1.75f;
                var origin=Vector3.Lerp(root,top,.52f+branch*.125f);
                var direction=new Vector3(Mathf.Cos(branchAngle),.18f,Mathf.Sin(branchAngle));
                var tip=origin+direction*1.65f;
                geometry.Cylinder(origin,tip,.018f);
                for(var leaf=0;leaf<7;leaf++)
                {
                    var start=Vector3.Lerp(origin,tip,.18f+leaf*.115f);
                    var side=Vector3.Cross(direction,Vector3.up).normalized*(leaf%2==0?1:-1);
                    var end=start+direction*.48f+side*.55f+Vector3.down*(.08f+leaf*.014f);
                    var middle=(start+end)*.5f+Vector3.up*.075f;
                    geometry.Triangle(start,middle+side*.07f,end,true,true);
                    geometry.Triangle(start,end,middle-side*.07f,true,true);
                }
            }
        }
        return geometry.Save("BuiTre_VietNam");
    }
    private static Mesh BananaMesh()
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(DetailModels+"/BuiChuoi.asset");if(existing)return existing;
        var geometry=new Geometry();geometry.Cylinder(Vector3.zero,new Vector3(.12f,2.5f,0),.16f);
        for(var leaf=0;leaf<9;leaf++)
        {
            var angle=leaf*Mathf.PI*2/9;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            var side=Vector3.Cross(direction,Vector3.up);var origin=new Vector3(.12f,2.1f+(leaf%3)*.2f,0);
            for(var segment=0;segment<5;segment++)
            {
                var t0=segment/5f;var t1=(segment+1)/5f;
                var p0=origin+direction*t0*2.6f+Vector3.up*(Mathf.Sin(t0*Mathf.PI)*.7f-t0*.45f);
                var p1=origin+direction*t1*2.6f+Vector3.up*(Mathf.Sin(t1*Mathf.PI)*.7f-t1*.45f);
                var width0=Mathf.Sin(t0*Mathf.PI)*.34f;var width1=Mathf.Sin(t1*Mathf.PI)*.34f;
                geometry.Triangle(p0-side*width0,p1-side*width1,p1+side*width1,true,true);
                geometry.Triangle(p0-side*width0,p1+side*width1,p0+side*width0,true,true);
            }
        }
        return geometry.Save("BuiChuoi");
    }
    private static void MakeTree(Transform parent,Vector3 position,float height,Material wood,Material dark,Material light,string name)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(DetailModels+"/CayOi_Village.asset");
        if(!mesh)
        {
            var geometry=new Geometry();geometry.Cylinder(Vector3.zero,new Vector3(0,2.5f,0),.12f);
            geometry.Cylinder(new Vector3(0,1.5f,0),new Vector3(-1.1f,2.8f,.25f),.075f);
            geometry.Cylinder(new Vector3(0,1.7f,0),new Vector3(1.15f,2.8f,-.3f),.075f);
            geometry.Lobe(new Vector3(0,3.0f,0),new Vector3(1.65f,1.25f,1.7f));
            geometry.Lobe(new Vector3(-1,2.75f,.15f),new Vector3(1.05f,.9f,1.05f));
            geometry.Lobe(new Vector3(1.1f,2.8f,-.25f),new Vector3(1.05f,1f,1.1f));
            mesh=geometry.Save("CayOi_Village");
        }
        var tree=MeshProp(parent,name,position,mesh,wood,dark);tree.transform.localScale=Vector3.one*(height/4.25f);
        var collider=tree.AddComponent<CapsuleCollider>();collider.radius=.18f;collider.height=2.5f;collider.center=Vector3.up*1.25f;
    }
    private static void MakeGardens(Transform parent,Material earth,Material wood,Material dark,Material light)
    {
        var garden=Group("VuonRauCanhNgo",parent);
        Block(garden.transform,"NenVuon",new Vector3(-12.4f,.015f,32.4f),new Vector3(9.3f,.09f,5.7f),earth);
        var geometry=new Geometry();
        for(var row=0;row<4;row++)
        {
            var x=-15.5f+row*2;
            Block(garden.transform,"LuongRau",new Vector3(x,.09f,32.4f),new Vector3(1.3f,.16f,4.8f),earth,false);
            for(var plant=0;plant<8;plant++)
            {
                var position=new Vector3(x,.28f,30.5f+plant*.53f);
                geometry.Lobe(position,new Vector3(.32f,.18f,.32f));
                geometry.Lobe(position+new Vector3(.1f,.14f,0),new Vector3(.18f,.16f,.18f));
            }
        }
        // One shared mesh for all vegetables, rather than 64 separate crop objects.
        var vegetableMesh=geometry.Save("RauTrongVuon");
        MeshProp(garden.transform,"RauXanh",Vector3.zero,vegetableMesh,wood,light);
        var banana=BananaMesh();
        foreach(var position in new[] {new Vector3(-9,0,35),new Vector3(10,0,32),new Vector3(-30,0,57.3f)})
            MeshProp(parent,"BuiChuoiBenVuon",position,banana,wood,light);
        MakeTree(parent,new Vector3(11.5f,0,28.5f),3.8f,wood,dark,light,"CayAnQuaCanhTiem");
        MakeTree(parent,new Vector3(-28,0,22.5f),3.5f,wood,dark,light,"CayAnQua_VuonSau");
    }
    private static void MakeBoundary(Transform parent,Material wood,Material dark,Material light,Material grass,Material earth)
    {
        EnsureFolder(NewMaterials);
        var stalks=AssetDatabase.LoadAssetAtPath<Material>(NewMaterials+"/ThanTre_Xanh.mat");
        if(!stalks)
        {
            stalks=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="ThanTre_Xanh"};
            stalks.SetColor("_BaseColor",new Color(.29f,.37f,.09f));stalks.SetFloat("_Smoothness",.15f);
            AssetDatabase.CreateAsset(stalks,NewMaterials+"/ThanTre_Xanh.mat");
        }
        var mesh=BambooMesh();var random=new System.Random(6269);
        foreach(var side in new[]{-1,1})
        for(var z=-8;z<=82;z+=9)
        {
            var position=new Vector3(side*42.1f,0,z);
            var bamboo=MeshProp(parent,"BuiTre_BienLang",position,mesh,stalks,side<0?dark:light);
            bamboo.transform.rotation=Quaternion.Euler(0,random.Next(0,360),0);
            bamboo.transform.localScale=Vector3.one*(.85f+(float)random.NextDouble()*.25f);
        }
        for(var x=-36;x<=36;x+=12)
        {
            MeshProp(parent,"LuyTre_CuoiLang",new Vector3(x,0,84),mesh,stalks,dark);
            if(Mathf.Abs(x)>4)MeshProp(parent,"LuyTre_DauLang",new Vector3(x,0,-12.5f),mesh,stalks,dark);
        }
        var hill=AssetDatabase.LoadAssetAtPath<Mesh>(DetailModels+"/GoDat_BienLang.asset");
        if(!hill)
        {
            var geometry=new Geometry();var top=new Vector3(0,1,0);
            var corners=new[]{new Vector3(-1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,-1)};
            for(var index=0;index<4;index++)geometry.Triangle(top,corners[index],corners[(index+1)%4]);
            hill=geometry.Save("GoDat_BienLang");
        }
        foreach(var side in new[]{-1,1})
        for(var z=-10;z<=84;z+=10)
        {
            var mound=MeshProp(parent,"GoCoSauLuyTre",new Vector3(side*44,0,z),hill,grass);
            mound.transform.localScale=new Vector3(4,1.4f+(float)random.NextDouble()*.6f,7);
        }
        for(var x=-40;x<=40;x+=10)
        {
            var mound=MeshProp(parent,"GoDatCuoiLang",new Vector3(x,0,87),hill,grass);
            mound.transform.localScale=new Vector3(7,1.3f,4);
            if(Mathf.Abs(x)>4)
            {
                var front=MeshProp(parent,"GoDatDauLang",new Vector3(x,0,-14),hill,grass);
                front.transform.localScale=new Vector3(7,1.2f,3);
            }
        }
        InvisibleBoundary(parent,"RanhGioi_Tay",new Vector3(-43,2.5f,36.5f),new Vector3(1,5,99));
        InvisibleBoundary(parent,"RanhGioi_Dong",new Vector3(43,2.5f,36.5f),new Vector3(1,5,99));
        InvisibleBoundary(parent,"RanhGioi_Nam",new Vector3(0,2.5f,-12),new Vector3(87,5,1));
        InvisibleBoundary(parent,"RanhGioi_Bac",new Vector3(0,2.5f,85),new Vector3(87,5,1));
        Block(parent,"CotCongLang_Trai",new Vector3(-2.7f,1.6f,-10.8f),new Vector3(.22f,3.2f,.22f),wood);
        Block(parent,"CotCongLang_Phai",new Vector3(2.7f,1.6f,-10.8f),new Vector3(.22f,3.2f,.22f),wood);
        Block(parent,"XaCongLang",new Vector3(0,3.18f,-10.8f),new Vector3(5.65f,.18f,.25f),wood,false);
    }
    private static void InvisibleBoundary(Transform parent,string name,Vector3 center,Vector3 size)
    {
        var obj=Group(name,parent);obj.transform.position=center;obj.AddComponent<BoxCollider>().size=size;
    }
}
