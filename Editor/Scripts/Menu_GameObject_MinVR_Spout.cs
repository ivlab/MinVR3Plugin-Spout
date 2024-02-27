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
        private const string MotiveServerIp = "127.0.0.1";

        private const string HeadVrpnDeviceName = "head";
        private const string HeadPositionEventName = "Head/Position";
        private const string HeadRotationEventName = "Head/Rotation";

        private const string WandVrpnDeviceName = "wand";
        private const string WandPositionEventName = "Wand/Position";
        private const string WandRotationEventName = "Wand/Rotation";

        private const string PenVrpnDeviceName = "pen";
        private const string PenPositionEventName = "Pen/Position";
        private const string PenRotationEventName = "Pen/Rotation";

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


            // VRPN Head Tracker
            GameObject headObj = MenuHelpers.CreateAndPlaceGameObject(
                "VRPN Tracker '" + HeadVrpnDeviceName + "'",
                inputDevObj,
                typeof(VRPNTracker),
                typeof(TrackedHeadPoseDriver)
            );

            VRPNTracker headVrpn = headObj.GetComponent<VRPNTracker>();
            headVrpn.incomingCoordinateSystem = new CoordConversion.CoordSystem
            (   // Motive tracking is right-handed y-up
                CoordConversion.CoordSystem.Handedness.RightHanded,
                CoordConversion.CoordSystem.Axis.PosY,
                CoordConversion.CoordSystem.Axis.NegZ
            );
            headVrpn.minVR3PositionEventName = HeadPositionEventName;
            headVrpn.minVR3RotationEventName = HeadRotationEventName;
            headVrpn.vrpnDevice = HeadVrpnDeviceName;
            headVrpn.vrpnServer = MotiveServerIp;

            TrackedHeadPoseDriver headPoseDriver = headObj.GetComponent<TrackedHeadPoseDriver>();
            headPoseDriver.positionEvent = VREventPrototypeVector3.Create(HeadPositionEventName);
            headPoseDriver.rotationEvent = VREventPrototypeQuaternion.Create(HeadRotationEventName);

            // VRPN Wand Tracker
            GameObject wandObj = MenuHelpers.CreateAndPlaceGameObject(
                "VRPN Tracker '" + WandVrpnDeviceName + "'",
                inputDevObj,
                typeof(VRPNTracker),
                typeof(TrackedPoseDriver)
            );

            VRPNTracker wandVrpn = wandObj.GetComponent<VRPNTracker>();
            wandVrpn.incomingCoordinateSystem = new CoordConversion.CoordSystem
            (   // Motive tracking is right-handed y-up
                CoordConversion.CoordSystem.Handedness.RightHanded,
                CoordConversion.CoordSystem.Axis.PosY,
                CoordConversion.CoordSystem.Axis.NegZ
            );
            wandVrpn.minVR3PositionEventName = WandPositionEventName;
            wandVrpn.minVR3RotationEventName = WandRotationEventName;
            wandVrpn.vrpnDevice = WandVrpnDeviceName;
            wandVrpn.vrpnServer = MotiveServerIp;

            TrackedPoseDriver wandPoseDriver = wandObj.GetComponent<TrackedPoseDriver>();
            wandPoseDriver.positionEvent = VREventPrototypeVector3.Create(WandPositionEventName);
            wandPoseDriver.rotationEvent = VREventPrototypeQuaternion.Create(WandRotationEventName);

            // VRPN Pen Tracker
            GameObject penObj = MenuHelpers.CreateAndPlaceGameObject(
                "VRPN Tracker '" + PenVrpnDeviceName + "'",
                inputDevObj,
                typeof(VRPNTracker),
                typeof(TrackedPoseDriver)
            );

            VRPNTracker penVrpn = penObj.GetComponent<VRPNTracker>();
            penVrpn.incomingCoordinateSystem = new CoordConversion.CoordSystem
            (   // Motive tracking is right-handed y-up
                CoordConversion.CoordSystem.Handedness.RightHanded,
                CoordConversion.CoordSystem.Axis.PosY,
                CoordConversion.CoordSystem.Axis.NegZ
            );
            penVrpn.minVR3PositionEventName = PenPositionEventName;
            penVrpn.minVR3RotationEventName = PenRotationEventName;
            penVrpn.vrpnDevice = PenVrpnDeviceName;
            penVrpn.vrpnServer = MotiveServerIp;

            TrackedPoseDriver penPoseDriver = penObj.GetComponent<TrackedPoseDriver>();
            penPoseDriver.positionEvent = VREventPrototypeVector3.Create(PenPositionEventName);
            penPoseDriver.rotationEvent = VREventPrototypeQuaternion.Create(PenRotationEventName);

            // TODO: Add VRPN Button devices


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
            leftCompositeCamera.depth = 1;
            leftCompositeCamera.clearFlags = CameraClearFlags.Color;
            leftCompositeCamera.backgroundColor = Color.black;

            SpoutSender leftSpoutSender = leftEyeSenderObj.GetComponent<SpoutSender>();
            leftSpoutSender.spoutName = SpoutLeftEyeSourceName;
            leftSpoutSender.captureMethod = CaptureMethod.Texture;
            leftSpoutSender.sourceTexture = leftCompositeRT;

            // note: one StampTextureOnScreen per wall will also be added later when iterating through the walls


            GameObject rightEyeSenderObj = MenuHelpers.CreateAndPlaceGameObject("Right Eye Image", spoutSendersObj, typeof(Camera), typeof(SpoutSender));

            Camera rightCompositeCamera = rightEyeSenderObj.GetComponent<Camera>();
            rightCompositeCamera.targetTexture = rightCompositeRT;
            rightCompositeCamera.depth = 5;
            rightCompositeCamera.clearFlags = CameraClearFlags.Color;
            rightCompositeCamera.backgroundColor = Color.black;

            SpoutSender rightSpoutSender = rightEyeSenderObj.GetComponent<SpoutSender>();
            rightSpoutSender.spoutName = SpoutRightEyeSourceName;
            rightSpoutSender.captureMethod = CaptureMethod.Texture;
            rightSpoutSender.sourceTexture = rightCompositeRT;

            // note: one StampTextureOnScreen per wall will also be added later when iterating through the walls



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



            // VRCONFIG -> DISPLAY DEVICES -> Cave Camera Rig
            GameObject cameraRigObj = MenuHelpers.CreateAndPlaceGameObject("Cave Camera Rig", displayDevObj, typeof(CameraRigSettings));


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
                leftProj.projectionScreenQuad = wallObjs[i];
                leftProj.trackedHeadPoseDriver = headPoseDriver;
                leftProj.whichEye = ObliqueProjectionToQuad.Eye.LeftEye;

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
                rightProj.projectionScreenQuad = wallObjs[i];
                rightProj.trackedHeadPoseDriver = headPoseDriver;
                rightProj.whichEye = ObliqueProjectionToQuad.Eye.RightEye;

                StampTextureOnScreen rightTexCopier = rightEyeSenderObj.AddComponent<StampTextureOnScreen>();
                rightTexCopier.stampTexture = wallRightRT;
                rightTexCopier.topLeftCornerUV = new Vector2(viewportWidth * i, 1);
                rightTexCopier.botRightCornerUV = new Vector2(viewportWidth * (i + 1), 0);
            }

            // VRCONFIG -> DISPLAY DEVICES -> Game Window Camera
            GameObject gameWinCamObj = MenuHelpers.CreateAndPlaceGameObject("Game Window Camera", displayDevObj, typeof(Camera), typeof(DrawFPS));
            Camera gameWinCam = gameWinCamObj.GetComponent<Camera>();
            gameWinCam.clearFlags = CameraClearFlags.Color;
            gameWinCam.backgroundColor = Color.black;
            gameWinCam.cullingMask = 0;

            StampTextureOnScreen leftDebugTexCopier = gameWinCamObj.AddComponent<StampTextureOnScreen>();
            leftDebugTexCopier.stampTexture = leftCompositeRT;
            leftDebugTexCopier.topLeftCornerUV = new Vector2(0, 1);
            leftDebugTexCopier.botRightCornerUV = new Vector2(1, 0.5f);

            StampTextureOnScreen rightDebugTexCopier = gameWinCamObj.AddComponent<StampTextureOnScreen>();
            rightDebugTexCopier.stampTexture = rightCompositeRT;
            rightDebugTexCopier.topLeftCornerUV = new Vector2(0, 0.5f);
            rightDebugTexCopier.botRightCornerUV = new Vector2(1, 0);

            DrawSpoutSenderInfo leftInfo = gameWinCamObj.AddComponent<DrawSpoutSenderInfo>();
            leftInfo.sender = leftSpoutSender;
            leftInfo.m_FontSize = 24;
            leftInfo.m_TextAnchor = TextAnchor.UpperLeft;
            leftInfo.m_Position = new Rect(0.01f, 0.03f, 1, 0.1f);

            DrawSpoutSenderInfo rightInfo = gameWinCamObj.AddComponent<DrawSpoutSenderInfo>();
            rightInfo.sender = rightSpoutSender;
            rightInfo.m_FontSize = 24;
            rightInfo.m_TextAnchor = TextAnchor.UpperLeft;
            rightInfo.m_Position = new Rect(0.01f, 0.53f, 1, 0.1f);


            // VRCONFIG->Cave Debug Graphics
            GameObject debugGfxObj = MenuHelpers.CreateAndPlaceGameObject("Cave Debug Graphics", vrConfigObj);

            // VRCONFIG->Cave Debug Graphics->Eyes Projected on Walls
            GameObject eyesOnWallsObj = MenuHelpers.CreateAndPlaceGameObject("Eyes Projected on Walls", debugGfxObj);

            for (int i = 0; i < wallObjs.Length; i++) {
                DrawEyes drawEyesForWall = eyesOnWallsObj.AddComponent<DrawEyes>();
                drawEyesForWall.headPoseDriver = headPoseDriver;
                drawEyesForWall.projectionScreenQuad = wallObjs[i];
            }

            // VRCONFIG->Cave Debug Graphics->Tracker Axes
            GameObject trackerAxesObj = MenuHelpers.CreateAndPlaceGameObject("Tracker Axes", debugGfxObj, typeof(DrawTrackers));
            DrawTrackers drawTrackers = trackerAxesObj.GetComponent<DrawTrackers>();

            DrawTrackers.TrackerDescription wandDesc = new DrawTrackers.TrackerDescription();
            wandDesc.displayName = "Wand";
            wandDesc.positionEvent = VREventPrototypeVector3.Create(WandPositionEventName);
            wandDesc.rotationEvent = VREventPrototypeQuaternion.Create(WandRotationEventName);
            drawTrackers.trackers.Add(wandDesc);

            DrawTrackers.TrackerDescription penDesc = new DrawTrackers.TrackerDescription();
            penDesc.displayName = "Pen";
            penDesc.positionEvent = VREventPrototypeVector3.Create(PenPositionEventName);
            penDesc.rotationEvent = VREventPrototypeQuaternion.Create(PenRotationEventName);
            drawTrackers.trackers.Add(penDesc);
        }


    } // end class

} // end namespace
