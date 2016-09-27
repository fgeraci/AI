using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace NPC {

    public class NPCCamController : MonoBehaviour {
    
        public float Speed = 1.0f;
        public float CameraRotationSpeed = 20f;
        public float IsometricHeight = 4.0f;
        NPCControlManager g_NPCControlManager;
        
    
        Transform Camera = null;
        private float PanSmoothness = 0.1f;
        private NPCController Target = null;
        private bool gPanning = false;
        private bool gCloseUp = false;
        private float gMouseX;
        private float gMouseY;

        public bool Targetting {
            get {
                return CurrentMode == CAMERA_MODE.THIRD_PERSON ||
                    CurrentMode == CAMERA_MODE.FIRST_PERSON || 
                    CurrentMode == CAMERA_MODE.ISOMETRIC_FOLLOW;
            }
        }

        public bool CloseUp {
            get {
                return gCloseUp;
            }
            set {
                gCloseUp = value;
                gPanning = true;
            }
        }
        
        public CAMERA_MODE CurrentMode;

        public enum CAMERA_MODE {
            FREE,
            THIRD_PERSON,
            FIRST_PERSON,
            ISOMETRIC,
            ISOMETRIC_FOLLOW
        }
        
        public void SetCamera(Transform t) {
            Camera = t;
        }

        public void SetTarget(NPCController t) {
            Target = t;
        }

        void Start() {
            g_NPCControlManager = FindObjectOfType<NPCControlManager>();
            if (Target != null) {
                SetThirdPersonView();
                CurrentMode = CAMERA_MODE.THIRD_PERSON;
            }
            if(g_NPCControlManager == null) {
                Debug.Log("NPCCamController --> No NPCControlManager with the NPCCamController enabled");
            }
        }

        public void UpdateCamera() {
            switch (CurrentMode) {
                case CAMERA_MODE.FREE:
                    HandleFreeCamera();
                    break;
                case CAMERA_MODE.FIRST_PERSON:
                    if (Target == null) {
                        Debug.Log("NPCCamController --> Can't set this mode without an NPC target");
                        CurrentMode = CAMERA_MODE.FREE;
                    } else {
                        if (!Target.Body.IsIdle)
                            SetFirstPersonView();
                    }
                    break;
                case CAMERA_MODE.THIRD_PERSON:
                    if (Target == null) {
                        Debug.Log("NPCCamController --> Can't set this mode without an NPC target");
                        CurrentMode = CAMERA_MODE.FREE;
                    } else {
                        if (!Target.Body.IsIdle || CloseUp)
                            SetThirdPersonView();
                    }
                    break;
                case CAMERA_MODE.ISOMETRIC:
                    HandleIsometricCamera();
                    break;
            }
        }
     
        public void UpdateCameraMode(CAMERA_MODE mode) {
            bool noTarget = false;
            CurrentMode = mode;
            switch (CurrentMode) {
                case CAMERA_MODE.FREE:
                    if (Target != null) {
                        Target.Body.Navigation = NAV_STATE.DISABLED;
                        SetThirdPersonView();
                    }
                    g_NPCControlManager.SetIOTarget(null);
                    break;
                case CAMERA_MODE.FIRST_PERSON:
                    if (Target != null) {
                        Target.Body.Navigation = NAV_STATE.DISABLED;
                        SetFirstPersonView();
                        g_NPCControlManager.SetIOTarget(Target);
                    } else noTarget = true;
                    break;
                case CAMERA_MODE.THIRD_PERSON:
                    if (Target != null) {
                        Target.Body.Navigation = NAV_STATE.DISABLED;
                        SetThirdPersonView();
                    }
                    else noTarget = true;
                    break;
                case CAMERA_MODE.ISOMETRIC:
                    if (Target != null) Target.Body.Navigation = NAV_STATE.STEERING_NAV;
                    SetIsometricView();
                    break;
            }
            if(noTarget) {
                g_NPCControlManager.SetIOTarget(null);
                CurrentMode = CAMERA_MODE.FREE;
                Debug.Log("NPCCamControlelr --> No target agent set, camera stays in FREE mode.");
            }
        }

        public void ResetView() {
            UpdateCameraMode(CurrentMode);
        }

        private void HandleCameraRotation() {
            if(Input.GetMouseButton(1)) {
                float mouseYRot = (Input.mousePosition.x - gMouseX) * Time.deltaTime * CameraRotationSpeed;
                float mouseXRot = (Input.mousePosition.y - gMouseY) * Time.deltaTime * CameraRotationSpeed;
                // we set the rotation as current angles + delta
                transform.eulerAngles = new Vector3(-mouseXRot + transform.localEulerAngles.x, 
                    transform.localEulerAngles.y + mouseYRot , 0);
            }
        }

        private void HandleFreeCamera() {
            float speedModifier = Input.GetKey(KeyCode.LeftShift) ? Speed * 2f : Speed;
            HandleCameraRotation();
            if (Input.GetKey(KeyCode.W)) {
                transform.position += transform.forward * (Time.deltaTime * speedModifier);
            } else if (Input.GetKey(KeyCode.S)) {
                transform.position -= transform.forward * (Time.deltaTime * speedModifier);
            }
            if (Input.GetKey(KeyCode.A)) {
                transform.position -= transform.right * (Time.deltaTime * speedModifier);
            } else if (Input.GetKey(KeyCode.D)) {
                transform.position += transform.right * (Time.deltaTime * speedModifier);
            }
            gMouseX = Input.mousePosition.x;
            gMouseY = Input.mousePosition.y;
        }

        private void HandleIsometricCamera() {
            float speedModifier = Input.GetKey(KeyCode.LeftShift) ? Speed * 2f : Speed;
            if (Input.GetKey(KeyCode.W)) {
                Camera.position += Vector3.right * (Time.deltaTime * speedModifier);
            } else if (Input.GetKey(KeyCode.S)) {
                Camera.position -= Vector3.right * (Time.deltaTime * speedModifier);
            }
            if (Input.GetKey(KeyCode.A)) {
                Camera.position += Vector3.forward * (Time.deltaTime * speedModifier);
            } else if (Input.GetKey(KeyCode.D)) {
                Camera.position -= Vector3.forward * (Time.deltaTime * speedModifier);
            }

            if(Input.GetAxis("Mouse ScrollWheel") > 0.0f) {
                Camera.position -= Vector3.up * (0.08f * speedModifier);
            } else if (Input.GetAxis("Mouse ScrollWheel") < 0.0f) {
                Camera.position += Vector3.up * (0.08f * speedModifier);
            }
        }

        private void SetThirdPersonView() {
            Vector3 pos;
            if (CloseUp) {
                pos = Target.Body.Head.position;
                pos += Target.transform.forward * -0.2f;
                pos += Target.transform.right * 0.2f;
                pos += Target.transform.up * 0.05f;
                if (gPanning) {
                    float delta = Time.deltaTime * PanSmoothness;
                    Camera.position = Vector3.Lerp(Camera.position, pos, delta);
                    gPanning = delta > 1f;
                } else {
                    gPanning = false;
                    Camera.position = pos;
                }
                Camera.LookAt(Target.Body.TargetObject);
            } else {
                Camera.rotation = Target.transform.rotation;
                pos = Target.transform.position;
                pos += Camera.up * 0.8f;
                pos += Camera.forward * -0.6f;
                pos += Camera.right * 0.2f;
                Camera.position = pos;
                Camera.RotateAround(Camera.position, Camera.right, 15f);
            }
        }

        private void SetFirstPersonView() {
            Camera.position = Target.transform.position;
            Camera.rotation = Target.transform.rotation;
            Camera.position += Target.transform.forward * 0.1f;
            Camera.position += Target.transform.up * 0.45f;
        }

        private void SetIsometricView() {
            Vector3 curPos = Camera.position;
            Camera.rotation = Quaternion.identity;
            Camera.Rotate(Vector3.up, 90.0f);
            Camera.Rotate(Vector3.right, 35.0f);
            Camera.position = new Vector3(curPos.x, IsometricHeight, curPos.z);
            Camera.position -= (Vector3.right * 0.5f);
        }
    }

}
