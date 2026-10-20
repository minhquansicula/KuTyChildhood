"""Extract separate Tencent marbles, bake coloured low-poly game variants."""
import bpy, json, hashlib
from pathlib import Path
from mathutils import Vector

project=Path(__file__).resolve().parents[2]
source=project.parent/'BLENDER/VienBi'
out=project/'Tools/Scene3/VienBi'
staging=out/'Staging';staging.mkdir(parents=True,exist_ok=True)
objfile=source/'1ca9a2dd15e97dae21ef7955ada009d2.obj'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=str(objfile),forward_axis='NEGATIVE_Z',up_axis='Y')
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
parts=[o for o in bpy.context.scene.objects if o.type=='MESH']
round_parts=[]
for part in parts:
    d=part.dimensions
    if min(d)/max(d)>.83 and len(part.data.polygons)>50000:
        round_parts.append(part)
    part.hide_render=True
round_parts.sort(key=lambda p:p.location.x)
assert len(round_parts)==4,[(p.name,list(p.dimensions)) for p in round_parts]
base=bpy.data.images.load(str(source/'texture_pbr_20250901.png'),check_existing=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
scene.render.bake.use_selected_to_active=True;scene.render.bake.use_cage=False
scene.render.bake.cage_extrusion=.006;scene.render.bake.max_ray_distance=.035;scene.render.bake.margin=8

variants=[('Player',0,.63),('Target0',1,.50),('Target1',2,.31),('Target2',3,.43),('Target3',0,.19),('Target4',2,.78)]
records=[];lows=[]
for label,index,hue in variants:
    high=round_parts[index]
    high.hide_set(False);high.hide_render=False
    low=high.copy();low.data=high.data.copy();bpy.context.collection.objects.link(low)
    low.name='VienBi_'+label
    bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
    decimate=low.modifiers.new('Game mesh, 800 triangles','DECIMATE')
    decimate.ratio=min(1,800/len(low.data.polygons))
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    for poly in low.data.polygons:poly.use_smooth=True
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    triangles=sum(len(poly.vertices)-2 for poly in low.data.polygons)
    assert 600<=triangles<=820,(label,triangles)

    src=bpy.data.materials.new('Source_'+label);src.use_nodes=True
    nodes=src.node_tree.nodes;nodes.clear();links=src.node_tree.links
    source_tex=nodes.new('ShaderNodeTexImage');source_tex.image=base
    shift=nodes.new('ShaderNodeHueSaturation');shift.inputs['Hue'].default_value=hue
    shift.inputs['Saturation'].default_value=1.12
    emission=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial')
    links.new(source_tex.outputs['Color'],shift.inputs['Color'])
    links.new(shift.outputs['Color'],emission.inputs['Color'])
    links.new(emission.outputs[0],output.inputs['Surface'])
    high.data.materials.clear();high.data.materials.append(src)

    image=bpy.data.images.new('VienBi_'+label+'_Color',width=512,height=512,alpha=False)
    dst=bpy.data.materials.new('Game_'+label);dst.use_nodes=True
    dstnodes=dst.node_tree.nodes
    tex=dstnodes.new('ShaderNodeTexImage');tex.image=image;dstnodes.active=tex
    low.data.materials.clear();low.data.materials.append(dst)

    for other in parts:other.hide_render=other!=high
    low.hide_render=False
    bpy.ops.object.select_all(action='DESELECT');high.select_set(True);low.select_set(True)
    bpy.context.view_layer.objects.active=low
    bpy.ops.object.bake(type='EMIT')
    texture=staging/('VienBi_'+label+'_Color.png')
    image.filepath_raw=str(texture);image.file_format='PNG';image.save()
    dst.node_tree.links.new(tex.outputs['Color'],dstnodes['Principled BSDF'].inputs['Base Color'])

    local=[Vector(c) for c in low.bound_box]
    lo=Vector(tuple(min(v[k] for v in local) for k in range(3)))
    hi=Vector(tuple(max(v[k] for v in local) for k in range(3)))
    center=(lo+hi)/2;diameter=max(hi-lo)
    # Preserve proportions. Place the model centre on the Rigidbody origin.
    for v in low.data.vertices:v.co=(v.co-center)/diameter
    low.data.update();low.location=Vector((0,0,0));low.rotation_euler=(0,0,0);low.scale=(1,1,1)
    bpy.ops.object.select_all(action='DESELECT');low.select_set(True);bpy.context.view_layer.objects.active=low
    model=staging/('VienBi_'+label+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(model),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_anim=False,path_mode='STRIP',embed_textures=False)
    low.hide_render=True;high.hide_render=True;high.hide_set(True)
    lows.append(low)
    records.append({'name':label,'source_part':high.name,'source_triangles':len(high.data.polygons),'game_triangles':triangles,'source_diameter':diameter,'game_dimensions':list(low.dimensions),'hue_parameter':hue,'fbx':model.name,'texture':texture.name})
    print('BUILT',label,triangles,flush=True)

(out/'game_assets_report.json').write_text(json.dumps({'source_sha256':hashlib.sha256(objfile.read_bytes()).hexdigest(),'original_triangles':1500000,'separable_round_parts':len(round_parts),'variants':records},indent=2),encoding='utf-8')

for part in parts:part.hide_render=True
for index,low in enumerate(lows):
    low.hide_set(False);low.hide_render=False;low.location=Vector(((index%3)-1,(index//3)*1.25,0))
scene.render.bake.use_selected_to_active=False
scene.cycles.samples=16;scene.view_settings.view_transform='Standard'
scene.render.resolution_x=1000;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.8
ld=bpy.data.lights.new('Softbox','AREA');ld.energy=1600;ld.size=5
light=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(light);light.location=(2,-4,5)
camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=4.6
camera.location=(0,-5,4);camera.rotation_euler=(Vector((0,.6,0))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(out/'Six_Game_Marbles.png');bpy.ops.render.render(write_still=True)
print('DONE',flush=True)
