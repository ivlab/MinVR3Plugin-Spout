using UnityEditor;
using UnityEngine;
using IVLab.MinVR3;
using Klak.Spout;

#if MINVR3_HAS_VRPN_PLUGIN
using IVLab.MinVR3.VRPN;
#endif


// disable warnings about unused functions because these editor menu functions can look to the compiler
// as though they are never called
#pragma warning disable IDE0051

namespace IVLab.MinVR3.Spout
{
    public class Menu_GameObject_MinVR_Spout : MonoBehaviour
    {
        private const string VRConfigName = "VRConfig_UMNCave via Spout";

        // Tracking details
        private const string HeadVrpnDeviceName = "head";
        private const string MotiveServerIp = "10.0.50.203";
        private const string HeadPositionEventName = "Head/Position";
        private const string HeadRotationEventName = "Head/Rotation";

        // Spout will render to two textures, the first contains the imagery for all four walls
        // for the left eye, and the second contains all four walls for the right eye.
        private const string SpoutLeftEyeSourceName = "CaveWalls_LeftEye";
        private const string SpoutRightEyeSourceName = "CaveWalls_RightEye";

        // Size in pixels for each wall of the Cave
        private const int ScreenWidth = 1280;
        private const int ScreenHeight = 1280;

        // Intrapupillary distance for stereo rendering
        private const float IPD = 0.063f;  // 63mm for avg adult

        // CAVE Wall Geometry
        private static readonly string[] WallNames = 
        {
            "Left Wall",
            "Front Wall",
            "Right Wall",
            "Floor"
        };

        // UMN's CAVE is an 8x8x8 ft cube
        private const float HalfCaveWidth = 1.2192f;
        private const float HalfCaveHeight = 1.2192f;
        private const float HalfCaveDepth = 1.2192f;

        private static readonly Vector3[] WallCenterPts =
        {
            new Vector3(-HalfCaveWidth, 0, 0),
            new Vector3(0, 0, HalfCaveDepth),
            new Vector3(HalfCaveWidth, 0, 0),
            new Vector3(0, -HalfCaveHeight, 0)
        };

        private static readonly Vector3[] WallNormals =
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0)
        };

        private static readonly Vector3[] WallUpVectors =
        {
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1)
        };

        // Scale factors that need to be applied to a Unity Quad primitive to make it the same size
        // as the wall.  The scale is applied before the orientation, so the screen width should be
        // in the X scale factor, screen height in Y, and identity (1) in Z. 
        private static readonly Vector3[] WallScales =
        {
            new Vector3(2*HalfCaveDepth, 2*HalfCaveHeight, 1),
            new Vector3(2*HalfCaveWidth, 2*HalfCaveHeight, 1),
            new Vector3(2*HalfCaveDepth, 2*HalfCaveHeight, 1),
            new Vector3(2*HalfCaveWidth, 2*HalfCaveDepth, 1)
        };


        private static readonly Color[] WallColors =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan
        };




        [MenuItem("GameObject/MinVR/VRConfig/VRConfig_UMNCave via Spout", false, MenuHelpers.vrConfigSec2Priority)]
        public static void CreateVRConfigUMNCaveSpout(MenuCommand command)
        {
            GameObject vrEngineObj = MenuHelpers.CreateVREngineIfNeeded();
            GameObject roomSpaceObj = MenuHelpers.CreateRoomSpaceOriginIfNeeded();

            // If the menu command is relative to an object in the hierarchy, then make sure it is somewhere
            // under the MinVR Room Space Origin.
            GameObject parentObj = command.context as GameObject;
            if (parentObj == null) {
                parentObj = roomSpaceObj;
            } else if (!MenuHelpers.IsUnderRoomSpace(parentObj)) {
                Debug.LogWarning("For tracking (reported in RoomSpace coordinates) to work correctly, " +
                    "VRConfigs should always be placed under Room Space in the hierarchy. I will move " +
                    "the new VRConfig there for you.");
                parentObj = roomSpaceObj;
            }

            // VRCONFIG
            GameObject vrConfigObj = MenuHelpers.CreateAndPlaceGameObject(VRConfigName, parentObj, typeof(VRConfig));


            // VRCONFIG -> INPUT DEVICES

            GameObject inputDevObj = MenuHelpers.CreateAndPlaceGameObject("Input Devices", vrConfigObj);

            // A define must be added in the inspector for IVLab.MinVR3.Spout.Editor.asmdef for this
            // define to work. MINVR3_HAS_VRPN_PLUGIN should be defined if any version of the MinVR3
            // VRPN plugin is available, then this script can create the appropriate tracking devices.
#if MINVR3_HAS_VRPN_PLUGIN
            VRPNTracker trackerHead = MenuHelpers.CreateAndPlaceGameObject("VRPN Tracker 'head'", inputDevObj, typeof(VRPNTracker)).GetComponent<VRPNTracker>();

            // Motive tracking is right-handed y-up
            trackerHead.incomingCoordinateSystem = new CoordConversion.CoordSystem
            (
                CoordConversion.CoordSystem.Handedness.RightHanded,
                CoordConversion.CoordSystem.Axis.PosY,
                CoordConversion.CoordSystem.Axis.NegZ
            );
            trackerHead.minVR3PositionEventName = HeadPositionEventName;
            trackerHead.minVR3RotationEventName = HeadRotationEventName;
            trackerHead.vrpnDevice = HeadVrpnDeviceName;
            trackerHead.vrpnServer = MotiveServerIp;
#else
            Debug.LogWarning("MinVR3 VRPN plugin not found. Please install the plugin or select a different source for the perspective tracking events.");
#endif


            // VRCONFIG -> DISPLAY DEVICES

            GameObject displayDevObj = MenuHelpers.CreateAndPlaceGameObject("Display Devices", vrConfigObj);

            // Create/update Render Textures in Assets/ folder
            RenderTexture leftCompositeRT = new RenderTexture(new RenderTextureDescriptor(ScreenWidth * WallNames.Length, ScreenHeight));
            AssetDatabase.CreateAsset(leftCompositeRT, "Assets/CaveWalls_LeftEye_RT.asset");
            Debug.Log("Adding RenderTexture asset to the project: " + AssetDatabase.GetAssetPath(leftCompositeRT));

            RenderTexture rightCompositeRT = new RenderTexture(new RenderTextureDescriptor(ScreenWidth * WallNames.Length, ScreenHeight));
            AssetDatabase.CreateAsset(rightCompositeRT, "Assets/CaveWalls_RightEye_RT.asset");
            Debug.Log("Adding RenderTexture asset to the project: " + AssetDatabase.GetAssetPath(rightCompositeRT));


            // VRCONFIG -> DISPLAY DEVICES -> Spout Senders

            GameObject spoutSendersObj = MenuHelpers.CreateAndPlaceGameObject("Spout Senders", displayDevObj);

            GameObject leftEyeSenderObj = MenuHelpers.CreateAndPlaceGameObject("Left Eye Image", spoutSendersObj, typeof(Camera), typeof(SpoutSender));

            Camera leftCompositeCamera = leftEyeSenderObj.GetComponent<Camera>();
            leftCompositeCamera.targetTexture = leftCompositeRT;
            leftCompositeCamera.depth = 5;
            leftCompositeCamera.clearFlags = CameraClearFlags.Color;
            leftCompositeCamera.backgroundColor = Color.black;

            SpoutSender leftSpout = leftEyeSenderObj.GetComponent<SpoutSender>();
            leftSpout.spoutName = SpoutLeftEyeSourceName;
            leftSpout.captureMethod = CaptureMethod.Texture;
            leftSpout.sourceTexture = leftCompositeRT;


            GameObject rightEyeSenderObj = MenuHelpers.CreateAndPlaceGameObject("Right Eye Image", spoutSendersObj, typeof(Camera), typeof(SpoutSender));

            Camera rightCompositeCamera = rightEyeSenderObj.GetComponent<Camera>();
            rightCompositeCamera.targetTexture = rightCompositeRT;
            rightCompositeCamera.depth = 5;
            rightCompositeCamera.clearFlags = CameraClearFlags.Color;
            rightCompositeCamera.backgroundColor = Color.black;

            SpoutSender rightSpout = rightEyeSenderObj.GetComponent<SpoutSender>();
            rightSpout.spoutName = SpoutRightEyeSourceName;
            rightSpout.captureMethod = CaptureMethod.Texture;
            rightSpout.sourceTexture = rightCompositeRT;


            // VRCONFIG -> DISPLAY DEVICES -> Projection Screen Geometry
            GameObject projScreensObj = MenuHelpers.CreateAndPlaceGameObject("Projection Screen Geometry", displayDevObj);
            GameObject[] wallObjs = new GameObject[WallNames.Length];
            for (int i = 0; i < WallNames.Length; i++) {
                wallObjs[i] = MenuHelpers.CreateAndPlacePrimitive(WallNames[i], projScreensObj, PrimitiveType.Quad);
                wallObjs[i].transform.localPosition = WallCenterPts[i];
                wallObjs[i].transform.localRotation = Quaternion.LookRotation(-WallNormals[i], WallUpVectors[i]);
                wallObjs[i].transform.localScale = WallScales[i];
                wallObjs[i].GetComponent<Renderer>().material.color = WallColors[i];
            }
            // hide the geometry for the walls -- we don't generally want to draw them
            projScreensObj.SetActive(false);


            // VRCONFIG -> DISPLAY DEVICES -> Cave Camera Rig
            GameObject cameraRigObj = MenuHelpers.CreateAndPlaceGameObject("Cave Camera Rig", displayDevObj, typeof(TrackedPoseDriver),typeof(CameraRigProjectionSettings));
            TrackedPoseDriver tpd = cameraRigObj.GetComponent<TrackedPoseDriver>();
            tpd.positionEvent = VREventPrototypeVector3.Create(HeadPositionEventName);
            tpd.rotationEvent = VREventPrototypeQuaternion.Create(HeadRotationEventName);

            // Add two cameras (left and right eyes) for each wall
            float viewportWidth = 1.0f / (float)WallNames.Length;
            for (int i = 0; i < WallNames.Length; i++) {

                // Create/update Render Textures in Assets/ folder
                RenderTexture wallLeftRT = new RenderTexture(new RenderTextureDescriptor(ScreenWidth, ScreenHeight));
                AssetDatabase.CreateAsset(wallLeftRT, "Assets/" + WallNames[i] + " LeftEye RT.asset");
                Debug.Log("Adding RenderTexture asset to the project: " + AssetDatabase.GetAssetPath(wallLeftRT));

                GameObject leftEyeObj = MenuHelpers.CreateAndPlaceGameObject(
                    WallNames[i] + " Camera (Left Eye)",
                    cameraRigObj,
                    typeof(Camera),
                    typeof(ObliqueProjectionToQuad)
                );
                Camera leftCam = leftEyeObj.GetComponent<Camera>();
                //leftCam.rect = new Rect(viewportWidth * i, 0, viewportWidth, 1.0f);
                leftCam.targetTexture = wallLeftRT;
                leftCam.depth = 0;
                leftCam.clearFlags = CameraClearFlags.SolidColor;
                leftCam.backgroundColor = Color.black;
                ObliqueProjectionToQuad leftProj = leftEyeObj.GetComponent<ObliqueProjectionToQuad>();
                leftProj.applyStereoEyeOffset = true;
                leftProj.whichEye = ObliqueProjectionToQuad.Eye.LeftEye;
                leftProj.projectionScreenQuad = wallObjs[i];

                StampTextureOnScreen leftTexCopier = leftEyeSenderObj.AddComponent<StampTextureOnScreen>();
                leftTexCopier.stampTexture = wallLeftRT;
                leftTexCopier.topLeftCornerUV = new Vector2(viewportWidth * i, 1);
                leftTexCopier.botRightCornerUV = new Vector2(viewportWidth * (i + 1), 0); 


                // Create/update Render Textures in Assets/ folder
                RenderTexture wallRightRT = new RenderTexture(new RenderTextureDescriptor(ScreenWidth, ScreenHeight));
                AssetDatabase.CreateAsset(wallRightRT, "Assets/" + WallNames[i] + " RightEye RT.asset");
                Debug.Log("Adding RenderTexture asset to the project: " + AssetDatabase.GetAssetPath(wallLeftRT));

                GameObject rightEyeObj = MenuHelpers.CreateAndPlaceGameObject(
                    WallNames[i] + " Camera (Right Eye)",
                    cameraRigObj,
                    typeof(Camera),
                    typeof(ObliqueProjectionToQuad)
                );
                Camera rightCam = rightEyeObj.GetComponent<Camera>();
                //rightCam.rect = new Rect(viewportWidth * i, 0, viewportWidth, 1.0f);
                rightCam.targetTexture = wallRightRT;
                rightCam.depth = 0;
                rightCam.clearFlags = CameraClearFlags.SolidColor;
                rightCam.backgroundColor = Color.black;
                ObliqueProjectionToQuad rightProj = rightEyeObj.GetComponent<ObliqueProjectionToQuad>();
                rightProj.applyStereoEyeOffset = true;
                rightProj.whichEye = ObliqueProjectionToQuad.Eye.RightEye;
                rightProj.projectionScreenQuad = wallObjs[i];


                StampTextureOnScreen rightTexCopier = rightEyeSenderObj.AddComponent<StampTextureOnScreen>();
                rightTexCopier.stampTexture = wallRightRT;
                rightTexCopier.topLeftCornerUV = new Vector2(viewportWidth * i, 1);
                rightTexCopier.botRightCornerUV = new Vector2(viewportWidth * (i + 1), 0);
            }

            // VRCONFIG -> DISPLAY DEVICES -> Debug Camera
            GameObject debugCamObj = MenuHelpers.CreateAndPlaceGameObject("Debug Camera", displayDevObj, typeof(Camera));
            Camera debugCam = debugCamObj.GetComponent<Camera>();
            debugCam.clearFlags = CameraClearFlags.Color;
            debugCam.backgroundColor = Color.black;

            StampTextureOnScreen leftDebugTexCopier = debugCamObj.AddComponent<StampTextureOnScreen>();
            leftDebugTexCopier.stampTexture = leftCompositeRT;
            leftDebugTexCopier.topLeftCornerUV = new Vector2(0, 1);
            leftDebugTexCopier.botRightCornerUV = new Vector2(1, 0.5f);

            StampTextureOnScreen rightDebugTexCopier = debugCamObj.AddComponent<StampTextureOnScreen>();
            rightDebugTexCopier.stampTexture = rightCompositeRT;
            rightDebugTexCopier.topLeftCornerUV = new Vector2(0, 0.5f);
            rightDebugTexCopier.botRightCornerUV = new Vector2(1, 0);

            debugCamObj.SetActive(false);
        }

    } // end class

} // end namespace
