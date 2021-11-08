using System;
using System.Collections;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using XR.General;
using VarjoInput;

/// <summary>
/// A generic gesture input system that draws a many to one link between VR APKS and globally accessible bools for multi platform support
/// </summary>
public class GestureInputSample : MonoBehaviour
{
    public static GestureInputSample Instance;
    #region Unity Editor Variables
    [Header("SteamVRControllers")]
    [SerializeField] Hand _leftSteamController = null;
    [SerializeField] Hand _rightSteamController = null;

    [Header("OculusVRControllers")]
    [SerializeField] Transform _leftOculusController = null;
    [SerializeField] Transform _rightOculusController = null;

    [Header("VarjoRControllers")]
    [SerializeField] Controller _rightVarjoController = null;
    [SerializeField] Controller _leftVarjoController = null;
    #endregion// Unity Editor Variables

    #region Private Variables
    Transform _rightControllerTransform = null;
    Transform _leftcontrollerTransform = null;

    bool _rightTriggerPressed;
    bool _leftTriggerPressed;
    bool _rightTouchPressed;
    bool _leftTouchPressed;
    bool _rightMenuButtonPressed;
    bool _leftMenuButtonPressed;
    bool _rightGripPressed;
    bool _leftGripPressed;

    Vector2 _rightTouchPosition;
    Vector2 _leftTouchPosition;
    #endregion //Private Variables

    #region Public Properties
    //This area contains all public input, allowing platform agnostic input to be called form elsewhere in the app
    public bool RightTriggerPressed { get { return _rightTriggerPressed; } private set { } }
    public bool LeftTriggerPressed { get { return _leftTriggerPressed; } private set { } }
    public bool RightTouchPressed { get { return _rightTouchPressed; } private set { } }
    public bool LeftTouchPressed { get { return _leftTouchPressed; } private set { } }
    public bool RightMenuButtonPressed { get { return _rightMenuButtonPressed; } private set { } }
    public bool LeftMenuButtonPressed { get { return _leftMenuButtonPressed; } private set { } }
    public bool RightGripPressed { get { return _rightGripPressed; } private set { } }
    public bool LeftGripPressed { get { return _leftGripPressed; } private set { } }

    public Vector2 RightTouchPosition { get { return _rightTouchPosition; } private set { } }
    public Vector2 LeftTouchPosition { get { return _leftTouchPosition; } private set { } }
    #endregion // Public Properties

    #region Private Unity Methods
    //Intialise static instance and controller transforms Using a seperate manager to determine platfrom
    void Start()
    {
        _XRRaycaster = XRRaycaster.Instance;
        switch(XRManager.Instance.ThisVRPlatform)
        {
            case VRPlatform.SteamVR:
                _rightControllerTransform = _rightSteamController.transform;
                _leftcontrollerTransform = _leftSteamController.transform;
                break;
            case VRPlatform.Oculus:
                _rightControllerTransform = _rightOculusController;
                _leftcontrollerTransform = _leftOculusController;
                break;
            case VRPlatform.Varjo:
                _rightControllerTransform = _rightVarjoController.transform;
                _leftcontrollerTransform = _leftVarjoController.transform;
                break;

        }
        _rightControllerTransform = _rightVarjoController.transform;
        _leftcontrollerTransform = _leftVarjoController.transform;
    }

    // Using the XRManager, determine current platform and poll for Input from the appropriate SDK
    void Update()
    {
        _rightTouchPosition = Vector2.zero;
        _leftTouchPosition = Vector2.zero;

        if(XRManager.Instance.ThisVRPlatform == VRPlatform.SteamVR)
        {
            _rightTriggerPressed = SteamVR_Actions.TheoremVRActions.RightTrigger.GetStateDown(SteamVR_Input_Sources.Any);
            _leftTriggerPressed = SteamVR_Actions.TheoremVRActions.LeftTrigger.GetStateDown(SteamVR_Input_Sources.Any);

            _rightGripPressed = SteamVR_Actions.TheoremVRActions.RightGrip.GetStateDown(SteamVR_Input_Sources.Any);
            _leftGripPressed = SteamVR_Actions.TheoremVRActions.LeftGrip.GetStateDown(SteamVR_Input_Sources.Any);

            _rightTouchPressed = SteamVR_Actions.TheoremVRActions.RightDirectionalPress.GetStateDown(SteamVR_Input_Sources.Any);
            _leftTouchPressed = SteamVR_Actions.TheoremVRActions.LeftDirectionalPress.GetStateDown(SteamVR_Input_Sources.Any);

            _rightMenuButtonPressed = SteamVR_Actions.TheoremVRActions.RightMenubutton.GetStateDown(SteamVR_Input_Sources.Any);
            _leftMenuButtonPressed = SteamVR_Actions.TheoremVRActions.LeftMenuButton.GetStateDown(SteamVR_Input_Sources.Any);

            _rightTouchPosition = SteamVR_Actions.TheoremVRActions.RightDirectional.GetAxis(SteamVR_Input_Sources.Any);
            _leftTouchPosition = SteamVR_Actions.TheoremVRActions.LeftDirectional.GetAxis(SteamVR_Input_Sources.Any);
        }
        else if(XRManager.Instance.ThisPlatform == VRPlatform.Oculus)
        {
            _rightTriggerPressed = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);
            _leftTriggerPressed = OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger);

            _rightGripPressed = OVRInput.GetDown(OVRInput.RawButton.RHandTrigger);
            _leftGripPressed = OVRInput.GetDown(OVRInput.RawButton.LHandTrigger);

            _rightTouchPressed = OVRInput.GetDown(OVRInput.RawButton.RThumbstick);
            _leftTouchPressed = OVRInput.GetDown(OVRInput.RawButton.LThumbstick);

            _rightMenuButtonPressed = OVRInput.GetDown(OVRInput.RawButton.A);
            _leftMenuButtonPressed = OVRInput.GetDown(OVRInput.RawButton.X);

            _rightTouchPosition = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
            _leftTouchPosition = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
        }
        if (XRManager.Instance.ThisPlatform == VRPlatform.Varjo)
        {

            _rightTriggerPressed = _rightVarjoController.TriggerButtonDown;
            _leftTriggerPressed = _leftVarjoController.TriggerButtonDown;

            _rightGripPressed = _rightVarjoController.GripButtonDown;
            _leftGripPressed = _leftVarjoController.GripButtonDown;

            _rightTouchPressed = _rightVarjoController.TouchpadClickDown;
            _leftTouchPressed = _leftVarjoController.TouchpadClickDown;

            _rightMenuButtonPressed = _rightVarjoController.MenuButtonDown;
            _leftMenuButtonPressed = _leftVarjoController.MenuButtonDown;

            _rightTouchPosition = _rightVarjoController.TouchpadTouchPosition;
            _leftTouchPosition = _leftVarjoController.TouchpadTouchPosition;
        }
    }
    #endregion// Private Unity Methods
}
