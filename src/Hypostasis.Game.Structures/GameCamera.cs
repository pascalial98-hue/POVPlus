using System;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Hypostasis.Dalamud;

namespace Hypostasis.Game.Structures;

[StructLayout(LayoutKind.Explicit)]
[GameStructure("40 53 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 83 C1 10 E8 ?? ?? ?? ?? 0F B6 83 ?? ?? ?? ?? 33 C9 24 FD")]
public struct GameCamera : IHypostasisStructure
{
	public unsafe class GameCameraVTable(nint* v) : VirtualTable(v)
	{
		public unsafe delegate void SetCameraLookAtDelegate(GameCamera* camera, Vector3* lookAtPosition, Vector3* cameraPosition, Vector3* a4);

		public unsafe delegate void GetCameraPositionDelegate(GameCamera* camera, GameObject* target, Vector3* position, Bool swapPerson);

		public unsafe delegate GameObject* GetCameraTargetDelegate(GameCamera* camera);

		public delegate Bool CanChangePerspectiveDelegate();

		public delegate float GetZoomDeltaDelegate();

		public unsafe readonly VirtualFunction<SetCameraLookAtDelegate> setCameraLookAt = new VirtualFunction<SetCameraLookAtDelegate>(v, 15, "40 53 48 83 EC 30 44 8B 89 ?? ?? ?? ?? 48 8B DA");

		public unsafe readonly VirtualFunction<GetCameraPositionDelegate> getCameraPosition = new VirtualFunction<GetCameraPositionDelegate>(v, 16);

		public unsafe readonly VirtualFunction<GetCameraTargetDelegate> getCameraTarget = new VirtualFunction<GetCameraTargetDelegate>(v, 18);

		public unsafe readonly VirtualFunction<CanChangePerspectiveDelegate> canChangePerspective = new VirtualFunction<CanChangePerspectiveDelegate>(v, 23);

		public unsafe readonly VirtualFunction<GetZoomDeltaDelegate> getZoomDelta = new VirtualFunction<GetZoomDeltaDelegate>(v, 29, "F3 0F 10 05 ?? ?? ?? ?? C3");
	}

	public unsafe delegate byte GetCameraAutoRotateModeDelegate(GameCamera* camera, Framework* framework);

	public unsafe delegate float GetCameraMaxMaintainDistanceDelegate(GameCamera* camera);

	public unsafe delegate Bool UpdateLookAtHeightOffsetDelegate(GameCamera* camera, GameObject* o, Bool zero);

	public unsafe delegate Bool ShouldDisplayObjectDelegate(GameCamera* camera, GameObject* o, Vector3* cameraPosition, Vector3* cameraLookAt);

	[FieldOffset(0)]
	public unsafe nint* vtbl;

	[FieldOffset(96)]
	public float x;

	[FieldOffset(100)]
	public float y;

	[FieldOffset(104)]
	public float z;

	[FieldOffset(144)]
	public float lookAtX;

	[FieldOffset(148)]
	public float lookAtY;

	[FieldOffset(152)]
	public float lookAtZ;

	[FieldOffset(292)]
	public float currentZoom;

	[FieldOffset(296)]
	public float minZoom;

	[FieldOffset(300)]
	public float maxZoom;

	[FieldOffset(304)]
	public float currentFoV;

	[FieldOffset(308)]
	public float minFoV;

	[FieldOffset(312)]
	public float maxFoV;

	[FieldOffset(316)]
	public float addedFoV;

	[FieldOffset(320)]
	public float currentHRotation;

	[FieldOffset(324)]
	public float currentVRotation;

	[FieldOffset(328)]
	public float hRotationDelta;

	[FieldOffset(344)]
	public float minVRotation;

	[FieldOffset(348)]
	public float maxVRotation;

	[FieldOffset(368)]
	public float tilt;

	[FieldOffset(384)]
	public int mode;

	[FieldOffset(388)]
	public int controlType;

	[FieldOffset(396)]
	public float interpolatedZoom;

	[FieldOffset(416)]
	public float transition;

	[FieldOffset(448)]
	public float viewX;

	[FieldOffset(452)]
	public float viewY;

	[FieldOffset(456)]
	public float viewZ;

	[FieldOffset(500)]
	public byte isFlipped;

	[FieldOffset(556)]
	public float interpolatedY;

	[FieldOffset(564)]
	public float lookAtHeightOffset;

	[FieldOffset(568)]
	public byte resetLookatHeightOffset;

	[FieldOffset(576)]
	public float interpolatedLookAtHeightOffset;

	[FieldOffset(704)]
	public byte lockPosition;

	[FieldOffset(724)]
	public float lookAtY2;

	private static GameCameraVTable vtable;

	public static readonly GameFunction<GetCameraAutoRotateModeDelegate> getCameraAutoRotateMode = new GameFunction<GetCameraAutoRotateModeDelegate>("E8 ?? ?? ?? ?? 48 8B CB 85 C0 0F 84 ?? ?? ?? ?? 83 E8 01");

	public static readonly GameFunction<GetCameraMaxMaintainDistanceDelegate> getCameraMaxMaintainDistance = new GameFunction<GetCameraMaxMaintainDistanceDelegate>("E8 ?? ?? ?? ?? F3 0F 5D 44 24 58");

	public static readonly GameFunction<UpdateLookAtHeightOffsetDelegate> updateLookAtHeightOffset = new GameFunction<UpdateLookAtHeightOffsetDelegate>("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 48 8B 02 48 8B F1 48 8B CA");

	public static readonly GameFunction<ShouldDisplayObjectDelegate> shouldDisplayObject = new GameFunction<ShouldDisplayObjectDelegate>("E8 ?? ?? ?? ?? 84 C0 75 18 48 8D 0D ?? ?? ?? ?? B3 01");

	public bool IsHRotationOffset => mode == isFlipped;

	public float GameObjectHRotation
	{
		get
		{
			float result;
			if (IsHRotationOffset)
			{
				result = currentHRotation;
			}
			else
			{
				result = ((currentHRotation > 0f) ? (currentHRotation - (float)Math.PI) : (currentHRotation + (float)Math.PI));
			}
			return result;
		}
	}

	public unsafe GameCameraVTable VTable => vtable ?? (vtable = new GameCameraVTable(vtbl));

	public unsafe void SetCameraLookAt(Vector3* lookAtPosition, Vector3* cameraPosition, Vector3* a4)
	{
		fixed (GameCamera* camera = &this)
		{
			VTable.setCameraLookAt.Invoke(camera, lookAtPosition, cameraPosition, a4);
		}
	}

	public unsafe void GetCameraPosition(GameObject* target, Vector3* position, bool swapPerson)
	{
		fixed (GameCamera* camera = &this)
		{
			VTable.getCameraPosition.Invoke(camera, target, position, swapPerson);
		}
	}

	public unsafe GameObject* GetCameraTarget()
	{
		fixed (GameCamera* camera = &this)
		{
			return VTable.getCameraTarget.Invoke(camera);
		}
	}

	public Bool CanChangePerspective()
	{
		return VTable.canChangePerspective.Invoke();
	}

	public float GetZoomDelta()
	{
		return VTable.getZoomDelta.Invoke();
	}

	public unsafe byte GetCameraAutoRotateMode()
	{
		fixed (GameCamera* camera = &this)
		{
			return getCameraAutoRotateMode.Invoke(camera, Framework.Instance());
		}
	}

	public unsafe float GetCameraMaxMaintainDistance()
	{
		fixed (GameCamera* camera = &this)
		{
			return getCameraMaxMaintainDistance.Invoke(camera);
		}
	}

	public unsafe bool UpdateLookAtHeightOffset(GameObject* o, bool zero)
	{
		fixed (GameCamera* camera = &this)
		{
			return updateLookAtHeightOffset.Invoke(camera, o, zero);
		}
	}

	public unsafe bool ShouldDisplayObject(GameObject* o)
	{
		fixed (GameCamera* ptr = &this)
		{
			return shouldDisplayObject.Invoke(ptr, o, (Vector3*)(&ptr->x), (Vector3*)(&ptr->lookAtX));
		}
	}

	public bool Validate()
	{
		return true;
	}
}
