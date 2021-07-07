using UnityEngine;
using UnityEditor;
using System.Linq;

[InitializeOnLoad]
static public class BlenderEditorKeys
{
	static BlenderEditorKeys() {
		SceneView.beforeSceneGui -= SceneGUI;
		SceneView.beforeSceneGui += SceneGUI;

		EditorApplication.hierarchyWindowItemOnGUI -= HeirarchyGUI;
		EditorApplication.hierarchyWindowItemOnGUI += HeirarchyGUI;
	}

	static void OnDestroy() {
		SceneView.beforeSceneGui -= SceneGUI;
		EditorApplication.hierarchyWindowItemOnGUI -= HeirarchyGUI;
	}

	static void HeirarchyGUI(int i, Rect r) {
		if (Event.current.type == EventType.KeyDown) {
			HandleKeys();
		}
	}

	static void SceneGUI(SceneView sv) {
		// ignore all events while panning around the scene with right click
		if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && !isGrabbing && !isRotating && !isScaling){
			isPanning = true;
			return;
		} else if (Event.current.type == EventType.MouseUp && Event.current.button == 1) {
			isPanning = false;

		// key presses
		} else if (Event.current.type == EventType.KeyUp) {
			if (isSnapping && Event.current.keyCode == KeyCode.LeftControl) {
				isSnapping = false;
				UseEvent();
			}
		} else if (Event.current.type == EventType.KeyDown) {
			HandleKeys();

		// mouse clicks
		} else if (Event.current.type == EventType.MouseDown && (isGrabbing || isRotating || isScaling)) {
			// confirm on left click
			if (Event.current.button == 0) {
				UseEvent();
				HandleConfirm();
			// cancel on right click
			} else if (Event.current.button == 1) {
				UseEvent();
				HandleCancel();
			}
		}

		// update transformations
		if (isGrabbing) {
			UpdateTranslate();
		} else if (isRotating) {
			UpdateRotation();
		} else if (isScaling) {
			UpdateScale();
		}
	}

	static KeyCode[] numberKeys = { KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Period, KeyCode.Minus, KeyCode.Backspace };

	static void HandleKeys() {
		// ignore key events while camera is panning
		if (isPanning)
			return;

		// reset transforms with Shift + [key]
		if (Event.current.modifiers == EventModifiers.Shift && Event.current.keyCode == KeyCode.G) {
			ClearTranslate();
			UseEvent();
		} else if (Event.current.modifiers == EventModifiers.Shift && Event.current.keyCode == KeyCode.R) {
			ClearRotate();
			UseEvent();
		} else if (Event.current.modifiers == EventModifiers.Shift && Event.current.keyCode == KeyCode.S) {
			ClearScale();
			UseEvent();

		// grab
		} else if (!isGrabbing && !isRotating && !isScaling && Event.current.keyCode == KeyCode.G) {
			StartGrab();
			UseEvent();
		// rotate
		} else if 	(!isGrabbing && !isRotating && !isScaling && Event.current.keyCode == KeyCode.R) {
			StartRotate();
			UseEvent();
		// scale
		} else if 	(!isGrabbing && !isRotating && !isScaling && Event.current.keyCode == KeyCode.S) {
			StartScale();
			UseEvent();

		// axis restrictions
		// TO DO: some indication (unity output log?) of what axis are currently being used
		// TO DO: double axis lock with shift exclusion (instead of current additive-toggle method)
		} else if (Event.current.keyCode == KeyCode.X) {
			UseEvent();
			if (isRotating) ResetAxis();
			onX = !onX;
		} else if (Event.current.keyCode == KeyCode.Y) {
			UseEvent();
			if (isRotating) ResetAxis();
			onY = !onY;
		} else if (Event.current.keyCode == KeyCode.Z) {
			UseEvent();
			if (isRotating) ResetAxis();
			onZ = !onZ;

		// type in exact number to transform by
		} else if (numberKeys.Contains(Event.current.keyCode) && (isGrabbing || isRotating || isScaling)) {
			string keyStr = Event.current.keyCode.ToString();
			if (keyStr.StartsWith("Alpha"))
				exactNumber += keyStr.Substring(5);
			else if (keyStr == "Period")
				exactNumber += ".";
			else if (keyStr == "Minus")
				exactNumber += "-";
			else if (keyStr == "Backspace")
				exactNumber = exactNumber.Substring(0, exactNumber.Length-1);
			UseEvent();


		// snapping
		} else if (Event.current.keyCode == KeyCode.LeftControl) {
			isSnapping = true;
			UseEvent();

		// cancel on escape
		} else if (Event.current.keyCode == KeyCode.Escape) {
			UseEvent();
			HandleCancel();
		// confirm on any key
		} else if (Event.current.keyCode != KeyCode.None) {
			UseEvent();
			HandleConfirm();
		}
	}

	static void UseEvent() {
		if (isGrabbing || isRotating || isScaling)
			Event.current.Use();
	}

	static void HandleConfirm() {
		if (isGrabbing) {
			ConfirmGrab();
			return;
		}

		if (isRotating) {
			ConfirmRotate();
			return;
		}

		if (isScaling) {
			ConfirmScale();
			return;
		}
	}

	static void HandleCancel() {
		if (isGrabbing) {
			CancelGrab();
			return;
		}

		if (isRotating) {
			CancelRotate();
			return;
		}

		if (isScaling) {
			CancelScale();
			return;
		}
	}


	/* --- COMMON --- */
	static bool isPanning;
	
	static Transform[] selected;
	static Vector3 centerPos;
	static Vector2 mouseStart;
	static bool isSnapping = false;

	static Vector3[] origPos;
	static Quaternion[] origRot;
	static Vector3[] origScale;

	static string exactNumber = "";

	static bool onX;
	static bool onY;
	static bool onZ;

	static void ResetAxis() {
		onX = false;
		onY = false;
		onZ = false;
	}

	static void Prepare() {
		exactNumber = "";
		ResetAxis();

		mouseStart = Event.current.mousePosition;
		selected = Selection.GetTransforms(SelectionMode.TopLevel);

		// calculate center pos
		centerPos = Vector3.zero;
		for (int i = 0; i < selected.Length; i++) {
			centerPos += selected[i].position;
		}
		centerPos /= selected.Length;

		// save original transforms
		origPos = new Vector3[selected.Length];
		origRot = new Quaternion[selected.Length];
		origScale = new Vector3[selected.Length];
		for (int i = 0; i < selected.Length; i++) {
			origPos[i] = selected[i].position;
			origRot[i] = selected[i].rotation;
			origScale[i] = selected[i].localScale;
		}
	}

	static void Save(string undoType) {
		// store new transforms
		Vector3[] newPos = new Vector3[selected.Length];
		Quaternion[] newRot = new Quaternion[selected.Length];
		Vector3[] newScale = new Vector3[selected.Length];
		for (int i = 0; i < selected.Length; i++) {
			newPos[i] = selected[i].position;
			newRot[i] = selected[i].rotation;
			newScale[i] = selected[i].localScale;
		}

		// temporarily reset back to original transforms
		for (int i = 0; i < selected.Length; i++) {
			selected[i].position = origPos[i];
			selected[i].rotation = origRot[i];
			selected[i].localScale = origScale[i];
		}

		// record undo and set new transforms
		Undo.RecordObjects(selected, undoType);
		for (int i = 0; i < selected.Length; i++) {
			selected[i].position = newPos[i];
			selected[i].rotation = newRot[i];
			selected[i].localScale = newScale[i];
		}
	}

	static void Reset() {
		for (int i = 0; i < selected.Length; i++) {
			selected[i].position = origPos[i];
			selected[i].rotation = origRot[i];
			selected[i].localScale = origScale[i];
		}
	}

	static Vector2 getCenter() {
		// center position in screen space
		Camera sceneCam = SceneView.lastActiveSceneView.camera;
		Vector2 center = sceneCam.WorldToScreenPoint(centerPos);
		center.y = sceneCam.pixelHeight - center.y; // invert Y
		return center;
	}

	static bool validExactNumber() {
		return (exactNumber != "" && exactNumber != "-" && exactNumber != ".");
	}


	/* ----- GRAB ----- */
	static bool isGrabbing = false;
	static bool getGrabOffset;
	static Vector3 grabOffset;

	static void StartGrab() {
		Prepare();
		isGrabbing = true;
		getGrabOffset = true;
	}

	static Vector3 SnapVec(Vector3 vec) {
		if (!EditorSnapSettings.gridSnapEnabled && !isSnapping) {
			return vec;
		}

		vec.x /= EditorSnapSettings.move.x;
		vec.x = Mathf.Round(vec.x);
		vec.x *= EditorSnapSettings.move.x;

		vec.y /= EditorSnapSettings.move.y;
		vec.y = Mathf.Round(vec.y);
		vec.y *= EditorSnapSettings.move.y;

		vec.z /= EditorSnapSettings.move.z;
		vec.z = Mathf.Round(vec.z);
		vec.z *= EditorSnapSettings.move.z;

		return vec;
	}

	static void UpdateTranslate() {
		Camera sceneCam = SceneView.lastActiveSceneView.camera;
		Vector2 mousePos = Event.current.mousePosition;
		mousePos.y = Screen.height - mousePos.y; // invert Y

		// cast mouse position onto camera plane and cast a ray through it perpendicularly onto the scene
		Plane plane = new Plane(GeometryUtility.CalculateFrustumPlanes(sceneCam)[4].normal, centerPos);
		float distance;
		Ray ray = sceneCam.ScreenPointToRay(mousePos);
		if (plane.Raycast(ray, out distance)) {
			Vector3 pos = ray.GetPoint(distance);

			// get initial offset from mouse
			if (getGrabOffset) {
				getGrabOffset = false;
				grabOffset = centerPos - pos;
			}

			// global and free translation
			if ((!onX && !onY && !onZ) || Tools.pivotRotation == PivotRotation.Global) {
				for (int i = 0; i < selected.Length; i++) {
					Vector3 posOffset = pos + grabOffset;
					posOffset += origPos[i] - centerPos;
					Vector3 newPos = origPos[i];

					if (!onX && !onY && !onZ) {
						newPos = posOffset;
					} else {
						if (validExactNumber()) {
							posOffset.x = float.Parse(exactNumber);
							posOffset.y = float.Parse(exactNumber);
							posOffset.z = float.Parse(exactNumber);
						}

						if (onX)
							newPos.x = posOffset.x;
						if (onY)
							newPos.y = posOffset.y;
						if (onZ)
							newPos.z = posOffset.z;
					}

					newPos = SnapVec(newPos);
					selected[i].position = newPos;
				}
			
			// local axis-restricted translation
			} else {
				Vector3 thisOffset = centerPos - pos;
				Vector3 diff = grabOffset - thisOffset;

				if (validExactNumber()) {
					diff.x = float.Parse(exactNumber);
					diff.y = float.Parse(exactNumber);
					diff.z = float.Parse(exactNumber);
				}

				for (int i = 0; i < selected.Length; i++) {
					selected[i].position = origPos[i];

					Vector3 newPos = Vector3.zero;
					if (onX)
						newPos.x = diff.x;
					if (onY)
						newPos.y = diff.y;
					if (onZ)
						newPos.z = diff.z;

					newPos = SnapVec(newPos);
					selected[i].Translate(newPos, Space.Self);
				}
			}
		}
	}

	static void ConfirmGrab() {
		Save("Grab");
		isGrabbing = false;
	}

	static void CancelGrab() {
		Reset();
		isGrabbing = false;
	}

	static void ClearTranslate() {
		Prepare();
		Save("Clear Translation");
		for (int i = 0; i < selected.Length; i++)
			selected[i].position = Vector3.zero;
	}


	/* ----- ROTATE ----- */
	static bool isRotating = false;

	static void StartRotate() {
		Prepare();
		isRotating = true;
	}

	static float SnapAng(float ang) {
		if (!EditorSnapSettings.gridSnapEnabled && !isSnapping) {
			return ang;
		}

		ang /= EditorSnapSettings.rotate;
		ang = Mathf.Round(ang);
		ang *= EditorSnapSettings.rotate;

		return ang;
	}

	// TO DO: fix initial spazzing when axis is updated (reset mouse start pos?)
	static void UpdateRotation() {
		Vector2 mousePos = Event.current.mousePosition;
		Vector2 center = getCenter();

		// angle between starting mouse point and current
		float ang = Vector2.Angle(mouseStart - center, mousePos - center);

		// backwards rotation
		if (Vector3.Cross(mouseStart - center, mousePos - center).z < 0) {
			ang = 360 - ang;
		}

		if (validExactNumber())
			ang = float.Parse(exactNumber);

		ang = SnapAng(ang);

		// use unity's active transform (last selected object) for local axis
		Vector3 localRight = Selection.activeTransform.right;
		Vector3 localUp = Selection.activeTransform.up;
		Vector3 localForward = Selection.activeTransform.forward;

		// handle axis restrictions and rotate
		for (int i = 0; i < selected.Length; i++) {
			// reset transformation first so the spin doesn't exponentially increase
			selected[i].rotation = origRot[i];
			selected[i].position = origPos[i];

			Vector3 axis = Vector3.zero;

			if (!onX && !onY && !onZ) {
				// rotate around camera's axis
				Camera sceneCam = SceneView.lastActiveSceneView.camera;
				axis = -sceneCam.ScreenPointToRay(center).direction;
			} else {
				// rotate around a single axis
				if (onX) {
					if (Tools.pivotRotation == PivotRotation.Global)
						axis = Vector3.right;
					else
						axis = localRight;
				}
				if (onY) {
					if (Tools.pivotRotation == PivotRotation.Global)
						axis = Vector3.up;
					else
						axis = localUp;
				}
				if (onZ) {
					if (Tools.pivotRotation == PivotRotation.Global)
						axis = Vector3.forward;
					else
						axis = localForward;
				}
			}

			Vector3 pivotPos = centerPos;
			if (Tools.pivotMode == PivotMode.Pivot)
				pivotPos = selected[i].position;

			selected[i].RotateAround(pivotPos, axis, ang);
		}
	}

	static void ConfirmRotate() {
		Save("Rotate");
		isRotating = false;
	}

	static void CancelRotate() {
		Reset();
		isRotating = false;
	}

	static void ClearRotate() {
		Prepare();
		Save("Clear Rotation");
		for (int i = 0; i < selected.Length; i++)
			selected[i].rotation = Quaternion.identity;
	}


	/* ----- SCALE ----- */
	static bool isScaling = false;
	static float origDist;

	static void StartScale() {
		Prepare();
		isScaling = true;

		Vector2 center = getCenter();
		origDist = Vector2.Distance(mouseStart, center);
	}

	static float SnapMul(float mul) {
		if (!EditorSnapSettings.gridSnapEnabled && !isSnapping) {
			return mul;
		}

		mul /= EditorSnapSettings.scale;
		mul = Mathf.Round(mul);
		mul *= EditorSnapSettings.scale;

		return mul;
	}

	static void UpdateScale() {
		Vector2 mousePos = Event.current.mousePosition;
		Vector2 center = getCenter();

		// calculate scale from distance difference
		// TO DO: handle Tools.pivotMode == PivotMode.Pivot
		float dist = Vector2.Distance(mousePos, center);
		float mul = dist / origDist;

		if (validExactNumber())
			mul = float.Parse(exactNumber);

		mul = SnapMul(mul);

		// handle axis restrictions and scale
		for (int i = 0; i < selected.Length; i++) {
			Vector3 scale = origScale[i] * mul;
			Vector3 newScale = origScale[i];

			Vector3 diff = origPos[i] - centerPos;
			Vector3 pos = (diff * mul) + centerPos;
			Vector3 newPos = origPos[i];

			if (!onX && !onY && !onZ) {
				newScale = scale;
				newPos = pos;
			} else {
				if (onX) {
					newScale.x = scale.x;
					newPos.x = pos.x;
				}
				if (onY) {
					newScale.y = scale.y;
					newPos.y = pos.y;
				}
				if (onZ) {
					newScale.z = scale.z;
					newPos.z = pos.z;
				}
			}

			selected[i].position = newPos;
			selected[i].localScale = newScale;
		}
	}

	static void ConfirmScale() {
		Save("Scale");
		isScaling = false;
	}

	static void CancelScale() {
		Reset();
		isScaling = false;
	}

	static void ClearScale() {
		Prepare();
		Save("Clear Scale");
		for (int i = 0; i < selected.Length; i++)
			selected[i].localScale = Vector3.one;
	}
}
