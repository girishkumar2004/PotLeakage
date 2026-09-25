# Builder script for PotLeakage_Main.unity
param()

$samplePath = "d:\unity cli\PotLeakage\Assets\Scenes\SampleScene.unity"
$targetScenePath = "d:\unity cli\PotLeakage\Assets\PotLeakage\Scenes\PotLeakage_Main.unity"

$raw = Get-Content $samplePath -Raw

# 1. Extract Settings Header (Occlusion, RenderSettings, Lightmap, NavMesh)
$splitIndex = $raw.IndexOf("--- !u!1 &330585543")
$header = $raw.Substring(0, $splitIndex)

# 2. Extract Machine objects and their components from SampleScene
$sceneObjectsPart = $raw.Substring($raw.IndexOf("--- !u!1 &1130440524"))

$sb = [System.Text.StringBuilder]::new()
$null = $sb.Append($header)

# Append Environment root & Floor & Lighting & Global Volume
$sb.AppendLine(@"
--- !u!1 &100000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100000001}
  m_Layer: 0
  m_Name: Environment
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100000001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 100000011}
  - {fileID: 100000021}
  - {fileID: 100000031}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &100000010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100000011}
  m_Layer: 0
  m_Name: PotRoom
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100000011
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000010}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 100000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &100000020
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100000021}
  - component: {fileID: 100000022}
  m_Layer: 0
  m_Name: Directional Light
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100000021
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000020}
  serializedVersion: 2
  m_LocalRotation: {x: 0.38268343, y: -0.1464466, z: 0.060670868, w: 0.90998393}
  m_LocalPosition: {x: 0, y: 8, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 100000001}
  m_LocalEulerAnglesHint: {x: 45, y: -20, z: 0}
--- !u!108 &100000022
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000020}
  m_Enabled: 1
  serializedVersion: 11
  m_Type: 1
  m_Color: {r: 1, g: 0.96, b: 0.9, a: 1}
  m_Intensity: 1.8
  m_Range: 10
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.80208
  m_CookieSize: 10
  m_Shadows:
    m_Type: 2
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 1
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
    m_CullingMatrixOverride:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
    m_UseCullingMatrixOverride: 0
  m_Cookie: {fileID: 0}
  m_DrawHalo: 0
  m_Flare: {fileID: 0}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {x: 1, y: 1}
  m_BounceIntensity: 1
  m_ColorTemperature: 6570
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {x: 0, y: 0, z: 0, w: 0}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_ForceVisible: 0
  m_ShadowRadius: 0
  m_ShadowAngle: 0
  m_LightUnit: 1
  m_LuxAtDistance: 1
  m_EnableSpotReflector: 1
--- !u!1 &100000030
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100000031}
  - component: {fileID: 100000032}
  m_Layer: 0
  m_Name: Global Volume
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100000031
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000030}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 100000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &100000032
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000030}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 172515602e62fb746b5d573b38a5fe58, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  isGlobal: 1
  priority: 0
  blendDistance: 0
  weight: 1
  sharedProfile: {fileID: 0}
"@)

# CameraSystem & TransformPoints
$sb.AppendLine(@"
--- !u!1 &200000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000001}
  m_Layer: 0
  m_Name: CameraSystem
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 200000011}
  - {fileID: 200000021}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &200000010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000011}
  - component: {fileID: 200000012}
  - component: {fileID: 200000013}
  - component: {fileID: 200000014}
  - component: {fileID: 200000015}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000011
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000010}
  serializedVersion: 2
  m_LocalRotation: {x: 0.15643446, y: 0, z: 0, w: 0.98768836}
  m_LocalPosition: {x: 0, y: 5.5, z: -12.5}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 200000001}
  m_LocalEulerAnglesHint: {x: 18, y: 0, z: 0}
--- !u!20 &200000012
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000010}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {r: 0.192, g: 0.302, b: 0.475, a: 0}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {x: 2, y: 11}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {x: 36, y: 24}
  m_LensShift: {x: 0, y: 0}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.1
  far clip plane: 1000
  field of view: 55
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {fileID: 0}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!81 &200000013
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000010}
  m_Enabled: 1
--- !u!114 &200000014
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: a79441f348de89743a2939f4d699eac1, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_RenderShadows: 1
  m_RequiresDepthTextureOption: 2
  m_RequiresOpaqueTextureOption: 2
  m_CameraType: 0
  m_Cameras: []
  m_RendererIndex: -1
  m_VolumeLayerMask:
    serializedVersion: 2
    m_Bits: 1
  m_VolumeTrigger: {fileID: 0}
  m_VolumeFrameworkUpdateModeOption: 2
  m_RenderPostProcessing: 1
  m_Antialiasing: 0
  m_AntialiasingQuality: 2
  m_Dithering: 0
  m_ClearDepth: 1
  m_AllowXRRendering: 1
  m_AllowHDROutput: 1
--- !u!114 &200000015
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4d576c2eb9215bc43ab2082e0c26830d, type: 3}
  m_Name: 
  m_EditorClassIdentifier: PotLeakage.Camera::PotLeakageCameraController
  welcomeTarget: {fileID: 200000031}
  normalPotOperationTarget: {fileID: 200000041}
  idealPotVoltageTarget: {fileID: 200000051}
  idealCBTTemperatureTarget: {fileID: 200000061}
  potMachineLookTarget: {fileID: 200000071}
  cbtLookTarget: {fileID: 200000081}
  defaultDuration: 2
  quickTransitionDuration: 0.8
  movementCurve:
    serializedVersion: 2
    m_Curve:
    - serializedVersion: 3
      time: 0
      value: 0
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    - serializedVersion: 3
      time: 1
      value: 1
      inSlope: 0
      outSlope: 0
      tangentMode: 0
      weightedMode: 0
      inWeight: 0
      outWeight: 0
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  cameraTransitionAudio: {fileID: 0}
--- !u!1 &200000020
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000021}
  m_Layer: 0
  m_Name: TransformPoints
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000021
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000020}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 200000031}
  - {fileID: 200000041}
  - {fileID: 200000051}
  - {fileID: 200000061}
  - {fileID: 200000071}
  - {fileID: 200000081}
  m_Father: {fileID: 200000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &200000030
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000031}
  m_Layer: 0
  m_Name: Welcome
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000031
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000030}
  serializedVersion: 2
  m_LocalRotation: {x: 0.15643446, y: 0, z: 0, w: 0.98768836}
  m_LocalPosition: {x: 0, y: 5.5, z: -12.5}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 200000021}
  m_LocalEulerAnglesHint: {x: 18, y: 0, z: 0}
--- !u!1 &200000040
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000041}
  m_Layer: 0
  m_Name: Normal Pot Operation
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000041
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000040}
  serializedVersion: 2
  m_LocalRotation: {x: 0.12186934, y: 0, z: 0, w: 0.99254615}
  m_LocalPosition: {x: 0, y: 3.2, z: -6.5}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 200000021}
  m_LocalEulerAnglesHint: {x: 14, y: 0, z: 0}
--- !u!1 &200000050
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000051}
  m_Layer: 0
  m_Name: Ideal Pot Voltage
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000051
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000050}
  serializedVersion: 2
  m_LocalRotation: {x: 0.12186934, y: 0, z: 0, w: 0.99254615}
  m_LocalPosition: {x: 0, y: 3.2, z: -6.5}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 200000021}
  m_LocalEulerAnglesHint: {x: 14, y: 0, z: 0}
--- !u!1 &200000060
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000061}
  m_Layer: 0
  m_Name: Ideal CBT Temperature
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000061
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000060}
  serializedVersion: 2
  m_LocalRotation: {x: 0.10214812, y: 0.21327178, z: -0.022485547, w: 0.9713837}
  m_LocalPosition: {x: -2.2, y: 1.8, z: -3.2}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 200000021}
  m_LocalEulerAnglesHint: {x: 12, y: 25, z: 0}
--- !u!1 &200000070
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000071}
  m_Layer: 0
  m_Name: PotMachine_LookTarget
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000071
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000070}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 1.5, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 200000021}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &200000080
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 200000081}
  m_Layer: 0
  m_Name: CBT_LookTarget
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200000081
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 200000080}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: -0.8, y: 1, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 200000021}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"@)

# Highlight System & VFX
$sb.AppendLine(@"
--- !u!1 &300000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 300000001}
  m_Layer: 0
  m_Name: HighlightSystem
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &300000001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 300000000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 300000011}
  - {fileID: 300000021}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &300000010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 300000011}
  m_Layer: 0
  m_Name: PotMachine_Highlight
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &300000011
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 300000010}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 300000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &300000020
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 300000021}
  m_Layer: 0
  m_Name: CBT_Highlight
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &300000021
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 300000020}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 300000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &400000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 400000001}
  m_Layer: 0
  m_Name: VFX
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &400000001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 400000000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 400000011}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &400000010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 400000011}
  - component: {fileID: 400000012}
  - component: {fileID: 400000013}
  - component: {fileID: 400000014}
  m_Layer: 0
  m_Name: MoltenAluminium_VFX
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!4 &400000011
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 400000010}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: -1.2, y: 0.9, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 400000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!198 &400000012
ParticleSystem:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 400000010}
  serializedVersion: 7
  lengthInSec: 4
  startDelay: {minMaxState: 0, scalar: 0, minScalar: 0, maxCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}, minCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}}
  speed: 1
  randomSeed: 0
  autoRandomSeed: 1
  looping: 1
  prewarm: 0
  playOnAwake: 0
  useUnscaledTime: 0
  autoDisable: 0
  InitialModule:
    serializedVersion: 4
    startLifetime: {minMaxState: 0, scalar: 2, minScalar: 2, maxCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}, minCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}}
    startSpeed: {minMaxState: 0, scalar: 0.25, minScalar: 0.25, maxCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}, minCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}}
    startColor: {minMaxState: 0, minColor: {r: 1, g: 0.62, b: 0.15, a: 0.9}, maxColor: {r: 1, g: 0.62, b: 0.15, a: 0.9}, maxGradient: {serializedVersion: 2, key0: {r: 1, g: 1, b: 1, a: 1}, key1: {r: 1, g: 1, b: 1, a: 1}, key2: {r: 0, g: 0, b: 0, a: 0}, key3: {r: 0, g: 0, b: 0, a: 0}, key4: {r: 0, g: 0, b: 0, a: 0}, key5: {r: 0, g: 0, b: 0, a: 0}, key6: {r: 0, g: 0, b: 0, a: 0}, key7: {r: 0, g: 0, b: 0, a: 0}, ctime0: 0, ctime1: 65535, ctime2: 0, ctime3: 0, ctime4: 0, ctime5: 0, ctime6: 0, ctime7: 0, atime0: 0, atime1: 65535, atime2: 0, atime3: 0, atime4: 0, atime5: 0, atime6: 0, atime7: 0, m_Mode: 0, m_ColorSpace: -1, m_NumColorKeys: 2, m_NumAlphaKeys: 2}, minGradient: {serializedVersion: 2, key0: {r: 1, g: 1, b: 1, a: 1}, key1: {r: 1, g: 1, b: 1, a: 1}, key2: {r: 0, g: 0, b: 0, a: 0}, key3: {r: 0, g: 0, b: 0, a: 0}, key4: {r: 0, g: 0, b: 0, a: 0}, key5: {r: 0, g: 0, b: 0, a: 0}, key6: {r: 0, g: 0, b: 0, a: 0}, key7: {r: 0, g: 0, b: 0, a: 0}, ctime0: 0, ctime1: 65535, ctime2: 0, ctime3: 0, ctime4: 0, ctime5: 0, ctime6: 0, ctime7: 0, atime0: 0, atime1: 65535, atime2: 0, atime3: 0, atime4: 0, atime5: 0, atime6: 0, atime7: 0, m_Mode: 0, m_ColorSpace: -1, m_NumColorKeys: 2, m_NumAlphaKeys: 2}}
    startSize: {minMaxState: 0, scalar: 0.05, minScalar: 0.05, maxCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}, minCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}}
    maxNumParticles: 100
  ShapeModule:
    serializedVersion: 7
    enabled: 1
    type: 0
    radius: 0.5
    scale: {x: 0.8, y: 0.3, z: 0.8}
  EmissionModule:
    serializedVersion: 5
    enabled: 1
    rateOverTime: {minMaxState: 0, scalar: 15, minScalar: 15, maxCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}, minCurve: {serializedVersion: 2, m_Curve: [], m_PreInfinity: 2, m_PostInfinity: 2, m_RotationOrder: 4}}
--- !u!199 &400000013
ParticleSystemRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 400000010}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}
--- !u!114 &400000014
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 400000010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f5eea448536a57140b02c40423e310d6, type: 3}
  m_Name: 
  m_EditorClassIdentifier: PotLeakage.VFX::MoltenAluminiumVFXController
  moltenAluminiumVFXTransform: {fileID: 400000011}
  maxParticles: 100
  emissionRate: 15
  particleSize: 0.05
  glowIntensity: 1.8
"@)

# UI System (Canvas with Welcome Panel containing Header, CompanyLogo, Content, and Footer)
$sb.AppendLine(@"
--- !u!1 &500000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000001}
  m_Layer: 5
  m_Name: UI
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &500000001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 500000011}
  - {fileID: 500000021}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &500000010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000011}
  - component: {fileID: 500000012}
  - component: {fileID: 500000013}
  - component: {fileID: 500000014}
  m_Layer: 5
  m_Name: Canvas
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &500000011
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000010}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 0, y: 0, z: 0}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 500000041}
  m_Father: {fileID: 500000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0, y: 0}
--- !u!223 &500000012
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000010}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 25
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0
--- !u!114 &500000013
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0cd44c1031e13a943bb63640046fad76, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {x: 1920, y: 1080}
  m_ScreenMatchMode: 0
  m_MatchWidthOrHeight: 0.5
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
--- !u!114 &500000014
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: dc42784cf147c0c48a680349fa168899, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
--- !u!1 &500000020
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000021}
  - component: {fileID: 500000022}
  - component: {fileID: 500000023}
  m_Layer: 5
  m_Name: EventSystem
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &500000021
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000020}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 500000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &500000022
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000020}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 76c392e42b5098c458856cdf6ecaaaa1, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_FirstSelected: {fileID: 0}
  m_sendNavigationEvents: 1
  m_DragThreshold: 10
--- !u!114 &500000023
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000020}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 01614664b831546d2ae94a42149d80ac, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_SendPointerHoverToParent: 1
  m_MoveRepeatDelay: 0.5
  m_MoveRepeatRate: 0.1
  m_XRTrackingOrigin: {fileID: 0}
  m_ActionsAsset: {fileID: -944628639613478452, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_PointAction: {fileID: -1654692200621890270, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_LeftClickAction: {fileID: 3001919216984110777, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_MiddleClickAction: {fileID: -2185481490778289446, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_RightClickAction: {fileID: -4090225696740746782, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_ScrollWheelAction: {fileID: 6240969308177333660, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_MoveAction: {fileID: -8784545083839296357, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_SubmitAction: {fileID: 392368643174621059, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
  m_CancelAction: {fileID: 7727032971353384032, guid: ca9f5fa95ffab41fb9a615ab714db018, type: 3}
--- !u!1 &500000040
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000041}
  - component: {fileID: 500000042}
  - component: {fileID: 500000043}
  m_Layer: 5
  m_Name: Welcome Panel
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &500000041
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000040}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 500000051}
  - {fileID: 500000061}
  - {fileID: 500000071}
  - {fileID: 500000081}
  - {fileID: 500000091}
  - {fileID: 500000101}
  - {fileID: 500000111}
  m_Father: {fileID: 500000011}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0}
  m_AnchorMax: {x: 0.5, y: 0}
  m_AnchoredPosition: {x: 0, y: 40}
  m_SizeDelta: {x: 1100, y: 260}
  m_Pivot: {x: 0.5, y: 0}
--- !u!222 &500000042
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000040}
  m_CullTransparentMesh: 1
--- !u!114 &500000043
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000040}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 0.04, g: 0.07, b: 0.12, a: 0.92}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_UseSpriteMesh: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseAmount: 1
--- !u!1 &500000050
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000051}
  - component: {fileID: 500000052}
  - component: {fileID: 500000053}
  m_Layer: 5
  m_Name: ProgressText
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &500000051
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000050}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 500000041}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 35, y: -20}
  m_SizeDelta: {x: -200, y: 25}
  m_Pivot: {x: 0, y: 1}
--- !u!222 &500000052
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000050}
  m_CullTransparentMesh: 1
--- !u!114 &500000053
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000050}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 0, g: 0.9, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: "<color=#00E5FF>TASK 01 / 04</color>"
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 0, g: 0.9, b: 1, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontSize: 17
  m_fontSizeBase: 17
  m_fontWeight: 700
  m_fontStyle: 1
  m_HorizontalAlignment: 1
  m_VerticalAlignment: 256
--- !u!1 &500000060
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000061}
  - component: {fileID: 500000062}
  - component: {fileID: 500000063}
  m_Layer: 5
  m_Name: Title
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &500000061
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000060}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 500000041}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 35, y: -50}
  m_SizeDelta: {x: -200, y: 35}
  m_Pivot: {x: 0, y: 1}
--- !u!222 &500000062
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000060}
  m_CullTransparentMesh: 1
--- !u!114 &500000063
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000060}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: WELCOME TO VEDANTA ALUMINIUM
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 1, g: 1, b: 1, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontSize: 26
  m_fontSizeBase: 26
  m_fontWeight: 700
  m_fontStyle: 1
  m_HorizontalAlignment: 1
  m_VerticalAlignment: 256
--- !u!1 &500000070
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000071}
  - component: {fileID: 500000072}
  - component: {fileID: 500000073}
  m_Layer: 5
  m_Name: Description
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &500000071
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000070}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 500000041}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0.72, y: 1}
  m_AnchoredPosition: {x: 35, y: -95}
  m_SizeDelta: {x: 0, y: 95}
  m_Pivot: {x: 0, y: 1}
--- !u!222 &500000072
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000070}
  m_CullTransparentMesh: 1
--- !u!114 &500000073
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000070}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 0.85, g: 0.9, b: 0.95, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: "POT LEAKAGE VISUALIZATION\n\nA guided visual introduction to pot operation,
    pot voltage and collector bar temperature."
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 0.85, g: 0.9, b: 0.95, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontSize: 18
  m_fontSizeBase: 18
  m_fontWeight: 400
  m_fontStyle: 0
  m_HorizontalAlignment: 1
  m_VerticalAlignment: 256
  m_enableWordWrapping: 1
--- !u!1 &500000080
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000081}
  - component: {fileID: 500000082}
  - component: {fileID: 500000083}
  m_Layer: 5
  m_Name: StatusText
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &500000081
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000080}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 500000041}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0.72, y: 0}
  m_AnchoredPosition: {x: 35, y: 25}
  m_SizeDelta: {x: 0, y: 35}
  m_Pivot: {x: 0, y: 0}
--- !u!222 &500000082
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000080}
  m_CullTransparentMesh: 1
--- !u!114 &500000083
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000080}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 0.1, g: 1, b: 0.5, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: 'POT STATUS: <color=#00FF66>NORMAL OPERATION</color>'
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 0.1, g: 1, b: 0.5, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontSize: 18
  m_fontSizeBase: 18
  m_fontWeight: 700
  m_fontStyle: 1
  m_HorizontalAlignment: 1
  m_VerticalAlignment: 256
--- !u!1 &500000090
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000091}
  - component: {fileID: 500000092}
  - component: {fileID: 500000093}
  m_Layer: 5
  m_Name: ValueText
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &500000091
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000090}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 500000041}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0.72, y: 0}
  m_AnchoredPosition: {x: 35, y: 25}
  m_SizeDelta: {x: 0, y: 55}
  m_Pivot: {x: 0, y: 0}
--- !u!222 &500000092
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000090}
  m_CullTransparentMesh: 1
--- !u!114 &500000093
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000090}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: "NORMAL PRODUCTION VALUE:\n<size=110%><b><color=#FFAA00>VALUE NOT CONFIGURED</color></b></size>"
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 1, g: 1, b: 1, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontSize: 18
  m_fontSizeBase: 18
  m_fontWeight: 400
  m_fontStyle: 0
  m_HorizontalAlignment: 1
  m_VerticalAlignment: 256
--- !u!1 &500000100
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000101}
  - component: {fileID: 500000102}
  - component: {fileID: 500000103}
  - component: {fileID: 500000104}
  m_Layer: 5
  m_Name: NextButton
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &500000101
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000100}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 500000106}
  m_Father: {fileID: 500000041}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 0}
  m_AnchorMax: {x: 1, y: 0}
  m_AnchoredPosition: {x: -35, y: 30}
  m_SizeDelta: {x: 220, y: 65}
  m_Pivot: {x: 1, y: 0}
--- !u!222 &500000102
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000100}
  m_CullTransparentMesh: 1
--- !u!114 &500000103
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 0, g: 0.55, b: 0.85, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_UseSpriteMesh: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseAmount: 1
--- !u!114 &500000104
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000100}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.1, g: 0.7, b: 1, a: 1}
    m_PressedColor: {r: 0, g: 0.4, b: 0.7, a: 1}
    m_SelectedColor: {r: 1, g: 1, b: 1, a: 1}
    m_DisabledColor: {r: 0.78, g: 0.78, b: 0.78, a: 0.5}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: 500000103}
  m_OnClick:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: 600000043}
        m_TargetAssemblyTypeName: SequenceHelperFunctions, Assembly-CSharp
        m_MethodName: CompleteCurrentTask
        m_Mode: 1
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          m_IntArgument: 0
          m_FloatArgument: 0
          m_StringArgument: 
          m_BoolArgument: 0
        m_CallState: 2
--- !u!1 &500000105
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000106}
  - component: {fileID: 500000107}
  - component: {fileID: 500000108}
  m_Layer: 5
  m_Name: Text
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &500000106
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000105}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 500000101}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &500000107
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000105}
  m_CullTransparentMesh: 1
--- !u!114 &500000108
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000105}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: <b>NEXT  ></b>
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 1, g: 1, b: 1, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontSize: 22
  m_fontSizeBase: 22
  m_fontWeight: 700
  m_fontStyle: 1
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
--- !u!1 &500000110
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 500000111}
  - component: {fileID: 500000112}
  - component: {fileID: 500000113}
  m_Layer: 5
  m_Name: CompanyLogo
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &500000111
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000110}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 500000041}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 1, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: -25, y: -15}
  m_SizeDelta: {x: 75, y: 75}
  m_Pivot: {x: 1, y: 1}
--- !u!222 &500000112
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000110}
  m_CullTransparentMesh: 1
--- !u!114 &500000113
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 500000110}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 21300000, guid: 8e5f2a1b9c4d3e2f1a0b8c7d6e5f4a3b, type: 3}
  m_Type: 0
  m_UseSpriteMesh: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseAmount: 1
"@)

# TrainingFramework (Manager, Sequence, Sequence Handler, Sequence Helper)
$sb.AppendLine(@"
--- !u!1 &600000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 600000001}
  m_Layer: 0
  m_Name: TrainingFramework
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &600000001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 600000011}
  - {fileID: 600000021}
  - {fileID: 600000031}
  - {fileID: 600000041}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1 &600000010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 600000011}
  - component: {fileID: 600000012}
  - component: {fileID: 600000013}
  - component: {fileID: 600000014}
  - component: {fileID: 600000015}
  - component: {fileID: 600000016}
  m_Layer: 0
  m_Name: Manager
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &600000011
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000010}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 600000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &600000012
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: e7b7066ffc89e1441942703e97340541, type: 3}
  m_Name: 
  m_EditorClassIdentifier: PotLeakage.UI::PotLeakageUIController
  headerPanel: {fileID: 0}
  welcomePanel: {fileID: 500000040}
  titleText: {fileID: 500000063}
  descriptionText: {fileID: 500000073}
  progressText: {fileID: 500000053}
  companyLogo: {fileID: 500000113}
  nextButton: {fileID: 500000104}
  valueDisplay: {fileID: 600000013}
  potMachineHighlightTarget: {fileID: 5575632638316201680}
  cbtHighlightTarget: {fileID: 120000000}
  welcomeVoiceover: {fileID: 0}
  normalOperationVoiceover: {fileID: 0}
  idealPotVoltageVoiceover: {fileID: 0}
  idealCBTTemperatureVoiceover: {fileID: 0}
  welcomeSFX: {fileID: 0}
  normalOperationSFX: {fileID: 0}
  idealPotVoltageSFX: {fileID: 0}
  idealCBTTemperatureSFX: {fileID: 0}
  cameraTransitionSFX: {fileID: 0}
  vfxSFX: {fileID: 0}
  voiceoverAudioSource: {fileID: 600000015}
  sfxAudioSource: {fileID: 600000016}
--- !u!114 &600000013
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5c0a9f400e0391a4a981663cce04da01, type: 3}
  m_Name: 
  m_EditorClassIdentifier: PotLeakage.UI::PotLeakageValueDisplay
  config: {fileID: 11400000, guid: 546a79f573e4cec4080879712e8f78d9, type: 2}
  normalProductionPotVoltage: 0
  normalProductionCBTTemperature: 0
  titleText: {fileID: 500000063}
  descriptionText: {fileID: 500000073}
  valueText: {fileID: 500000093}
  statusText: {fileID: 500000083}
  progressText: {fileID: 500000053}
--- !u!114 &600000014
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000010}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 6e9b418a03c800e408ecbb42c55127cf, type: 3}
  m_Name: 
  m_EditorClassIdentifier: PotLeakage.Core::PotLeakageManager
  mainCamera: {fileID: 200000012}
  welcomeTarget: {fileID: 200000031}
  normalPotOperationTarget: {fileID: 200000041}
  idealPotVoltageTarget: {fileID: 200000051}
  idealCBTTemperatureTarget: {fileID: 200000061}
  cameraController: {fileID: 200000015}
  potMachineHighlightTarget: {fileID: 5575632638316201680}
  cbtHighlightTarget: {fileID: 120000000}
  welcomePanel: {fileID: 500000040}
  panelTitle: {fileID: 500000063}
  panelDescription: {fileID: 500000073}
  valueText: {fileID: 500000093}
  statusText: {fileID: 500000083}
  progressText: {fileID: 500000053}
  companyLogo: {fileID: 500000113}
  nextButton: {fileID: 500000104}
  uiController: {fileID: 600000012}
  valueDisplay: {fileID: 600000013}
  welcomeVoiceover: {fileID: 0}
  normalOperationVoiceover: {fileID: 0}
  idealPotVoltageVoiceover: {fileID: 0}
  idealCBTTemperatureVoiceover: {fileID: 0}
  welcomeSFX: {fileID: 0}
  normalOperationSFX: {fileID: 0}
  idealPotVoltageSFX: {fileID: 0}
  idealCBTTemperatureSFX: {fileID: 0}
  cameraTransitionSFX: {fileID: 0}
  vfxSFX: {fileID: 0}
  moltenAluminiumVFX: {fileID: 400000014}
  sequenceHandler: {fileID: 600000032}
  sequenceHelper: {fileID: 600000043}
  sequenceAsset: {fileID: 600000022}
  potLeakageConfig: {fileID: 11400000, guid: 546a79f573e4cec4080879712e8f78d9, type: 2}
--- !u!82 &600000015
AudioSource:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000010}
  m_Enabled: 1
  serializedVersion: 4
  OutputAudioMixerGroup: {fileID: 0}
  m_audioClip: {fileID: 0}
  m_PlayOnAwake: 0
  m_Volume: 1
  m_Pitch: 1
  Loop: 0
  Mute: 0
  Spatialize: 0
  SpatializePostEffects: 0
  Priority: 128
  DopplerLevel: 1
  MinDistance: 1
  MaxDistance: 500
  Pan2D: 0
  rolloffMode: 0
  BypassEffects: 0
  BypassListenerEffects: 0
  BypassReverbZones: 0
  rolloffCustomCurve:
    serializedVersion: 2
    m_Curve: []
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  panLevelCustomCurve:
    serializedVersion: 2
    m_Curve: []
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  spreadCustomCurve:
    serializedVersion: 2
    m_Curve: []
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  reverbZoneMixCustomCurve:
    serializedVersion: 2
    m_Curve: []
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
--- !u!82 &600000016
AudioSource:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000010}
  m_Enabled: 1
  serializedVersion: 4
  OutputAudioMixerGroup: {fileID: 0}
  m_audioClip: {fileID: 0}
  m_PlayOnAwake: 0
  m_Volume: 1
  m_Pitch: 1
  Loop: 0
  Mute: 0
  Spatialize: 0
  SpatializePostEffects: 0
  Priority: 128
  DopplerLevel: 1
  MinDistance: 1
  MaxDistance: 500
  Pan2D: 0
  rolloffMode: 0
  BypassEffects: 0
  BypassListenerEffects: 0
  BypassReverbZones: 0
  rolloffCustomCurve:
    serializedVersion: 2
    m_Curve: []
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  panLevelCustomCurve:
    serializedVersion: 2
    m_Curve: []
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  spreadCustomCurve:
    serializedVersion: 2
    m_Curve: []
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  reverbZoneMixCustomCurve:
    serializedVersion: 2
    m_Curve: []
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
--- !u!1 &600000020
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 600000021}
  - component: {fileID: 600000022}
  m_Layer: 0
  m_Name: Sequence
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &600000021
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000020}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 600000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &600000022
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000020}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 84496e5ff052fe644b3497c77c5d088f, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Sequence
  SequenceName: PotLeakage_Introduction
  TaskList:
  - TaskName: 01 \u2013 Welcome
    TaskCompleted: 0
    TriggerCompleted: 0
    typeOfInteraction: 0
    EventsToFollow:
      m_PersistentCalls:
        m_Calls:
        - m_Target: {fileID: 200000015}
          m_TargetAssemblyTypeName: PotLeakage.Camera.PotLeakageCameraController, Assembly-CSharp
          m_MethodName: MoveToTarget
          m_Mode: 2
          m_Arguments:
            m_ObjectArgument: {fileID: 200000031}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Transform, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000012}
          m_TargetAssemblyTypeName: PotLeakage.UI.PotLeakageUIController, Assembly-CSharp
          m_MethodName: SetTask01UI
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000043}
          m_TargetAssemblyTypeName: SequenceHelperFunctions, Assembly-CSharp
          m_MethodName: ClearAllHighlights
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 400000014}
          m_TargetAssemblyTypeName: PotLeakage.VFX.MoltenAluminiumVFXController, Assembly-CSharp
          m_MethodName: DisableVFX
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
    instructionText: POT LEAKAGE VISUALIZATION
    ttsTextOrKey: 
    useTTS: 0
    audioClipOverride: {fileID: 0}
    completionMode: 0
  - TaskName: 02 \u2013 Normal Pot Operation
    TaskCompleted: 0
    TriggerCompleted: 0
    typeOfInteraction: 0
    EventsToFollow:
      m_PersistentCalls:
        m_Calls:
        - m_Target: {fileID: 200000015}
          m_TargetAssemblyTypeName: PotLeakage.Camera.PotLeakageCameraController, Assembly-CSharp
          m_MethodName: MoveToTarget
          m_Mode: 2
          m_Arguments:
            m_ObjectArgument: {fileID: 200000041}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Transform, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000043}
          m_TargetAssemblyTypeName: SequenceHelperFunctions, Assembly-CSharp
          m_MethodName: HighlightObject
          m_Mode: 2
          m_Arguments:
            m_ObjectArgument: {fileID: 5575632638316201680}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.GameObject, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000012}
          m_TargetAssemblyTypeName: PotLeakage.UI.PotLeakageUIController, Assembly-CSharp
          m_MethodName: SetTask02UI
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 400000014}
          m_TargetAssemblyTypeName: PotLeakage.VFX.MoltenAluminiumVFXController, Assembly-CSharp
          m_MethodName: DisableVFX
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
    instructionText: During normal production, the pot operates under controlled electrical
      and thermal conditions.
    ttsTextOrKey: 
    useTTS: 0
    audioClipOverride: {fileID: 0}
    completionMode: 0
  - TaskName: 03 \u2013 Ideal Pot Voltage
    TaskCompleted: 0
    TriggerCompleted: 0
    typeOfInteraction: 0
    EventsToFollow:
      m_PersistentCalls:
        m_Calls:
        - m_Target: {fileID: 200000015}
          m_TargetAssemblyTypeName: PotLeakage.Camera.PotLeakageCameraController, Assembly-CSharp
          m_MethodName: MoveToTarget
          m_Mode: 2
          m_Arguments:
            m_ObjectArgument: {fileID: 200000051}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Transform, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000043}
          m_TargetAssemblyTypeName: SequenceHelperFunctions, Assembly-CSharp
          m_MethodName: HighlightObject
          m_Mode: 2
          m_Arguments:
            m_ObjectArgument: {fileID: 5575632638316201680}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.GameObject, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000012}
          m_TargetAssemblyTypeName: PotLeakage.UI.PotLeakageUIController, Assembly-CSharp
          m_MethodName: SetTask03UI
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 400000014}
          m_TargetAssemblyTypeName: PotLeakage.VFX.MoltenAluminiumVFXController, Assembly-CSharp
          m_MethodName: DisableVFX
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
    instructionText: Technical in-charge monitors pot voltage during operations.
    ttsTextOrKey: 
    useTTS: 0
    audioClipOverride: {fileID: 0}
    completionMode: 0
  - TaskName: 04 \u2013 Ideal CBT Temperature
    TaskCompleted: 0
    TriggerCompleted: 0
    typeOfInteraction: 0
    EventsToFollow:
      m_PersistentCalls:
        m_Calls:
        - m_Target: {fileID: 200000015}
          m_TargetAssemblyTypeName: PotLeakage.Camera.PotLeakageCameraController, Assembly-CSharp
          m_MethodName: MoveToTarget
          m_Mode: 2
          m_Arguments:
            m_ObjectArgument: {fileID: 200000061}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Transform, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000043}
          m_TargetAssemblyTypeName: SequenceHelperFunctions, Assembly-CSharp
          m_MethodName: RemoveHighlight
          m_Mode: 2
          m_Arguments:
            m_ObjectArgument: {fileID: 5575632638316201680}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.GameObject, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000043}
          m_TargetAssemblyTypeName: SequenceHelperFunctions, Assembly-CSharp
          m_MethodName: HighlightObject
          m_Mode: 2
          m_Arguments:
            m_ObjectArgument: {fileID: 120000000}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.GameObject, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 600000012}
          m_TargetAssemblyTypeName: PotLeakage.UI.PotLeakageUIController, Assembly-CSharp
          m_MethodName: SetTask04UI
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
        - m_Target: {fileID: 400000014}
          m_TargetAssemblyTypeName: PotLeakage.VFX.MoltenAluminiumVFXController, Assembly-CSharp
          m_MethodName: EnableVFX
          m_Mode: 1
          m_Arguments:
            m_ObjectArgument: {fileID: 0}
            m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            m_IntArgument: 0
            m_FloatArgument: 0
            m_StringArgument: 
            m_BoolArgument: 0
          m_CallState: 2
    instructionText: Collector Bar Temperature (CBT) is monitored as an important
      indicator of pot condition. High CBT is an early symptom associated with potential
      collector bar leakage.
    ttsTextOrKey: 
    useTTS: 0
    audioClipOverride: {fileID: 0}
    completionMode: 0
--- !u!1 &600000030
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 600000031}
  - component: {fileID: 600000032}
  m_Layer: 0
  m_Name: Sequence Handler
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &600000031
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000030}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 600000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &600000032
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000030}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3b5d9a79be9f1764889ec26c99cc94e6, type: 3}
  m_Name: 
  m_EditorClassIdentifier: SequenceHandler
  autostart: 1
  sequenceList:
  - {fileID: 600000022}
  currentSequence: 0
  currentTask: 0
  WaitForTrigger: 0
  isSequenceMode: 1
  lockSequence: 0
--- !u!1 &600000040
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 600000041}
  - component: {fileID: 600000043}
  m_Layer: 0
  m_Name: Sequence Helper
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &600000041
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000040}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 600000001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &600000043
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 600000040}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 6c7c03354a4a4ce46805bcdc9a5a5d20, type: 3}
  m_Name: 
  m_EditorClassIdentifier: SequenceHelperFunctions
  handler: {fileID: 600000032}
  BG_Audio: {fileID: 600000016}
  Voice_Audio: {fileID: 600000015}
  sequenceAnimations: []
  _fadeobj: {fileID: 0}
  fadeMaterial: {fileID: 0}
  fadeDuration: 1
  player: {fileID: 0}
  _nextPlayerTransform: {fileID: 0}
  UI_Canvas: {fileID: 500000010}
  TitleText: {fileID: 500000063}
  titleString: 
  DescriptionText: {fileID: 500000073}
  desString: 
  _nextCanvasTransform: {fileID: 0}
  nextButton: {fileID: 500000104}
  previousButton: {fileID: 0}
  transparentMaterial: {fileID: 2100000, guid: de5f06cfc6d4c6b4a928f896d754db68, type: 2}
  _isTransparent: 0
  movementMappings: []
  currentTarget: {fileID: 0}
  ppeObjectToEnable: {fileID: 0}
  multiGrabMappings: []
"@)

# Append Fire object inside Machine
$sb.AppendLine(@"
--- !u!1 &120000000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 120000001}
  - component: {fileID: 120000002}
  - component: {fileID: 120000003}
  m_Layer: 0
  m_Name: Fire
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &120000001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 120000000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: -1.2, y: 0.9, z: 0}
  m_LocalScale: {x: 0.6, y: 0.4, z: 0.6}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!33 &120000002
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 120000000}
  m_Mesh: {fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}
--- !u!23 &120000003
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 120000000}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 2100000, guid: de5f06cfc6d4c6b4a928f896d754db68, type: 2}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {fileID: 0}
"@)

# Append all the 3D Machine objects from SampleScene
$null = $sb.Append($sceneObjectsPart)

# Write output scene
Set-Content -Path $targetScenePath -Value $sb.ToString() -Encoding UTF8
Write-Host "Successfully generated scene at: $targetScenePath"
