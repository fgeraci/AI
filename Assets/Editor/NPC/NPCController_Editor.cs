using UnityEditor;
using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

namespace NPC {

    [CustomEditor(typeof(NPCController))]
    public class NPCController_Editor : Editor {

        NPCController gController;

        #region Constants
        private const string label_ViewAngle = "View Angle";
        private const string label_PerceptionRadius = "Perception Radius";
        private const string label_BodyNavigation = "Navigation";
        private const string label_IKEnabled = "Enable IK";
        private const string label_MainAgent = "Main Agent";
        private const string label_SelectHighlight = "Enable Selection Indicator";
        private const string label_AnimatorEnabled = "Use Animator";
        private const string label_UseAnimCurves = "Use Animation Curves";
        private const string label_NavStoppingThresh = "Breaking Threshold";
        private const string label_AIPathfind = "Pathfinder";
        private const string label_NPCLoadedMods = "Loaded NPC Modules";
        private const string label_DebugPrint = "Debug Mode";
        private const string label_NavMeshAgentPathfinding = "Use NavMeshAgent";
        private const string label_UseSocialForces = "Use Social Forces";
        private const string label_AgentRepulsionWeight = "Agents Repulsion Weight";
        private const string label_AgentRepulsionDistanceTolerance = "Agents Distance Tolerance";
        private const string label_TestNPC = "Test NPC";
        private const string label_IKFeetHeight = "IK Feet Height Correction";
        private const string label_IKFeetForward = "IK Feet Forward Correction";
        private const string label_IKFeetEffectorCorrector = "IK Feet Effector Height";
        private const string label_IKUseHints = "Use IK Hints";
        private const string label_IKFeetStairsInt = "IK Stairs Interpolation";

        [SerializeField]
        int selectedPathfinder;

        #endregion

        #region Insperctor_GUI
        private bool gGeneralSettings = true;
        private bool gShowPerception = true;
        private bool gShowBody = true;
        private bool gShowAI = true;
        private bool gShowMods = true;
        #endregion

        public override void OnInspectorGUI() {


            gController = (NPCController) target;
            
            /*
             * Look for added compatible added components, extend with interface later on
             */
            /**/

            EditorGUI.BeginChangeCheck();

            /* Load Modules */
            if (gController.GetComponent<INPCModule>() != null) {
                gShowMods = EditorGUILayout.Foldout(gShowMods, "NPC Modules");
                if (gShowMods) {
                    gController.LoadNPCModules();
                    INPCModule[] mods = gController.NPCModules;
                    foreach(INPCModule m in mods) {
                        EditorGUILayout.LabelField(m.NPCModuleName());
                    }
                }
            } else EditorGUILayout.LabelField("No NPC Modules Loaded");

            /* General Controller */
            gGeneralSettings = EditorGUILayout.Foldout(gGeneralSettings, "General Settings");
            if(gGeneralSettings) { 
                gController.MainAgent = (bool)EditorGUILayout.Toggle(label_MainAgent, (bool)gController.MainAgent);
                gController.DebugMode = (bool)EditorGUILayout.Toggle(label_DebugPrint, (bool)gController.DebugMode);
                gController.DisplaySelectedHighlight = (bool)EditorGUILayout.Toggle(label_SelectHighlight, (bool)gController.DisplaySelectedHighlight);
                gController.TestNPC = (bool)EditorGUILayout.Toggle(label_TestNPC, (bool)gController.TestNPC);
                if (gController.TestNPC) {
                    gController.TestTargetLocation = (Transform)EditorGUILayout.ObjectField("Test Target Location", (Transform)gController.TestTargetLocation, typeof(Transform), true);
                } else gController.TestTargetLocation = null;
            }

            /* Perception */
            gShowPerception = EditorGUILayout.Foldout(gShowPerception, "Perception") && gController.Perception != null;
            if(gShowPerception) {
                gController.Perception.ViewAngle = (float) EditorGUILayout.IntSlider(label_ViewAngle, (int) gController.Perception.ViewAngle, 
                    (int) NPCPerception.MIN_VIEW_ANGLE, 
                    (int) NPCPerception.MAX_VIEW_ANGLE);
                gController.Perception.PerceptionRadius = (float) EditorGUILayout.IntSlider(label_PerceptionRadius, (int) gController.Perception.PerceptionRadius, 
                    (int) NPCPerception.MIN_PERCEPTION_FIELD, 
                    (int) NPCPerception.MAX_PERCEPTION_FIELD);
            }

            /* AI */
            gShowAI = EditorGUILayout.Foldout(gShowAI, "AI") && gController.AI != null;
            if(gShowAI) {
                if(gController.AI.Pathfinders != null) {
                    string[] pfds = new string[gController.AI.Pathfinders.Count];
                    gController.AI.Pathfinders.Keys.CopyTo(pfds, 0);
                    selectedPathfinder = 0;
                    for (int i = 0; i < pfds.Length; ++i) { 
                        if (pfds[i] == gController.AI.SelectedPathfinder)
                            selectedPathfinder = i;
                    }
                    if (gController.AI.Pathfinders.ContainsKey(pfds[selectedPathfinder])) {
                        selectedPathfinder = EditorGUILayout.Popup("Pathfinders", selectedPathfinder, pfds);
                        gController.AI.SelectedPathfinder = pfds[selectedPathfinder];
                    } else {
                        gController.AI.SelectedPathfinder = pfds[0];
                    }
                    if (gController.Body.Navigation == NAV_STATE.STEERING_NAV) {
                        gController.AI.NavMeshAgentPathfinding = (bool)EditorGUILayout.Toggle(label_NavMeshAgentPathfinding, (bool)gController.AI.NavMeshAgentPathfinding);
                        if(gController.AI.NavMeshAgentPathfinding)
                            gController.AI.SelectedPathfinder = pfds[0];
                    } else {
                        gController.AI.NavMeshAgentPathfinding = false;
                    }
                }
                   
            }

            /* Body */
            gShowBody = EditorGUILayout.Foldout(gShowBody, "Body") && gController.Body != null;
            if(gShowBody) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label_BodyNavigation);
                gController.Body.Navigation = (NAV_STATE)EditorGUILayout.EnumPopup((NAV_STATE)gController.Body.Navigation);
                EditorGUILayout.EndHorizontal();
                if(gController.Body.Navigation != NAV_STATE.DISABLED)
                    gController.Body.NavDistanceThreshold = (float) EditorGUILayout.FloatField(label_NavStoppingThresh, (float) gController.Body.NavDistanceThreshold);
                gController.Body.IKEnabled = (bool)EditorGUILayout.Toggle(label_IKEnabled, (bool)gController.Body.IKEnabled);
                if (gController.Body.IKEnabled) {
                    gController.Body.IK_USE_HINTS = (bool)EditorGUILayout.Toggle(label_IKUseHints, (bool)gController.Body.IK_USE_HINTS);
                    gController.Body.IK_FEET_HEIGHT_CORRECTION = (float)EditorGUILayout.Slider(label_IKFeetHeight, gController.Body.IK_FEET_HEIGHT_CORRECTION, 0f, 0.5f);
                    gController.Body.IK_FEET_FORWARD_CORRECTION = (float)EditorGUILayout.Slider(label_IKFeetForward, gController.Body.IK_FEET_FORWARD_CORRECTION, -0.5f, 0.5f);
                    gController.Body.IK_FEET_HEIGHT_EFFECTOR_CORRECTOR = (float)EditorGUILayout.Slider(label_IKFeetEffectorCorrector, gController.Body.IK_FEET_HEIGHT_EFFECTOR_CORRECTOR, 0f, 0.3f);
                    gController.Body.IK_FEET_STAIRS_INTERPOLATION = (float)EditorGUILayout.Slider(label_IKFeetStairsInt, gController.Body.IK_FEET_STAIRS_INTERPOLATION, 0f, 20f);
                }
                gController.Body.UseAnimatorController = (bool)EditorGUILayout.Toggle(label_AnimatorEnabled, (bool)gController.Body.UseAnimatorController);
                gController.Body.UseCurves = (bool)EditorGUILayout.Toggle(label_UseAnimCurves, (bool)gController.Body.UseCurves);
                gController.Body.EnableSocialForces = (bool)EditorGUILayout.Toggle(label_UseSocialForces, (bool)gController.Body.EnableSocialForces);
                if(gController.Body.EnableSocialForces) {
                    gController.Body.AgentRepulsionWeight = (float)EditorGUILayout.FloatField(label_AgentRepulsionWeight, (float) gController.Body.AgentRepulsionWeight);
                    gController.Body.DistanceTolerance = (float)EditorGUILayout.FloatField(label_AgentRepulsionDistanceTolerance, (float)gController.Body.DistanceTolerance);
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(gController, "Parameter Changed");
                EditorUtility.SetDirty(gController);
            }
        }

        private void OnSceneGUI() {
            if(gController != null) {
                if(gShowPerception) {

                    Transform t = gController.Perception.PerceptionField.transform;
            
                    /* Draw View Angle */
                    float angleSplit = gController.Perception.ViewAngle / 2;
                    Debug.DrawRay(t.position,
                        Quaternion.AngleAxis(angleSplit, Vector3.up) * t.rotation * Vector3.forward * gController.Perception.PerceptionRadius * t.lossyScale.z, Color.red);
                    Debug.DrawRay(t.position, 
                        Quaternion.AngleAxis((-1) * angleSplit, Vector3.up) * t.rotation * Vector3.forward * gController.Perception.PerceptionRadius * t.lossyScale.z, Color.red);
                }
            }
        }

    }

}
