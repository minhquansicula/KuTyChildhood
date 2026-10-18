"""Game-only derivatives of local Maxtree models; never edit downloaded sources.

Normalize each clump to metres/root at zero. Decimate rigid woody parts, but thin
foliage by whole connected leaves to avoid collapsed/exploded leaf triangles.
"""
from pathlib import Path
import bpy, json, math, random, sys, numpy as np
from mathutils import Vector, Matrix

root=Path(__file__).resolve().parents[2]
work=root/'Tools/Scene3/Maxtree'
assets=root/'Assets/_Project/Art/Models/Scene3/MaxtreeOptimized'
assets.mkdir(parents=True,exist_ok=True)
stage=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'grass'
specs=[('Cyperus_eragrostis',0,'CoLac',.43,650),('Digitaria_sanguinalis',0,'CoManTrau',.23,1100),('Lolium_perenne',0,'CoThap',.16,700),('Setaria_viridis_beauv',0,'CoDuoiChon',.38,650)]
if stage in ('trees','debug'):specs=[('Alnus_cremastogyne',0,'CayTanRong',6.4,36000),('Alnus_cremastogyne',2,'CayVuon',4.8,26000)]
report=[]

def triangles(obj):obj.data.calc_loop_triangles();return len(obj.data.loop_triangles)
def trim_connected(obj,budget,seed,keep_main=False,leaf_scale=1):
    # Keep entire connected components distributed throughout the canopy.
    mesh=obj.data;n=len(mesh.vertices);parent=np.arange(n,dtype=np.int32)
    def find(x):
        while parent[x]!=x:parent[x]=parent[parent[x]];x=int(parent[x])
        return x
    for e in mesh.edges:
        a,b=map(int,e.vertices);a=find(a);b=find(b)
        if a!=b:parent[b]=a
    grouped={}
    for p in mesh.polygons:grouped.setdefault(find(int(p.vertices[0])),[]).append(p.index)
    groups=list(grouped.values());random.Random(seed).shuffle(groups)
    if keep_main:
        # Keep the connected trunk/core before thinning detached fine twigs.
        groups.sort(key=len,reverse=True)
    desired=max(.001,budget/max(1,triangles(obj)))
    # Area correction keeps distant foliage from becoming nearly transparent.
    count=max(1,round(len(groups)*desired));keep=set(i for g in groups[:count] for i in g)
    if leaf_scale>1:
        # Compensate canopy coverage when whole leaves are thinned. Enlarge each
        # retained leaf around its own centroid, never scale the tree or trunk.
        # Farther LODs use larger leaf clusters to retain a readable silhouette.
        positions=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',positions);positions=positions.reshape(-1,3)
        for group in groups[:count]:
            ids=list({int(v) for face in group for v in mesh.polygons[face].vertices})
            points=positions[ids]
            if float(np.max(points.max(axis=0)-points.min(axis=0)))>.65:continue
            center=points.mean(axis=0);positions[ids]=center+(points-center)*leaf_scale
        mesh.vertices.foreach_set('co',positions.ravel())
    import bmesh
    bm=bmesh.new();bm.from_mesh(mesh);bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.index not in keep],context='FACES')
    bm.to_mesh(mesh);bm.free();mesh.update()
    return len(groups),count

for species,index,name,height,budget in specs:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(next(work.glob('*_'+species+'_01.fbx'))),use_image_search=False,use_anim=False)
    candidates=sorted([o for o in bpy.data.objects if o.type=='MESH'],key=lambda o:o.name)
    src=candidates[index];source_tris=triangles(src)
    # Resolve 3ds Max world transform, including the six sample layout offsets.
    src.data.transform(src.matrix_world);src.matrix_world=Matrix.Identity(4)
    co=np.empty(len(src.data.vertices)*3,dtype=np.float32);src.data.vertices.foreach_get('co',co);co=co.reshape(-1,3)
    lo=co.min(axis=0);hi=co.max(axis=0);base=co[co[:,2]<lo[2]+(hi[2]-lo[2])*.012].mean(axis=0);base[2]=lo[2]
    scale=height/(hi[2]-lo[2]);src.data.transform(Matrix.Scale(float(scale),4)@Matrix.Translation(-Vector(base)))
    for o in candidates:
        if o!=src:bpy.data.objects.remove(o,do_unlink=True)
    bpy.context.view_layer.objects.active=src;src.select_set(True)
    # Split by material to preserve bark silhouette separately from leaf thinning.
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='MATERIAL');bpy.ops.object.mode_set(mode='OBJECT')
    parts=[o for o in bpy.data.objects if o.type=='MESH']
    # FBX duplicates geometric vertices at UV/normal seams. Weld matching positions
    # before simplification so thousands of tiny disconnected triangles do not
    # become a hard lower bound for Decimate. UVs remain on independent face loops.
    import bmesh
    for part in parts:
        bm=bmesh.new();bm.from_mesh(part.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000005);bm.to_mesh(part.data);bm.free();part.data.update()
    part_counts=[triangles(o) for o in parts]
    print('PARTS',[(o.name,triangles(o),[(i,o.data.materials[i].name) for i in set(p.material_index for p in o.data.polygons)]) for o in parts],flush=True)
    if stage=='debug':break
    for o in parts:o.select_set(False)
    levels=[]
    for lod,ratio in enumerate((1,.30,.085)):
        out=[];stats=[]
        for part,original_count in zip(parts,part_counts):
            copy=part.copy();copy.data=part.data.copy();bpy.context.collection.objects.link(copy)
            copy.select_set(True);bpy.context.view_layer.objects.active=copy
            allowance=max(30,int(budget*ratio*original_count/source_tris))
            used={p.material_index for p in copy.data.polygons}
            foliage=any('Leaf' in copy.data.materials[i].name for i in used)
            if original_count>allowance:
                if stage=='trees':stats.append(trim_connected(copy,allowance*(1 if foliage else 3),4060+lod,keep_main=not foliage))
                else:
                    modifier=copy.modifiers.new('GameOnlyReduction','DECIMATE');modifier.ratio=min(1,allowance/original_count);modifier.use_collapse_triangulate=True
                    bpy.ops.object.modifier_apply(modifier=modifier.name)
            # Some exports weld many leaf clusters into a single component. Bound
            # those cases too; otherwise far LODs silently keep an entire canopy.
            remaining=triangles(copy)
            if remaining>allowance*1.15:
                modifier=copy.modifiers.new('BoundConnectedCanopy','DECIMATE');modifier.ratio=min(1,allowance/remaining);modifier.use_collapse_triangulate=True
                bpy.ops.object.modifier_apply(modifier=modifier.name)
            # One rooted wind mask retained across every LOD.
            color=copy.data.color_attributes.new(name='Col',type='FLOAT_COLOR',domain='POINT')
            positions=np.empty(len(copy.data.vertices)*3,dtype=np.float32);copy.data.vertices.foreach_get('co',positions);positions=positions.reshape(-1,3)
            positions[:,2]=np.clip(positions[:,2],0,height);copy.data.vertices.foreach_set('co',positions.ravel())
            colors=np.ones((len(positions),4),dtype=np.float32);colors[:,0]=positions[:,2]/height;colors[:,1]=1 if foliage else .2;colors[:,2]=.5
            color.data.foreach_set('color',colors.ravel())
            if not foliage:
                for face in copy.data.polygons:face.use_smooth=True
            out.append(copy);copy.select_set(False)
        for o in out:o.select_set(True)
        bpy.context.view_layer.objects.active=out[0];bpy.ops.object.join();obj=out[0];obj.name=f'{name}_LOD{lod}'
        final_budget=int(budget*ratio)
        if triangles(obj)>final_budget*1.04:
            modifier=obj.modifiers.new('FinalGameBudget','DECIMATE');modifier.ratio=final_budget/triangles(obj);modifier.use_collapse_triangulate=True
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        levels.append(obj);obj.select_set(False)
    for o in parts:bpy.data.objects.remove(o,do_unlink=True)
    for o in levels:o.select_set(True)
    bpy.context.view_layer.objects.active=levels[0]
    destination=assets/(name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(destination),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False,colors_type='LINEAR')
    entry=dict(species=species,source_variant=index+1,name=name,height=height,source_triangles=source_tris,lods=[dict(name=o.name,triangles=triangles(o),materials=[m.name for m in o.data.materials]) for o in levels])
    report.append(entry);(work/(stage+'_geometry.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('EXPORTED',name,[triangles(o) for o in levels],flush=True)
