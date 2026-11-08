using System.Numerics;
using Hypostasis.Game.Structures;

namespace PovPlus;

internal static class GlobalVars
{
	public static float InitOnlyOncePls = 0f;

	public static GameCamera PlayersCamera;

	public static float RotationBindBoneX = 1f;

	public static int RotationBoneValueX = 46;

	public static float RotationBindOffsetX = -0.96f;

	public static string RotationBoneValueXName = "ERROR";

	public static float RotationBindBoneZ = 1f;

	public static int RotationBoneValueZ = 46;

	public static float RotationBindOffsetZ = -0.67f;

	public static string BoundBone_BoneName = "ERROR";

	public static int PlayerBoneCount = 1;

	public static float CameraXRelative = 0f;

	public static float CameraXRelativePrev = 0f;

	public static float CameraZRelative = 0f;

	public static float CameraZRelativePrev = 0f;

	public static Quaternion CameraQuartCurrent;

	public static Quaternion CameraQuartPrev;

	public static Quaternion CameraQuartNormalize;

	public static float PlayerXRotationCurrent;

	public static float PlayerXRotationPrev;

	public static float PlayerXRotationNormalize;

	public static float BoneYawRotateCurrent = 0f;

	public static float BoneYawRotateBefore = 0f;

	public static float BonePitchRotateCurrent = 0f;

	public static float BonePitchRotateBefore = 0f;

	public static float TotalRotationOffsetAddition = 0f;

	public static float TotalYawCurrent;

	public static float TotalYawPrev;

	public static Vector3 LastPlayerPosition;

	public static Vector3 CurrentPlayerPosition;

	public static bool RightClickMoving;

	public static bool StationaryRotateRight;

	public static float TemporalYawPrevious = 10f;

	public static float TOTALYAW = 0f;

	public static float PreviousRotationBeforeChanges = 0f;

	public static float PreviousMaxVRotation;

	public static float PreviousMinVRotation;

	public static float PreviousCurrentFoV;

	public static float PreviousTilt;

	public static float PreviousMinFOV;

	public static float BoneYaw_X;

	public static float BoneRoll_Y;

	public static float BonePitch_Z;

	public static bool CameraFlipped;

	public static bool YZFlip;

	public static int CameraBindValue = 1;

	public static int RotationBindValue;

	public static bool HideOwnBody = true;

	public static Vector3 BoneLocation = default;

	public static Vector3 BoneLookingEnding = default;

	public static Vector3 DEBUG_PitchCameraEnd = default;

	public static Vector3 DEBUG_YawCameraEnd = default;

	public static Vector3 DEBUG_RollCameraEnd = default;
}
