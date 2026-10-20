"""Native Blender material weathering, baked once; no runtime shader overhead.

Keep the input atlas, UV layout, normal map and mesh untouched. Colours are
muted, walls get restrained rain/ground stains, and metal loses its fresh gloss.
"""
import json


def apply_weathering(material, report):
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    shader = nodes.get('Principled BSDF')
    base = shader.inputs['Base Color'].links[0].from_socket
    rough = shader.inputs['Roughness'].links[0].from_socket
    metal = shader.inputs['Metallic'].links[0].from_socket

    def node(kind, label):
        result = nodes.new(kind)
        result.label = label
        return result

    def math(operation, a, b=None):
        result = node('ShaderNodeMath', operation)
        result.operation = operation
        for index, value in enumerate([a, b]):
            if value is None:
                continue
            if isinstance(value, (float, int)):
                result.inputs[index].default_value = value
            else:
                links.new(value, result.inputs[index])
        return result.outputs[0]

    def mix(color, target, factor, label, mode='MIX'):
        result = node('ShaderNodeMixRGB', label)
        result.blend_type = mode
        links.new(color, result.inputs[1])
        result.inputs[2].default_value = (*target, 1)
        if isinstance(factor, (float, int)):
            result.inputs[0].default_value = factor
        else:
            links.new(factor, result.inputs[0])
        return result.outputs[0]

    def remap(value, low, high, minimum, maximum, label):
        result = node('ShaderNodeMapRange', label)
        result.clamp = True
        result.interpolation_type = 'SMOOTHSTEP'
        links.new(value, result.inputs['Value'])
        for key, value in [('From Min', low), ('From Max', high),
                           ('To Min', minimum), ('To Max', maximum)]:
            result.inputs[key].default_value = value
        return result.outputs[0]

    coords = node('ShaderNodeTexCoord', 'Stable house coordinates')
    split = node('ShaderNodeSeparateXYZ', 'Height above foundation')
    links.new(coords.outputs['Object'], split.inputs[0])
    noise = node('ShaderNodeTexNoise', 'Broad irregular age patches')
    noise.inputs['Scale'].default_value = .85
    noise.inputs['Detail'].default_value = 2
    links.new(coords.outputs['Object'], noise.inputs['Vector'])

    hue = node('ShaderNodeHueSaturation', 'Muted village palette')
    hue.inputs['Saturation'].default_value = .72
    hue.inputs['Value'].default_value = .88
    links.new(base, hue.inputs['Color'])
    color = mix(hue.outputs['Color'], (.79, .75, .67),
                remap(noise.outputs['Fac'], .25, .75, .05, .30, 'Uneven fading'),
                'Soft brown-grey age variation', 'MULTIPLY')

    geometry = node('ShaderNodeNewGeometry', 'Wall orientation')
    normals = node('ShaderNodeSeparateXYZ', 'Vertical wall mask')
    links.new(geometry.outputs['Normal'], normals.inputs[0])
    vertical = math('SUBTRACT', 1, math('ABSOLUTE', normals.outputs['Z']))
    damp = remap(split.outputs['Z'], .05, 1.2, 1, 0, 'Damp near ground only')
    patch = remap(noise.outputs['Fac'], .25, .72, .35, 1, 'Broken ground stains')
    foot = math('MULTIPLY', math('MULTIPLY', damp, patch), vertical)
    color = mix(color, (.17, .135, .10), math('MULTIPLY', foot, .32), 'Weathered wall foot')
    moss = remap(noise.outputs['Fac'], .50, .74, 0, 1, 'Sparse restrained moss')
    color = mix(color, (.145, .175, .105),
                math('MULTIPLY', math('MULTIPLY', foot, moss), .18), 'Muted moss at base')

    stretch = node('ShaderNodeVectorMath', 'Vertical rain streak coordinates')
    stretch.operation = 'MULTIPLY'
    links.new(coords.outputs['Object'], stretch.inputs[0])
    stretch.inputs[1].default_value = (5, 5, .30)
    streak = node('ShaderNodeTexNoise', 'Fine rain streaks')
    streak.inputs['Scale'].default_value = 1
    streak.inputs['Detail'].default_value = 2
    links.new(stretch.outputs['Vector'], streak.inputs['Vector'])
    rain = remap(streak.outputs['Fac'], .55, .73, 0, .14, 'Subtle runoff')
    color = mix(color, (.23, .19, .14), math('MULTIPLY', rain, vertical), 'Rain marks on walls')

    rust = math('MULTIPLY', metal,
                remap(noise.outputs['Fac'], .50, .73, 0, .26, 'Restrained rust patches'))
    color = mix(color, (.26, .095, .035), rust, 'Weathered corrugated metal')
    links.new(color, shader.inputs['Base Color'])
    links.new(math('MAXIMUM', rough, .72), shader.inputs['Roughness'])
    links.new(math('MULTIPLY', metal, .50), shader.inputs['Metallic'])
    settings = {'saturation': .72, 'brightness_multiplier': .88,
                'roughness_floor': .72, 'metallic_multiplier': .50,
                'ground_stain_height_m': 1.2,
                'method': 'Blender procedural material, baked to independent LOD PBR atlases; no geometry or source image changes'}
    report.write_text(json.dumps(settings, indent=2), encoding='utf-8')
