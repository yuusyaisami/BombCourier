# Screen Transition M5 Migration Plan

## 1. Scope
This document tracks the phase-5 replacement of legacy fade/crossfade flows with BC Screen Transition System.

## 2. Completed in this step
- Replaced title-return scene load path in GameLogicManagerMB from direct UI fade + SceneManager.LoadSceneAsync to SceneManagerService.LoadSceneWithTransitionAsync.
- Kept a guarded fallback path when SceneManagerService is unavailable.

## 3. Legacy dependency inventory

### 3.1 Full-screen fade controller (legacy)
- Assets/Scripts/UI/Effect/UIFadeEffectMB.cs
- Assets/Scripts/UI/Effect/UIEffectContracts.cs
- Assets/Scripts/Base/Loading/LoadingSceneService.cs
- Assets/Scripts/Base/Loading/LoadingSceneServiceMB.cs
- Assets/Scripts/Managers/UIManagerMB.cs
- Assets/Scripts/Managers/GameLogicManagerMB.cs

### 3.2 Dual-image crossfade controllers (legacy)
- Assets/Scripts/UI/Title/UITitleMainPageMB.cs
- Assets/Scripts/UI/Title/UIStageSelectPageMB.cs

## 4. Migration order
1. Remove direct SceneManager.LoadSceneAsync + UIFadeEffectMB from gameplay scene transitions.
2. Move LoadingSceneService fade from UIFadeEffectMB to ScreenTransitionServiceMB requests.
3. Replace title dual-image crossfades with texture-to-texture transition material path.
4. Remove fallback-only references, then remove UIFadeEffectMB from primary scene transitions.

## 5. Acceptance checks for M5 progress
- No direct full-screen scene transition path uses UIFadeEffectMB as primary flow.
- Scene transitions use SceneManagerService.LoadSceneWithTransitionAsync.
- Legacy fallback is explicit and logged where still required.
- Existing scenes continue to load without null-reference regressions.
