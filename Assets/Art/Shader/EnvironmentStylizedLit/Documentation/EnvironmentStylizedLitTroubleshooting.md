# EnvironmentStylizedLit Troubleshooting

Use the M14 validation scenes before changing shared preset baselines or broad production materials.

## Common Issues

| Symptom | Likely Cause | Action |
| --- | --- | --- |
| Shadows read as crushed black | Shadow Soft Fill too low, ambient too weak, or shadow palette too cold | Review the indoor room anchors, then raise Shadow Soft Fill or Ambient Strength before repainting every band color. |
| Broad walls look dirty | Band noise or cavity is too strong, often combined with SSAO | Lower LightBandNoiseStrength or CavityStrength first. Re-check with SSAO toggled off. |
| Material looks flat after bake | Indirect stylize and cavity are too weak, or the preset family is wrong | Compare the lightmapped and dynamic column anchors before raising specular. |
| Point lights overpower the main look | Additional Light Intensity or mode is too aggressive | Move back toward FillOnly and reduce intensity until the main light bands remain readable. |
| Spot light highlights look harsh | Specular mode or smoothness is too strong for the chosen preset | Compare CeramicToy and MatteToyPlastic on the lighting-lab strip before retuning manually. |
| Triplanar fixes seams but the material becomes expensive or mushy | Too many triplanar channels are enabled together | Keep only the needed triplanar channels and re-check the rough-UV and ProBuilder anchors. |
| World gradient looks painted on | Min/Max range does not match world scale | Re-anchor gradient Min/Max to scene height instead of object scale. |
| Debug View still appears on saved materials | A review material or test asset kept _DebugView enabled | Reset Debug View to Off and rerun the M13/M14 validation suites. |

## Debug View

- Debug View is for focused investigation only.
- Checked-in production-facing materials should keep Debug View Off.
- Non-development build validation treats active Debug View as a shipping problem.

## Shadow / Indirect Review Order

1. Check the shadow palette and Shadow Soft Fill.
2. Check Ambient Strength and Bounce Strength.
3. Compare lightmapped and dynamic anchors.
4. Only then touch cavity, band noise, or specular.

## Noise / Triplanar Review Order

1. Confirm the UV problem exists.
2. Tune world noise scale before increasing noise strength.
3. Enable triplanar only for the affected channels.
4. Re-check from the fixed near / far viewpoints.

## Use Cases

- Stylized environment walls, floors, ceilings, stairs, columns, and room pieces.
- Modular ProBuilder or authored-mesh stage geometry where stable light bands matter.
- Materials that need clear authoring boundaries between presets, validator rules, and runtime shading.

## Non-Use Cases

- Heavy alpha foliage workflows.
- Pure debug materials that should never be checked in as shipping assets.
- Surfaces that need fully realistic PBR behavior or transparent layering.
- Cases where post-process grading is expected to replace surface authoring.

## Escalation Rule

If a material only works after stacking triplanar, strong band noise, strong cavity, high additional-light intensity, and non-default debug inspection, step back and choose a different preset family or a different shader. That is usually a usage-boundary problem, not a missing knob.