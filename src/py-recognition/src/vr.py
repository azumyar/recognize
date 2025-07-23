import openvr
import threading
import time

TARACK_ROLE_LEFT = openvr.TrackedControllerRole_LeftHand
TARACK_ROLE_RIGHT = openvr.TrackedControllerRole_RightHand

BUTTON_QUEST_STICK = openvr.k_EButton_Axis1
BUTTON_QUEST_A = openvr.k_EButton_A
BUTTON_QUEST_B = openvr.k_EButton_ApplicationMenu
BUTTON_QUEST_X = openvr.k_EButton_A
BUTTON_QUEST_Y = openvr.k_EButton_ApplicationMenu
BUTTON_QUEST_GRIP = openvr.k_EButton_Grip
BUTTON_QUEST_TRIGGER = openvr.k_EButton_Axis2

#BUTTON_INDEX_TRACKPAD = openvr.k_EButton_Axis0
BUTTON_INDEX_STICK = openvr.k_EButton_Axis0
BUTTON_INDEX_A = openvr.k_EButton_Grip
BUTTON_INDEX_B = openvr.k_EButton_ApplicationMenu
BUTTON_INDEX_TRIGGER = openvr.k_EButton_Axis1

_vr_system = None
def init() -> None:
    thread = threading.Thread(target=_init_proc)
    thread.daemon = True
    thread.run()


def print_vr() -> None:
    vrsys = None
    try:
        vrsys = openvr.init(openvr.VRApplication_Overlay)
    except openvr.error_code.InitError:
        pass

    if vrsys == None:
        print("SteamVRと接続されていません")
        return

    vrsys = openvr.VRSystem()
    for i in range(openvr.k_unMaxTrackedDeviceCount):
        device_class = vrsys.getTrackedDeviceClass(i)
        if device_class == openvr.TrackedDeviceClass_Controller:
            print("########")
            role = vrsys.getControllerRoleForTrackedDeviceIndex(i)
            print(f"Role: {role}")

            s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_TrackingSystemName_String)
            print(f"TrackingSystemName: {s}")
            s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_ModelNumber_String)
            print(f"ModelNumber: {s}")
            s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_ManufacturerName_String)
            print(f"ManufacturerName: {s}")
            s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_RegisteredDeviceType_String)
            print(f"RegisteredDeviceType: {s}")
            #s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_InputProfilePath_String)
            #print(f"InputProfilePath: {s}")
            s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_ActualTrackingSystemName_String)
            print(f"ActualTrackingSystemName: {s}")
            #s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_DisplayMCImageLeft_String)
            #print(f"Prop_DisplayMCImageLeft_String: {s}")
            #s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_DisplayMCImageRight_String)
            #print(f"DisplayMCImageRight: {s}")
            #s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_ExpectedControllerType_String)
            #print(f"Prop_ExpectedControllerType_String: {s}")
            #s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_ModeLabel_String)
            #print(f"Prop_ModeLabel_String: {s}")
            s = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_ControllerType_String )
            print(f"ControllerType : {s}")

def _init_proc() -> None:
    global _vr_system
    while True:
        try:
            _vr_system = openvr.init(openvr.VRApplication_Overlay)
            time.sleep(0.5)
            break
        except openvr.error_code.InitError:
            continue
        except:
            break

def _get_controller_ids(vrsys:openvr.VRSystem, target_role:int, target_device:str | None) -> int | None:
    vrsys = openvr.VRSystem()
    for i in range(openvr.k_unMaxTrackedDeviceCount):
        device_class = vrsys.getTrackedDeviceClass(i)
        if device_class == openvr.TrackedDeviceClass_Controller:
            if target_device == None:
                flag = True
            else:
                tp = vrsys.getStringTrackedDeviceProperty(i, openvr.Prop_ControllerType_String)
                if tp == target_device:
                    flag = True
                else:
                    flag = False

            if flag:
                role = vrsys.getControllerRoleForTrackedDeviceIndex(i)
                if role == target_role:
                    return i
    return None


def is_press(index:int, role:int, target_device:str | None) -> bool:
    if _vr_system == None:
        return False

    vrsys = openvr.VRSystem()
    con = _get_controller_ids(vrsys, role, target_device)
    if con == None:
        return False

    _, state = vrsys.getControllerState(con)
    return (state.ulButtonPressed >> index & 1) == 1


def is_touch(index:int, role:int, target_device:str | None) -> bool:
    if _vr_system == None:
        return False

    vrsys = openvr.VRSystem()
    con = _get_controller_ids(vrsys, role, target_device)
    if con == None:
        return False

    _, state = vrsys.getControllerState(con)
    return (state.ulButtonTouched >> index & 1) == 1


