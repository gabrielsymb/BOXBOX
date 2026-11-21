"""Model grocery fixtures with swappable surface materials and authored product groups."""
from pathlib import Path
from math import radians

import bpy

OUTPUT = Path(__file__).resolve().parents[2] / "BOXBOX/boxbox/Assets/Grocery/Interior/Fixtures"

PALETTE = {
    "ShelfEnamel": (0.12, .24, .22), "ColdSteel": (.56,.63,.65),
    "Trim": (.045,.062,.07), "WoodCrate": (.36,.22,.12),
    "Milk": (.82,.88,.9), "Teal": (.035,.31,.26), "Tomato": (.72,.1,.06),
    "Amber": (.86,.46,.08), "Leaf": (.18,.35,.06), "Paper": (.68,.49,.31),
    "GlassTint": (.36,.65,.72), "Label": (.86,.82,.7),
}


def material(name):
    existing = bpy.data.materials.get(name)
    if existing:
        return existing
    color = PALETTE[name]
    value = bpy.data.materials.new(name)
    value.use_nodes = True
    shader = value.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = .55
    return value


def block(name, center, size, finish, bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        profile = obj.modifiers.new("Folded edges", "BEVEL")
        profile.width = bevel
        profile.segments = 2
        bpy.ops.object.modifier_apply(modifier=profile.name)
    obj.data.materials.append(material(finish))
    return obj


def bottle(x, y, z, finish):
    for radius, height, lift, surface in [
        (.031,.115,.0575,finish),(.02,.035,.129,finish),
        (.016,.014,.151,"Label"),(.032,.042,.061,"Label"),
    ]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=radius, depth=height, location=(x,y,z+lift))
        bpy.context.object.data.materials.append(material(surface))


def export(name):
    for mat in bpy.data.materials:
        objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"
                   and obj.data.materials and obj.data.materials[0] == mat]
        if not objects:
            continue
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        if len(objects) > 1:
            bpy.ops.object.join()
        objects[0].name = mat.name
    bpy.ops.export_scene.gltf(filepath=str(OUTPUT / f"{name}.glb"), export_format="GLB")


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def gondola(drinks):
    reset()
    block("Kick plate",(0,.02,.055),(.79,.61,.11),"Trim")
    block("Perforated back", (0,.245,.48),(.76,.035,.8),"ShelfEnamel")
    for x in (-.368,.368):
        block("Rolled upright",(x,.17,.45),(.037,.07,.86),"ShelfEnamel")
    for level,z in enumerate((.16,.38,.60)):
        block("Shelf deck",(0,-.028,z),(.73,.53,.028),"ShelfEnamel")
        block("Price rail",(0,-.296,z-.012),(.72,.024,.04),"Label")
        for index,x in enumerate((-.27,-.135,0,.135,.268)):
            finish=("Tomato","Teal","Amber","Milk","Paper")[(index+level)%5]
            for y in (-.17,.015):
                if drinks:
                    bottle(x,y,z+.017,finish)
                else:
                    block("Carton",(x,y,z+.085),(.105,.13,.141),finish)
                    block("Package label",(x,y-.067,z+.08),(.07,.006,.061),"Label",.001)
    export("boxbox-drink-gondola" if drinks else "boxbox-market-gondola")


def cooler():
    reset()
    block("Raised base",(0,.24,.052),(1,.46,.104),"Trim")
    block("Rear housing",(0,.445,.45),(1,.067,.9),"ColdSteel")
    block("Canopy",(0,.245,.865),(1,.48,.075),"ColdSteel")
    block("Service kick plate",(0,.009,.102),(.98,.048,.11),"ColdSteel")
    for x in (-.476,0,.476):
        block("Door jamb",(x,.012,.46),(.035,.053,.72),"Trim")
    for x in (-.48,.48):
        block("Cabinet side",(x,.25,.472),(.041,.43,.75),"ColdSteel")
    for level,z in enumerate((.19,.385,.58)):
        block("Chilled shelf",(0,.235,z),(.91,.35,.022),"ColdSteel")
        for index,x in enumerate((-.39,-.26,-.12,.12,.26,.39)):
            bottle(x,.135,z+.011,("Milk","Teal","Amber")[(index+level)%3])
    for x in (-.235,.235):
        block("Door pane",(x,.003,.486),(.423,.012,.65),"GlassTint",.002)
    for x in (-.045,.046):
        block("Metal handle",(x,-.031,.423),(.017,.025,.21),"ColdSteel")
    export("boxbox-chiller")


def produce(finish):
    reset()
    block("Reinforced plinth",(0,0,.085),(.65,.63,.17),"ShelfEnamel")
    for i,y in enumerate((-.16,.145)):
        z=.245+i*.09
        block("Crate base",(0,y,z),(.58,.28,.066),"WoodCrate")
        for x in (-.28,.28):
            block("Crate side",(x,y,z+.066),(.028,.28,.135),"WoodCrate")
        for end in (-.131,.131):
            block("Slim crate rail",(0,y+end,z+.065),(.55,.022,.12),"WoodCrate")
        for row,dy in enumerate((-.065,.061)):
            for column,x in enumerate((-.192,-.065,.063,.189)):
                bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,
                    radius=.063 + .006 * ((column + row) % 3),
                    location=(x,y+dy,z+.094+.017*((row+column)%2)))
                piece = bpy.context.object
                piece.scale.z = 1.07 if finish == "Tomato" else .88
                piece.data.materials.append(material(finish))
    export(f"boxbox-produce-{finish.lower()}")


gondola(False)
gondola(True)
cooler()
for fruit in ("Tomato","Amber","Leaf"):
    produce(fruit)
