using UnityEditor;
using UnityEngine;
using IVLab.MinVR3;
using Klak.Spout;




// A define must be added in the inspector for IVLab.MinVR3.Spout.Editor.asmdef for this
// define to work. MINVR3_HAS_VRPN_PLUGIN should be defined if any version of the MinVR3
// VRPN plugin is available, then this script can create the appropriate tracking devices.
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

        private const string Stick1VrpnDeviceName = "stick1";
        private const string Stick1PositionEventName = "Stick1/Position";
        private const string Stick1RotationEventName = "Stick1/Rotation";

        private const string Stick2VrpnDeviceName = "stick2";
        private const string Stick2PositionEventName = "Stick2/Position";
        private const string Stick2RotationEventName = "Stick2/Rotation";

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




        [MenuItem("GameObject/MinVR/VRConfig/VRConfig_UMNCave", false, MenuHelpers.vrConfigSec2Priority)]
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

            // ROOT
            GameObject caveRootObj = MenuHelpers.CreateAndPlaceGameObject("UMN Cave", parentObj, 
                typeof(VRConfigMask), typeof(QuitOnEscapeKey));
            VRConfigMask caveRootConfigMask = caveRootObj.GetComponent<VRConfigMask>();


            // ROOT -> INPUT DEVICES (SINGLE PROCESS & CLUSTER SERVER)

            GameObject serverOnlyInputDevObj = 
                MenuHelpers.CreateAndPlaceGameObject("Input Devices (Single Process and Cluster Server Only)", 
                caveRootObj, typeof(VRConfigMask));
            VRConfigMask serverOnlyDevicesConfigMask = serverOnlyInputDevObj.GetComponent<VRConfigMask>();


#if MINVR3_HAS_VRPN_PLUGIN

            var optiTrackCoordSystem = new CoordConversion.CoordSystem
            (   
                CoordConversion.CoordSystem.Handedness.RightHanded,
                CoordConversion.CoordSystem.Axis.PosY,
                CoordConversion.CoordSystem.Axis.NegZ
            );

            // VRPN Head Tracker
            GameObject headVrpnObj = MenuHelpers.CreateAndPlaceGameObject(
                "VRPN Tracker '" + HeadVrpnDeviceName + "'",
                serverOnlyInputDevObj,
                typeof(VRPNTracker)
            );
            VRPNTracker headVrpn = headVrpnObj.GetComponent<VRPNTracker>();
            headVrpn.incomingCoordinateSystem = optiTrackCoordSystem;
            headVrpn.minVR3PositionEventName = HeadPositionEventName;
            headVrpn.minVR3RotationEventName = HeadRotationEventName;
            headVrpn.vrpnDevice = HeadVrpnDeviceName;
            headVrpn.vrpnServer = MotiveServerIp;

            // VRPN Wand Tracker
            GameObject wandVrpnObj = MenuHelpers.CreateAndPlaceGameObject(
                "VRPN Tracker '" + WandVrpnDeviceName + "'",
                serverOnlyInputDevObj,
                typeof(VRPNTracker)
            );
            VRPNTracker wandVrpn = wandVrpnObj.GetComponent<VRPNTracker>();
            wandVrpn.incomingCoordinateSystem = optiTrackCoordSystem;
            wandVrpn.minVR3PositionEventName = WandPositionEventName;
            wandVrpn.minVR3RotationEventName = WandRotationEventName;
            wandVrpn.vrpnDevice = WandVrpnDeviceName;
            wandVrpn.vrpnServer = MotiveServerIp;

            // VRPN Pen Tracker
            GameObject penVrpnObj = MenuHelpers.CreateAndPlaceGameObject(
                "VRPN Tracker '" + PenVrpnDeviceName + "'",
                serverOnlyInputDevObj,
                typeof(VRPNTracker)
            );
            VRPNTracker penVrpn = penVrpnObj.GetComponent<VRPNTracker>();
            penVrpn.incomingCoordinateSystem = optiTrackCoordSystem;
            penVrpn.minVR3PositionEventName = PenPositionEventName;
            penVrpn.minVR3RotationEventName = PenRotationEventName;
            penVrpn.vrpnDevice = PenVrpnDeviceName;
            penVrpn.vrpnServer = MotiveServerIp;

            // VRPN Stick1 Tracker
            GameObject stick1VrpnObj = MenuHelpers.CreateAndPlaceGameObject(
                "VRPN Tracker '" + Stick1VrpnDeviceName + "'",
                serverOnlyInputDevObj,
                typeof(VRPNTracker)
            );
            VRPNTracker stick1Vrpn = stick1VrpnObj.GetComponent<VRPNTracker>();
            stick1Vrpn.incomingCoordinateSystem = optiTrackCoordSystem;
            stick1Vrpn.minVR3PositionEventName = Stick1PositionEventName;
            stick1Vrpn.minVR3RotationEventName = Stick1RotationEventName;
            stick1Vrpn.vrpnDevice = Stick1VrpnDeviceName;
            stick1Vrpn.vrpnServer = MotiveServerIp;

            // VRPN Stick2 Tracker
            GameObject stick2VrpnObj = MenuHelpers.CreateAndPlaceGameObject(
                "VRPN Tracker '" + Stick2VrpnDeviceName + "'",
                serverOnlyInputDevObj,
                typeof(VRPNTracker)
            );
            VRPNTracker stick2Vrpn = stick2VrpnObj.GetComponent<VRPNTracker>();
            stick2Vrpn.incomingCoordinateSystem = optiTrackCoordSystem;
            stick2Vrpn.minVR3PositionEventName = Stick2PositionEventName;
            stick2Vrpn.minVR3RotationEventName = Stick2RotationEventName;
            stick2Vrpn.vrpnDevice = Stick2VrpnDeviceName;
            stick2Vrpn.vrpnServer = MotiveServerIp;

            // TODO: Add VRPN Button devices


#else
            Debug.LogWarning("MinVR3 VRPN plugin not found. Please install the plugin or select a different source for the perspective tracking events.");
#endif
            GameObject spoutMouseKbdObj = MenuHelpers.CreateAndPlaceGameObject(
                "Spout Server Mouse & Keyboard", serverOnlyInputDevObj, 
                typeof(TcpJsonVREventConnection), typeof(VREventConnectionReceiver));
            TcpJsonVREventConnection spoutVREventConnection =
                spoutMouseKbdObj.GetComponent<TcpJsonVREventConnection>();
            spoutVREventConnection.connectToServer = true;
            spoutVREventConnection.connectToServerIP = "127.0.0.1";
            spoutVREventConnection.connectToServerPort = 9030;


            // ROOT -> INPUT DEVICES (ALL)

            GameObject allInputDevObj = MenuHelpers.CreateAndPlaceGameObject("Input Devices (All)", 
                caveRootObj);

            // the head pose obj is required because the obliqueprojection scripts needs to know about the head
            GameObject headPoseObj = MenuHelpers.CreateAndPlaceGameObject(
                "Tracked Head", allInputDevObj, typeof(TrackedHeadPoseDriver));
            TrackedHeadPoseDriver headPoseDriver = headPoseObj.GetComponent<TrackedHeadPoseDriver>();
            headPoseDriver.positionEvent = VREventPrototypeVector3.Create(HeadPositionEventName);
            headPoseDriver.rotationEvent = VREventPrototypeQuaternion.Create(HeadRotationEventName);

            // these other tracked objects are not actually required, but it might help people to have them
            GameObject wandPoseObj = MenuHelpers.CreateAndPlaceGameObject(
                "Tracked Wand", allInputDevObj, typeof(TrackedPoseDriver)); 
            TrackedPoseDriver wandPoseDriver = wandPoseObj.GetComponent<TrackedPoseDriver>();
            wandPoseDriver.positionEvent = VREventPrototypeVector3.Create(WandPositionEventName);
            wandPoseDriver.rotationEvent = VREventPrototypeQuaternion.Create(WandRotationEventName);

            GameObject penPoseObj = MenuHelpers.CreateAndPlaceGameObject(
                "Tracked Pen", allInputDevObj, typeof(TrackedPoseDriver));
            TrackedPoseDriver penPoseDriver = penPoseObj.GetComponent<TrackedPoseDriver>();
            penPoseDriver.positionEvent = VREventPrototypeVector3.Create(PenPositionEventName);
            penPoseDriver.rotationEvent = VREventPrototypeQuaternion.Create(PenRotationEventName);

            GameObject stick1PoseObj = MenuHelpers.CreateAndPlaceGameObject(
                "Tracked Stick1", allInputDevObj, typeof(TrackedPoseDriver));
            TrackedPoseDriver stick1PoseDriver = stick1PoseObj.GetComponent<TrackedPoseDriver>();
            stick1PoseDriver.positionEvent = VREventPrototypeVector3.Create(Stick1PositionEventName);
            stick1PoseDriver.rotationEvent = VREventPrototypeQuaternion.Create(Stick1RotationEventName);

            GameObject stick2PoseObj = MenuHelpers.CreateAndPlaceGameObject(
                "Tracked Stick2", allInputDevObj, typeof(TrackedPoseDriver));
            TrackedPoseDriver stick2PoseDriver = stick2PoseObj.GetComponent<TrackedPoseDriver>();
            stick2PoseDriver.positionEvent = VREventPrototypeVector3.Create(Stick2PositionEventName);
            stick2PoseDriver.rotationEvent = VREventPrototypeQuaternion.Create(Stick2RotationEventName);

            GameObject mouseKbdObj = MenuHelpers.CreateAndPlaceGameObject(
                "Mouse & Keyboard", allInputDevObj, typeof(MouseAndKeyboard));


            // ROOT -> PROJECTION SCREEN GEOMETRY
            
            GameObject projScreensObj = MenuHelpers.CreateAndPlaceGameObject("Projection Screen Geometry", caveRootObj);
            GameObject[] wallObjs = new GameObject[WallNames.Length];
            for (int i = 0; i < WallNames.Length; i++) {
                wallObjs[i] = MenuHelpers.CreateAndPlacePrimitive(WallNames[i], projScreensObj, PrimitiveType.Quad);
                wallObjs[i].transform.localPosition = WallCenterPts[i];
                wallObjs[i].transform.localRotation = Quaternion.LookRotation(-WallNormals[i], WallUpVectors[i]);
                wallObjs[i].transform.localScale = WallScales[i];
                wallObjs[i].GetComponent<Renderer>().material.color = WallColors[i];
            }
            projScreensObj.SetActive(false);


            // CREATE RENDER TEXTURE ASSETS FOR USE WITH THE CAMERAS FOR EACH WALL
            // Add (or update if they already exist) Render Texture assets to the project in the Assets/ folder
            RenderTexture[] leftRenderTextures = new RenderTexture[WallNames.Length];
            RenderTexture[] rightRenderTextures = new RenderTexture[WallNames.Length];
            for (int i = 0; i < WallNames.Length; i++)
            {
                RenderTexture wallLeftRT = new RenderTexture(new RenderTextureDescriptor(ScreenWidth, ScreenHeight));
                AssetDatabase.CreateAsset(wallLeftRT, "Assets/" + WallNames[i] + " LeftEye RT.asset");
                Debug.Log("Added RenderTexture asset to the project: " + AssetDatabase.GetAssetPath(wallLeftRT));
                leftRenderTextures[i] = wallLeftRT;

                RenderTexture wallRightRT = new RenderTexture(new RenderTextureDescriptor(ScreenWidth, ScreenHeight));
                AssetDatabase.CreateAsset(wallRightRT, "Assets/" + WallNames[i] + " RightEye RT.asset");
                Debug.Log("Added RenderTexture asset to the project: " + AssetDatabase.GetAssetPath(wallRightRT));
                rightRenderTextures[i] = wallRightRT;
            }

            // ROOT -> VRCONFIGS
            GameObject vrConfigsObj = MenuHelpers.CreateAndPlaceGameObject("VRConfigs", caveRootObj, typeof(CameraRigSettings));
            
            // VRCONFIG FOR SINGLE PROCESS MODE
            string msg1 = "--SINGLEPROCESS RUNS IN EDITOR OR BUILD BUT SLOWLY--";
            MenuHelpers.CreateAndPlaceGameObject(msg1, vrConfigsObj);

            GameObject singleProcObj = MenuHelpers.CreateAndPlaceGameObject("VRConfig_UMNCave_SingleProcess", 
                vrConfigsObj, typeof(VRConfig), typeof(WindowSettings));
            caveRootConfigMask.AddEnabledConfig(singleProcObj.GetComponent<VRConfig>());
            serverOnlyDevicesConfigMask.AddEnabledConfig(singleProcObj.GetComponent<VRConfig>());

            WindowSettings singleProcWinSettings = singleProcObj.GetComponent<WindowSettings>();
            singleProcWinSettings.windowTitle = "UMN Cave (4 Walls; Single Process)";
            // the small game window will be 4 walls wide and show the left eye
            // images for each wall in the top row and the right eye images in
            // the bottom row.
            singleProcWinSettings.width = 800;  // 4 walls wide
            singleProcWinSettings.height = 400; // 2 eyes tall
            singleProcWinSettings.fullScreenMode = FullScreenMode.Windowed;
            singleProcWinSettings.applyConfigTiming = WindowSettings.ApplyConfigTiming.Start;


            GameObject singleProcCameraRigObj = MenuHelpers.CreateAndPlaceGameObject("Camera Rig", singleProcObj, typeof(CameraRigSettings));
            GameObject singleProcGameWinObj = MenuHelpers.CreateAndPlaceGameObject("Game Window Camera", singleProcObj, typeof(Camera), typeof(DrawFPS));
            Camera singleProcGameWinCam = singleProcGameWinObj.GetComponent<Camera>();
            singleProcGameWinCam.depth = 1;
            singleProcGameWinCam.clearFlags = CameraClearFlags.Color;
            singleProcGameWinCam.backgroundColor = Color.black;
            singleProcGameWinCam.cullingMask = 0;


            // Add two cameras (left and right eyes) for each wall
            float viewportWidth = 1.0f / (float)WallNames.Length;
            for (int i = 0; i < WallNames.Length; i++) {
                GameObject leftEyeObj = MenuHelpers.CreateAndPlaceGameObject(
                    WallNames[i] + " Camera (Left Eye)",
                    singleProcCameraRigObj,
                    typeof(Camera),
                    typeof(ObliqueProjectionToQuad)
                );
                Camera leftCam = leftEyeObj.GetComponent<Camera>();
                //leftCam.rect = new Rect(viewportWidth * i, 0, viewportWidth, 1.0f);
                leftCam.targetTexture = leftRenderTextures[i];
                leftCam.depth = 0;
                ObliqueProjectionToQuad leftProj = leftEyeObj.GetComponent<ObliqueProjectionToQuad>();
                leftProj.projectionScreenQuad = wallObjs[i];
                leftProj.trackedHeadPoseDriver = headPoseDriver;
                leftProj.whichEye = ObliqueProjectionToQuad.Eye.LeftEye;

                StampTextureOnScreen leftTexCopier = singleProcGameWinObj.AddComponent<StampTextureOnScreen>();
                leftTexCopier.stampTexture = leftRenderTextures[i];
                leftTexCopier.topLeftCornerUV = new Vector2(viewportWidth * i, 1);
                leftTexCopier.botRightCornerUV = new Vector2(viewportWidth * (i + 1), 0.5f);

                SpoutSender leftSpoutSender = leftEyeObj.AddComponent<SpoutSender>();
                leftSpoutSender.spoutName = WallNames[i].Replace(" ", "") + "_LeftEye";
                leftSpoutSender.captureMethod = CaptureMethod.Texture;
                leftSpoutSender.sourceTexture = leftRenderTextures[i];

                if (i == 0)
                {
                    leftEyeObj.tag = "MainCamera";
                }


                GameObject rightEyeObj = MenuHelpers.CreateAndPlaceGameObject(
                    WallNames[i] + " Camera (Right Eye)",
                    singleProcCameraRigObj,
                    typeof(Camera),
                    typeof(ObliqueProjectionToQuad)
                );
                Camera rightCam = rightEyeObj.GetComponent<Camera>();
                //rightCam.rect = new Rect(viewportWidth * i, 0, viewportWidth, 1.0f);
                rightCam.targetTexture = rightRenderTextures[i];
                rightCam.depth = 0;
                ObliqueProjectionToQuad rightProj = rightEyeObj.GetComponent<ObliqueProjectionToQuad>();
                rightProj.projectionScreenQuad = wallObjs[i];
                rightProj.trackedHeadPoseDriver = headPoseDriver;
                rightProj.whichEye = ObliqueProjectionToQuad.Eye.RightEye;

                StampTextureOnScreen rightTexCopier = singleProcGameWinObj.AddComponent<StampTextureOnScreen>();
                rightTexCopier.stampTexture = rightRenderTextures[i];
                rightTexCopier.topLeftCornerUV = new Vector2(viewportWidth * i, 0.5f);
                rightTexCopier.botRightCornerUV = new Vector2(viewportWidth * (i + 1), 0);

                SpoutSender rightSpoutSender = rightEyeObj.AddComponent<SpoutSender>();
                rightSpoutSender.spoutName = WallNames[i].Replace(" ", "") + "_RightEye";
                rightSpoutSender.captureMethod = CaptureMethod.Texture;
                rightSpoutSender.sourceTexture = rightRenderTextures[i];
            }


            // VRCONFIGS FOR CLUSTER MODE
            string msg2 = "--CLUSTER RUNS ONLY FROM A BUILD; USE RUNCAVE.BAT TO START ALL PROCESSES--";
            MenuHelpers.CreateAndPlaceGameObject(msg2, vrConfigsObj);

            // Create 1 VRConfig per wall
            for (int i = 0; i < WallNames.Length; i++)
            {
                string vrConfigName;
                if (i == 0)
                {
                    vrConfigName = "VRConfig_UMNCave_Server_" + WallNames[i].Replace(" ", "");
                } else
                {
                    vrConfigName = "VRConfig_UMNCave_Client_" + WallNames[i].Replace(" ", "");
                }
                GameObject wallVRConfigObj = MenuHelpers.CreateAndPlaceGameObject(vrConfigName, vrConfigsObj, typeof(VRConfig), typeof(WindowSettings));
                caveRootConfigMask.AddEnabledConfig(wallVRConfigObj.GetComponent<VRConfig>());
                if (i == 0) {
                    ClusterServer server = wallVRConfigObj.AddComponent<ClusterServer>();
                    server.numClients = 3;
                    server.secondsToWaitForClientsToConnect = 60;
                    serverOnlyDevicesConfigMask.AddEnabledConfig(wallVRConfigObj.GetComponent<VRConfig>());
                } else {
                    ClusterClient client = wallVRConfigObj.AddComponent<ClusterClient>();
                    client.secondsToWaitTryingToConnectToServer = 30;
                }

                WindowSettings wallWinSettings = wallVRConfigObj.GetComponent<WindowSettings>();
                wallWinSettings.windowTitle = WallNames[i].Replace(" ", "") + "_LeftEye | " + WallNames[i].Replace(" ", "") + "_RightEye";
                wallWinSettings.upperLeftX = i * ScreenWidth + 100;
                wallWinSettings.upperLeftY = 100;
                wallWinSettings.width = 400;  // 2 eyes wide
                wallWinSettings.height = 200;
                wallWinSettings.fullScreenMode = FullScreenMode.Windowed;
                wallWinSettings.applyConfigTiming = WindowSettings.ApplyConfigTiming.Start;

                GameObject cameraRigObj = MenuHelpers.CreateAndPlaceGameObject("Camera Rig", wallVRConfigObj, typeof(CameraRigSettings));
                GameObject gameWinObj = MenuHelpers.CreateAndPlaceGameObject("Game Window Camera", wallVRConfigObj, typeof(Camera), typeof(DrawFPS));
                Camera gameWinCam = gameWinObj.GetComponent<Camera>();
                gameWinCam.depth = 1;
                gameWinCam.clearFlags = CameraClearFlags.Color;
                gameWinCam.backgroundColor = Color.black;
                gameWinCam.cullingMask = 0;

                GameObject leftEyeObj = MenuHelpers.CreateAndPlaceGameObject(
                    WallNames[i] + " Camera (Left Eye)",
                    cameraRigObj,
                    typeof(Camera),
                    typeof(ObliqueProjectionToQuad)
                );
                leftEyeObj.tag = "MainCamera";
                Camera leftCam = leftEyeObj.GetComponent<Camera>();
                //leftCam.rect = new Rect(viewportWidth * i, 0, viewportWidth, 1.0f);
                leftCam.targetTexture = leftRenderTextures[i];
                leftCam.depth = 0;
                ObliqueProjectionToQuad leftProj = leftEyeObj.GetComponent<ObliqueProjectionToQuad>();
                leftProj.projectionScreenQuad = wallObjs[i];
                leftProj.trackedHeadPoseDriver = headPoseDriver;
                leftProj.whichEye = ObliqueProjectionToQuad.Eye.LeftEye;

                StampTextureOnScreen leftTexCopier = gameWinObj.AddComponent<StampTextureOnScreen>();
                leftTexCopier.stampTexture = leftRenderTextures[i];
                leftTexCopier.topLeftCornerUV = new Vector2(0, 1);
                leftTexCopier.botRightCornerUV = new Vector2(0.5f, 0);

                SpoutSender leftSpoutSender = leftEyeObj.AddComponent<SpoutSender>();
                leftSpoutSender.spoutName = WallNames[i].Replace(" ", "") + "_LeftEye";
                leftSpoutSender.captureMethod = CaptureMethod.Texture;
                leftSpoutSender.sourceTexture = leftRenderTextures[i];


                GameObject rightEyeObj = MenuHelpers.CreateAndPlaceGameObject(
                    WallNames[i] + " Camera (Right Eye)",
                    cameraRigObj,
                    typeof(Camera),
                    typeof(ObliqueProjectionToQuad)
                );
                Camera rightCam = rightEyeObj.GetComponent<Camera>();
                //rightCam.rect = new Rect(viewportWidth * i, 0, viewportWidth, 1.0f);
                rightCam.targetTexture = rightRenderTextures[i];
                rightCam.depth = 0;
                ObliqueProjectionToQuad rightProj = rightEyeObj.GetComponent<ObliqueProjectionToQuad>();
                rightProj.projectionScreenQuad = wallObjs[i];
                rightProj.trackedHeadPoseDriver = headPoseDriver;
                rightProj.whichEye = ObliqueProjectionToQuad.Eye.RightEye;

                StampTextureOnScreen rightTexCopier = gameWinObj.AddComponent<StampTextureOnScreen>();
                rightTexCopier.stampTexture = rightRenderTextures[i];
                rightTexCopier.topLeftCornerUV = new Vector2(0.5f, 1);
                rightTexCopier.botRightCornerUV = new Vector2(1, 0);

                SpoutSender rightSpoutSender = rightEyeObj.AddComponent<SpoutSender>();
                rightSpoutSender.spoutName = WallNames[i].Replace(" ", "") + "_RightEye";
                rightSpoutSender.captureMethod = CaptureMethod.Texture;
                rightSpoutSender.sourceTexture = rightRenderTextures[i];

                // attach an editor-only gameobject to the server to create the run script
                // as part of the unity build process
                if (i == 0)
                {
                    GameObject runScriptObj = MenuHelpers.CreateAndPlaceGameObject("Generate RunCave.bat (Editor Only)", wallVRConfigObj, typeof(CreateTextFileOnPostBuild));
                    runScriptObj.tag = "EditorOnly";
                    CreateTextFileOnPostBuild fileWriter = runScriptObj.GetComponent<CreateTextFileOnPostBuild>();
                    var settings = new CreateTextFileOnPostBuild.CreateTextFileOnPostBuildSettings();
                    settings.fileName = "RunCave.bat";
                    settings.copyLocation = CreateTextFileOnPostBuild.PostBuildCopyLocation.BuildFolder;
                    string exeName = Application.productName + ".exe";
                    settings.fileText =
                        $"@REM Server \n" +
                        "START {exeName} -vrconfig \"VRConfig_UMNCave_Server_LeftWall\" -logFile .\\umncave-server-leftwall.log \n" +
                        "\n" +
                        "@REM Three Clients \n" +
                        "TIMEOUT /t 6 \n" +
                        "START {exeName} -vrconfig \"VRConfig_UMNCave_Client_FrontWall\" -logFile .\\umncave-client-frontwall.log \n" +
                        "TIMEOUT /t 3 \n" +
                        "START {exeName} -vrconfig \"VRConfig_UMNCave_Client_RightWall\" -logFile .\\umncave-client-rightwall.log \n" +
                        "TIMEOUT /t 3 \n" +
                        "START {exeName} -vrconfig \"VRConfig_UMNCave_Client_Floor\" -logFile .\\umncave-client-floor.log \n";
                    fileWriter.settings = settings;
                }
            }

            // VRCONFIG->Cave Debug Graphics
            GameObject debugGfxObj = MenuHelpers.CreateAndPlaceGameObject("Cave Debug Graphics", caveRootObj);
            debugGfxObj.SetActive(false);

            // VRCONFIG->Cave Debug Graphics->Eyes Projected on Walls
            GameObject eyesOnWallsObj = MenuHelpers.CreateAndPlaceGameObject("Eyes Projected on Walls", debugGfxObj);

            for (int i = 0; i < wallObjs.Length; i++) {
                DrawEyes drawEyesForWall = eyesOnWallsObj.AddComponent<DrawEyes>();
                drawEyesForWall.headPoseDriver = headPoseDriver;
                drawEyesForWall.projectionScreenQuad = wallObjs[i];
            }

            /* Not so important given that we now create tracked pose objects
          
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

            DrawTrackers.TrackerDescription stick1Desc = new DrawTrackers.TrackerDescription();
            stick1Desc.displayName = "Stick1";
            stick1Desc.positionEvent = VREventPrototypeVector3.Create(Stick1PositionEventName);
            stick1Desc.rotationEvent = VREventPrototypeQuaternion.Create(Stick1RotationEventName);
            drawTrackers.trackers.Add(stick1Desc);

            DrawTrackers.TrackerDescription stick2Desc = new DrawTrackers.TrackerDescription();
            stick2Desc.displayName = "Stick2";
            stick2Desc.positionEvent = VREventPrototypeVector3.Create(Stick2PositionEventName);
            stick2Desc.rotationEvent = VREventPrototypeQuaternion.Create(Stick2RotationEventName);
            drawTrackers.trackers.Add(stick2Desc);
            */


            VRConfigManager configMgr = vrEngineObj.GetComponent<VRConfigManager>();
            configMgr.startupConfig = singleProcObj.GetComponent<VRConfig>();
            configMgr.EnableStartupVRConfigAndDisableOthers();
        }


    } // end class

} // end namespace
