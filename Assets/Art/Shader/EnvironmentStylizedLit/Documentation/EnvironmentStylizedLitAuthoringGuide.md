# EnvironmentStylizedLit Authoring Guide

EnvironmentStylizedLit is intended for stylized environment surfaces that need clear light bands, stable room readability, and modular authoring support.

## Use Cases

- Indoor rooms with inward-facing walls, floors, and ceilings.
- ProBuilder and modular environment pieces where authored UVs are acceptable or triplanar is justified.
- Clay, plaster, toy plastic, ceramic, and chalk-like stage materials.
- Stylized environments that still need ShadowCaster, DepthOnly, DepthNormalsOnly, Meta, baked GI, probes, SSAO transport, and additional lights.

## Not Recommended

- Photoreal physically-based targets.
- Transparent or heavy alpha-driven foliage workflows.
- Materials that depend on large emissive contribution as the main look.
- Cases where every surface needs triplanar and strong band noise at once.
- Debug or inspection views left enabled on checked-in materials.

## Validation Scenes

| Scene | Path | Primary Use | Key M14 Anchors |
| --- | --- | --- | --- |
| ESL_TestRoom | Assets/Scenes/EnvironmentStylizedLit/ESL_TestRoom.unity | indoor room, ProBuilder surfaces, stairs, columns, bevel, lightmap, SSAO review positions | M14IndoorViewpoint, M14ProBuilderFloor_Room, M14ProBuilderWall_Room, M14Stair_Room, M14LightmappedColumn_Room, M14SSAOOnViewpoint |
| ESL_LightingLab | Assets/Scenes/EnvironmentStylizedLit/ESL_LightingLab.unity | directional baseline, preset strip, point light review, spot light review | M14DirectionalOnlyViewpoint, M14ClayDioramaSphere_Lab, M14PointLightViewpoint, M14SpotLightViewpoint |

Regenerate checked-in validation assets with the explicit bootstrapper when anchors drift.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe" -projectPath . -batchmode -nographics -executeMethod BC.Rendering.EnvironmentStylizedLitValidationBootstrapper.BootstrapM14ValidationAssets -logFile unity_m14_bootstrap.log -quit
```

## Recommended Workflow

1. Pick the closest preset first.
2. Validate the material in ESL_TestRoom before touching point or spot light tuning.
3. Only enable triplanar after confirming that the issue is truly UV-related.
4. Re-check the same material in ESL_LightingLab under directional, point, and spot light conditions.
5. Turn Debug View back to Off before saving checked-in assets.

## Preset Selection

| Preset | Best Fit | Starting Notes |
| --- | --- | --- |
| ClayDiorama | warm rooms, soft clay walls, broad indoor props | Starts from warm ambient bounce and restrained sheen. |
| PaintedPlaster | muted plaster walls, modular corridors, soft indirect response | Good default for large environment walls. |
| MatteToyPlastic | clean toy props and stylized mechanical pieces | Use when the material needs cleaner highlight structure. |
| CeramicToy | bright glazed props, polished trims, readable accent pieces | Best when the surface can support stronger specular contrast. |
| ChalkPastel | powdery surfaces, educational props, pastel walls | Use world gradient carefully to keep the look airy rather than flat. |

## Indoor / Room Authoring

- Start from Wrap Lighting, Ambient Strength, and Shadow Soft Fill before pushing band colors harder.
- Keep Additional Light Mode at Off or FillOnly for most room-filling surfaces.
- Use the indoor room viewpoint before evaluating point or spot light accents.
- If the room still feels dead, check bounce color and indirect stylize before adding more noise.

## ProBuilder Floor / Wall Authoring

- Use authored UVs first. Enable triplanar only when seams or stretch remain visible on the ProBuilder review wall.
- Keep world noise scale coherent across neighboring modules so modular seams do not shimmer.
- Large floor pieces should stay conservative on band noise and specular strength.
- Use the M13 rough-UV and M14 ProBuilder anchors together when judging whether triplanar is worth the cost.

## Stairs, Columns, and Beveled Surfaces

- Stairs reveal whether step transitions stay readable at grazing angles.
- Columns reveal whether specular and bounce lighting are over-concentrated on narrow forms.
- Beveled walls reveal whether band contrast is too hard for softened edges.
- Re-check the same preset on at least one broad surface and one narrow surface before committing it project-wide.

## Lightmap / SSAO Review

- Compare M14LightmappedColumn_Room and M14DynamicColumn_Room before changing indirect stylize or cavity strength.
- Refresh baked lighting for ESL_TestRoom before using the lightmapped column pair as the M14 bake reference.
- Treat SSAO as a support layer, not a replacement for shadow palette work.
- Use M14SSAOOnViewpoint and M14SSAOOffViewpoint as a paired manual review position while toggling the renderer feature.
- If SSAO makes walls dirty, lower cavity or band noise before reducing every ambient term.

## Point / Spot Light Review

- Keep directional-only readability acceptable before enabling local lights.
- Point lights should fill or accent, not replace the main stylized band structure.
- Spot lights are best used to validate highlight response and edge readability on specific props or walls.
- If point and spot lights both make the material noisy, reduce Additional Light Intensity or switch the mode back to FillOnly.

## Triplanar Guidance

- Enable only the triplanar channels that are truly needed.
- Base Map only is cheaper than Base Map + Normal + Noise together.
- Re-check large walls from the fixed viewpoints after every triplanar change.
- Turn triplanar back off on materials that already own stable UVs.

## Shipping Checklist

- Debug View is Off.
- The material still looks acceptable under directional light only.
- Point and spot lights do not overwhelm the main light bands.
- Broad walls and floors do not require aggressive triplanar and high band noise at the same time.
- Lightmapped and non-lightmapped variants remain in the same surface family.
- The material still fits one of the documented use cases.