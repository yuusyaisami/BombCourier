# EnvironmentStylizedLit Documentation

Canonical project docs live in Assets/Docs/ShaderSpec.md and Assets/Docs/ShaderMilestoneSpec.md. Module-local guides are added in later milestones.

Module-local guides:

- EnvironmentStylizedLitPropertyReference.md
- EnvironmentStylizedLitAuthoringGuide.md
- EnvironmentStylizedLitTroubleshooting.md

Current local runtime scope:

- M14 Production Validation / Authoring Guide is implemented. Module-local property, authoring, and troubleshooting guides are checked in, and ESL_TestRoom / ESL_LightingLab now carry fixed production-validation anchors for ProBuilder surfaces, stairs, columns, beveled walls, lightmap review, SSAO review positions, and preset comparison.
- M13 Performance / Variant Cleanup is implemented. Triplanar remains the only material-owned local keyword set, DebugView is blocked by an editor-side non-development build validator, and Low / Medium / High tier definitions live in a dedicated editor-only utility instead of being mixed into presets.
- M13 validation assets are checked in. ESL_Test_TierLow, ESL_Test_TierMedium, and ESL_Test_TierHigh back the M13 wall / floor / additional-light anchors in ESL_TestRoom and ESL_LightingLab, so Triplanar, Additional Lights, and Noise cost can be compared from fixed viewpoints.
- M12 ShaderGUI / Validator / Presets is implemented. The shader now resolves a custom inspector that groups material properties by authoring intent, applies named presets, and normalizes invalid values through the editor-only validator.
- M11 Triplanar / Vertex Color / WorldYGradient is implemented. Triplanar projection lives in Triplanar.hlsl, Surface owns vertex-color mask and world-gradient authoring, and ForwardLit remains a transport-only pass.
- M11 keeps vertex-color A as the reserved special-mask contract from the milestone spec. M11 runtime does not consume that mask yet; later milestones can bind it once a concrete effect is specified.
- M10 Additional Lights remains implemented. Lighting still owns mode-controlled additional light evaluation for Off, FillOnly, Quantized, and Continuous modes, while ForwardLit is limited to URP transport and variant setup.
- M9 baked GI / Light Probe / SSAO compatibility remains in place. ForwardLit still transports baked GI, Light Probe SH, normalized screen-space UV, shadow mask, and AO factors into the indirect lighting path.
- Main-light banding, triplanar sampling, vertex-color masks, indirect GI composition, world-gradient authoring, and editor-only authoring helpers remain separated by file responsibility. Additional lights stay scoped to diffuse/fill contribution, while ShaderGUI, validator, and preset application remain in the Editor-only surface.
- Required render passes from M8 remain intact. ShadowCaster, DepthOnly, and DepthNormalsOnly stay decoupled from stylized lighting, while DepthNormalsOnly continues to provide the SSAO prerequisite normal path.
- Meta still outputs bake-facing albedo and emission only. Stylized diffuse, specular, and noise evaluation stay out of the Meta path.
- EditMode validation contracts live in Assets/Tests/EditMode/EnvironmentStylizedLit.
- Existing validation scenes under Assets/Scenes/EnvironmentStylizedLit are treated as checked-in validation assets; M10 additional-light anchors, M11 triplanar / vertex / gradient anchors, M13 tier / perf viewpoints, and M14 production-validation anchors are generated through the explicit bootstrapper and then checked in.

Bootstrap command for the full M14 validation surface:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe" -projectPath . -batchmode -nographics -executeMethod BC.Rendering.EnvironmentStylizedLitValidationBootstrapper.BootstrapM14ValidationAssets -logFile unity_m14_bootstrap.log -quit
```
