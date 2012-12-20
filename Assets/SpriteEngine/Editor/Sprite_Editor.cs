using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Sprite))]
[CanEditMultipleObjects]
public class Sprite_Editor:Editor {
	SerializedProperty textureProp;
	SerializedProperty lock2DProp;
	SerializedProperty divideByWidthProp;
	SerializedProperty divideByHeightProp;
	SerializedProperty uvMappingProp;
	SerializedProperty collisionProp;
	SerializedProperty collisionWidthProp;
	SerializedProperty collisionSegMultiplyerProp;
	private HOEXInspectorUndo src;
	static public Sprite[] oldSelectedSprites = new Sprite[0];
	
	static bool[] edgeExpanded = new bool[] {false,false,false,false};
	static bool expandCollisionSides = false;
	//private bool needsUndo = false;
	
	static public void SetPositionSnap(Vector3 v) {
		System.Type type = System.Type.GetType("UnityEditor.SnapSettings,UnityEditor");
		PropertyInfo propertyInfo = type.GetProperty("move");
		Vector3 originalValue = (Vector3)propertyInfo.GetValue(null,null);
		if (v != originalValue) {
			propertyInfo.SetValue(null,v,null);
		}
	}
	void OnEnable () {
		src = target as HOEXInspectorUndo;
		textureProp = serializedObject.FindProperty("_texture");
		lock2DProp = serializedObject.FindProperty("lock2D");
		divideByWidthProp = serializedObject.FindProperty("divideByWidth");
		divideByHeightProp = serializedObject.FindProperty("divideByHeight");
		uvMappingProp = serializedObject.FindProperty("uvMapping");
		collisionProp = serializedObject.FindProperty("collision");
		collisionWidthProp = serializedObject.FindProperty("collisionWidth");
		collisionSegMultiplyerProp = serializedObject.FindProperty("collisionSegMultiplyer");
	}
	public Sprite[] GetSelectedSprites() {
		List<Sprite> sprites = new List<Sprite>();
		foreach (GameObject go in Selection.gameObjects) {
			Sprite sprite = go.GetComponent<Sprite>();
			if (sprite) {
				sprites.Add(sprite);
			}
		}
		return sprites.ToArray();
	}
	
	public override void OnInspectorGUI() {
		serializedObject.Update();
		Sprite targetSprite = (Sprite)target;
		Sprite[] sprites = GetSelectedSprites();
		
		bool selectionChanged = false;
		if (sprites.Length != oldSelectedSprites.Length) {
			selectionChanged = true;
			
		}
		else {
			for (int i = 0; i < sprites.Length; i++) {
				if (sprites[i] != oldSelectedSprites[i]) {
					selectionChanged = true;
					break;
				}
			}
		}
		if (selectionChanged) {
			for (int i = 0; i < oldSelectedSprites.Length; i++) {
				if (oldSelectedSprites[i]) {
					oldSelectedSprites[i].Rebuild();
				}
			}
		}
		oldSelectedSprites = sprites;
		
		Undo.SetSnapshotTarget(src, "HOEXInspectorUndo");
		//Texture originalTex = targetSprite.texture;
		
		EditorGUILayout.Space();
		Rect globalSettingsRect = EditorGUILayout.BeginVertical();
		EditorGUILayout.Space();
		GUI.Box(globalSettingsRect,"");
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("0.125",GUILayout.Width(45))) {
			SpriteWorldManager.current.gridSize.x = 0.125f;
		}
		if (GUILayout.Button("0.25",GUILayout.Width(45))) {
			SpriteWorldManager.current.gridSize.x = 0.25f;
		}
		if (GUILayout.Button("0.5",GUILayout.Width(45))) {
			SpriteWorldManager.current.gridSize.x = 0.5f;
		}
		if (GUILayout.Button("1",GUILayout.Width(45))) {
			SpriteWorldManager.current.gridSize.x = 1;
		}
		if (GUILayout.Button("2",GUILayout.Width(45))) {
			SpriteWorldManager.current.gridSize.x = 2;
		}
		if (GUILayout.Button("4",GUILayout.Width(45))) {
			SpriteWorldManager.current.gridSize.x = 4;
		}
		if (GUILayout.Button("8",GUILayout.Width(45))) {
			SpriteWorldManager.current.gridSize.x = 8;
		}
		EditorGUILayout.EndHorizontal();
		SpriteWorldManager.current.gridSize.x = EditorGUILayout.FloatField("Global Grid",SpriteWorldManager.current.gridSize.x);
		SpriteWorldManager.current.gridSize.x = Mathf.Max(SpriteWorldManager.current.gridSize.x,0.0f);
		SpriteWorldManager.current.gridSize.y = SpriteWorldManager.current.gridSize.x;
		SpriteWorldManager.current.gridSize.z = SpriteWorldManager.current.gridSize.x;
		SetPositionSnap(SpriteWorldManager.current.gridSize);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Snap Position")) {
			foreach (Sprite sprite in sprites) {
				sprite.transform.position = SnapVector3(sprite.transform.position,SpriteWorldManager.current.gridSize);
			}
		}
		if (GUILayout.Button("Reset Rotation")) {
			foreach (Sprite sprite in sprites) {
				sprite.transform.rotation = Quaternion.identity;
			}
		}
		if (GUILayout.Button("Reset Scale")) {
			foreach (Sprite sprite in sprites) {
				sprite.transform.localScale = Vector3.one;
			}
		}
		EditorGUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		SpriteWorldManager.current.editorUpdateRate = EditorGUILayout.IntSlider("Update Mode",SpriteWorldManager.current.editorUpdateRate,0,2);
		if (SpriteWorldManager.current.editorUpdateRate == 2) {
			GUILayout.Label("Constant",GUILayout.Width(60));
		}
		if (SpriteWorldManager.current.editorUpdateRate == 1) {
			GUILayout.Label("Limited",GUILayout.Width(60));
		}
		if (SpriteWorldManager.current.editorUpdateRate == 0) {
			GUILayout.Label("None",GUILayout.Width(60));
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();
		
		Rect textureFieldRect = globalSettingsRect;
		textureFieldRect.y += globalSettingsRect.height+4;
		//Debug.Log(textureFieldRect);
		//GUILayout.BeginArea(textureFieldRect);
		EditorGUI.PropertyField(textureFieldRect,textureProp, new GUIContent("Texture"));
		
		Vector2 originalAnchor = targetSprite.anchor;
		Rect anchorRect = textureFieldRect;
		anchorRect.x += 50;
		anchorRect.width = 75;
		anchorRect.height = 75;
		for (int x = 0; x < 3; x++) {
			for (int y = 0; y < 3; y++) {
				float buttonSize = anchorRect.height/3.0f;
				Rect buttonRect = anchorRect;
				buttonRect.x += x*buttonSize;
				buttonRect.y += y*buttonSize;
				buttonRect.width = buttonSize;
				buttonRect.height = buttonSize;
				if (GUI.Button(buttonRect,"")) {
					Vector2 anchor = new Vector2((float)x/2.0f,1.0f-((float)y/2.0f));
					targetSprite.anchor = anchor;
				}
			}
		}
		if (originalAnchor != targetSprite.anchor) {
			foreach (Sprite sprite in sprites) {
				if (sprite.anchor != targetSprite.anchor) {
					sprite.anchor = targetSprite.anchor;
				}
			}
		}
		
		GUILayout.Space(95);
		
		EditorGUILayout.PropertyField(collisionProp, new GUIContent("Collision"));
		EditorGUILayout.PropertyField(collisionSegMultiplyerProp, new GUIContent("Collision Segment Scaler"));
		EditorGUILayout.PropertyField(collisionWidthProp, new GUIContent("Collision Width"));
		
		GUI.color = new Color(0,0,0,0);
		if (GUILayout.Button("","Foldout")) {
			expandCollisionSides = !expandCollisionSides;
		}
		GUI.color = new Color(1,1,1,1);
		
		expandCollisionSides = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(),expandCollisionSides,"Collision Sides");
		if (expandCollisionSides) {
			EditorGUI.indentLevel = EditorGUI.indentLevel+1;
			bool original;
			//Top
			original = targetSprite.useCollisionTop;
			targetSprite.useCollisionTop = EditorGUILayout.Toggle("Top",targetSprite.useCollisionTop);
			if (original != targetSprite.useCollisionTop) {
				foreach (Sprite sprite in sprites) {
					sprite.useCollisionTop = targetSprite.useCollisionTop;
				}
			}
			//Bottom
			original = targetSprite.useCollisionBottom;
			targetSprite.useCollisionBottom = EditorGUILayout.Toggle("Bottom",targetSprite.useCollisionBottom);
			if (original != targetSprite.useCollisionBottom) {
				foreach (Sprite sprite in sprites) {
					sprite.useCollisionBottom = targetSprite.useCollisionBottom;
				}
			}
			//Left
			original = targetSprite.useCollisionLeft;
			targetSprite.useCollisionLeft = EditorGUILayout.Toggle("Left",targetSprite.useCollisionLeft);
			if (original != targetSprite.useCollisionLeft) {
				foreach (Sprite sprite in sprites) {
					sprite.useCollisionLeft = targetSprite.useCollisionLeft;
				}
			}
			//Right
			original = targetSprite.useCollisionRight;
			targetSprite.useCollisionRight = EditorGUILayout.Toggle("Right",targetSprite.useCollisionRight);
			if (original != targetSprite.useCollisionRight) {
				foreach (Sprite sprite in sprites) {
					sprite.useCollisionRight = targetSprite.useCollisionRight;
				}
			}
			EditorGUI.indentLevel = EditorGUI.indentLevel-1;
		}
		
		float originalPixelsPerUnit = targetSprite.pixelsPerUnit;
		targetSprite.pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit",targetSprite.pixelsPerUnit);
		if (originalPixelsPerUnit != targetSprite.pixelsPerUnit) {
			foreach (Sprite sprite in sprites) {
				if (sprite.pixelsPerUnit != targetSprite.pixelsPerUnit) {
					sprite.pixelsPerUnit = targetSprite.pixelsPerUnit;
				}
			}
		}
		
		Vector2 originalSize = targetSprite.size;
		Vector2 size = targetSprite.size;
		EditorGUILayout.BeginHorizontal();
		size.x = EditorGUILayout.FloatField("Width",size.x);
		if (GUILayout.Button("Snap",GUILayout.Width(50)) && SpriteWorldManager.current.gridSize.x != 0.0f) {
			size.x = Mathf.Round(size.x/SpriteWorldManager.current.gridSize.x)*SpriteWorldManager.current.gridSize.x;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		size.y = EditorGUILayout.FloatField("Height",size.y);
		if (GUILayout.Button("Snap",GUILayout.Width(50)) && SpriteWorldManager.current.gridSize.y != 0.0f) {
			size.y = Mathf.Round(size.y/SpriteWorldManager.current.gridSize.y)*SpriteWorldManager.current.gridSize.y;
		}
		EditorGUILayout.EndHorizontal();
		targetSprite.size = size;
		if (originalSize != targetSprite.size) {
			foreach (Sprite sprite in sprites) {
				if (sprite.size != targetSprite.size) {
					sprite.size = targetSprite.size;
				}
			}
		}
		
		int originalXSegs = targetSprite.widthSegs;
		int originalYSegs = targetSprite.heightSegs;
		int xSegs = EditorGUILayout.IntField("Width Segments",targetSprite.widthSegs);
		int ySegs = EditorGUILayout.IntField("Height Segments",targetSprite.heightSegs);
		xSegs = Mathf.Max(xSegs,1);
		ySegs = Mathf.Max(ySegs,1);
		if (xSegs != targetSprite.widthSegs) {
			targetSprite.widthSegs = xSegs;
		}
		if (ySegs != targetSprite.heightSegs) {
			targetSprite.heightSegs = ySegs;
		}
		
		foreach (Sprite sprite in sprites) {
			if (originalXSegs != targetSprite.widthSegs) {
				if (sprite.widthSegs != targetSprite.widthSegs) {
					sprite.widthSegs = targetSprite.widthSegs;
				}
			}
			if (originalYSegs != targetSprite.heightSegs) {
				if (sprite.heightSegs != targetSprite.heightSegs) {
					sprite.heightSegs = targetSprite.heightSegs;
				}
			}
		}
		
		
		EditorGUILayout.PropertyField(lock2DProp, new GUIContent("Lock 2D"));
		EditorGUILayout.PropertyField(divideByWidthProp, new GUIContent("Divide By Width"));
		EditorGUILayout.PropertyField(divideByHeightProp, new GUIContent("Divide By Height"));
		
		Color originalColor = targetSprite.color;
		float originalSaturation = targetSprite.saturation;
		float originalBrightness = targetSprite.brightness;
		float originalContrast = targetSprite.contrast;
		targetSprite.color = EditorGUILayout.ColorField("Color",targetSprite.color);
		targetSprite.saturation = EditorGUILayout.Slider("Saturation",targetSprite.saturation,0.0f,2.0f);
		targetSprite.brightness = EditorGUILayout.Slider("Brightness",targetSprite.brightness,0.0f,4.0f);
		targetSprite.contrast = EditorGUILayout.Slider("Contrast",targetSprite.contrast,0.0f,4.0f);
		if (Event.current.control) {
			if (originalSaturation != targetSprite.saturation) {
				targetSprite.saturation = Mathf.Round(targetSprite.saturation/0.25f)*0.25f;
			}
			if (originalBrightness != targetSprite.brightness) {
				targetSprite.brightness = Mathf.Round(targetSprite.brightness/0.25f)*0.25f;
			}
			if (originalContrast != targetSprite.contrast) {
				targetSprite.contrast = Mathf.Round(targetSprite.contrast/0.25f)*0.25f;
			}
		}
		foreach (Sprite sprite in sprites) {
			if (originalColor != targetSprite.color) {
				if (sprite.color != targetSprite.color) {
					sprite.color = targetSprite.color;
				}
			}
			if (originalSaturation != targetSprite.saturation) {
				if (sprite.saturation != targetSprite.saturation) {
					sprite.saturation = targetSprite.saturation;
				}
			}
			if (originalBrightness != targetSprite.brightness) {
				if (sprite.brightness != targetSprite.brightness) {
					sprite.brightness = targetSprite.brightness;
				}
			}
			if (originalContrast != targetSprite.contrast) {
				if (sprite.contrast != targetSprite.contrast) {
					sprite.contrast = targetSprite.contrast;
				}
			}
		}
		
		EditorGUILayout.PropertyField(uvMappingProp, new GUIContent("UV Mapping"));
		
		Vector2 originalUVOffset = targetSprite.uvOffset;
		Vector2 uvOffset = targetSprite.uvOffset;
		uvOffset.x = EditorGUILayout.FloatField("UV Offset X",uvOffset.x);
		uvOffset.y = EditorGUILayout.FloatField("UV Offset Y",uvOffset.y);
		targetSprite.uvOffset = uvOffset;
		
		if (originalUVOffset != targetSprite.uvOffset) {
			foreach (Sprite sprite in sprites) {
				if (sprite.uvOffset != targetSprite.uvOffset) {
					sprite.uvOffset = targetSprite.uvOffset;
				}
			}
		}
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Reset Points: ");
		if (GUILayout.Button("1")) {
			targetSprite.localPoint0 = Vector3.zero;
		}
		if (GUILayout.Button("2")) {
			targetSprite.localPoint1 = Vector3.zero;
		}
		if (GUILayout.Button("3")) {
			targetSprite.localPoint2 = Vector3.zero;
		}
		if (GUILayout.Button("4")) {
			targetSprite.localPoint3 = Vector3.zero;
		}
		if (GUILayout.Button("All")) {
			targetSprite.localPoint0 = Vector3.zero;
			targetSprite.localPoint1 = Vector3.zero;
			targetSprite.localPoint2 = Vector3.zero;
			targetSprite.localPoint3 = Vector3.zero;
		}
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.Space();
		Rect edgesRect = EditorGUILayout.BeginVertical();
		GUI.Box(edgesRect,"");
		
		GUILayout.Label("Edges...");
		EditorGUI.indentLevel = 1;
		DrawSplineInfo("Top",targetSprite.splineTop,0);
		DrawSplineInfo("Bottom",targetSprite.splineBottom,1);
		DrawSplineInfo("Left",targetSprite.splineLeft,2);
		DrawSplineInfo("Right",targetSprite.splineRight,3);
		EditorGUI.indentLevel = 0;
		EditorGUILayout.Space();
		EditorGUILayout.EndVertical();
		
		serializedObject.ApplyModifiedProperties();
		
		//Debug.Log(Event.current.type+" "+needsUndo);
		//EditorGUILayout.ObjectField("Texture",textureProp,typeof(Texture),true);
        if (GUI.changed) {
            Undo.CreateSnapshot();
            Undo.RegisterSnapshot();
        }
        Undo.ClearSnapshotTarget();
		
		if (SpriteWorldManager.current.editorUpdateRate > 0) {
			foreach (Sprite sprite in sprites) {
				sprite.Rebuild();
			}
		}
	}
	
	public void DrawSplineInfo(string title, BezierSpline spline, int index) {
		if (spline == null) return;
		
		GUI.color = new Color(0,0,0,0);
		if (GUILayout.Button("","Foldout")) {
			edgeExpanded[index] = !edgeExpanded[index];
		}
		GUI.color = new Color(1,1,1,1);
		
		edgeExpanded[index] = EditorGUI.Foldout(GUILayoutUtility.GetLastRect(),edgeExpanded[index],title);
		
		if (!edgeExpanded[index]) {
			return;
		}
		
		Rect rect = EditorGUILayout.BeginVertical();
		if (!spline.linear) {
			rect.x += 4;
			rect.width -= 4;
		}
		GUI.Box(rect,"");
		GUILayout.Space(4);

		
		EditorGUI.indentLevel = EditorGUI.indentLevel+1;
		spline.linear = EditorGUILayout.Toggle("Linear",spline.linear);
		if (!spline.linear) {
			spline.useCustomDistance = EditorGUILayout.Toggle("Custom Distance",spline.useCustomDistance);
			if (spline.useCustomDistance) {
				GUILayout.BeginHorizontal();
				spline.customDistance0 = EditorGUILayout.FloatField("P1 Distance",spline.customDistance0);
				if (GUILayout.Button("Reset",GUILayout.MaxWidth(50))) {
					spline.customDistance0 = 1.0f/3.0f;
				}
				if (GUILayout.Button("O",GUILayout.MaxWidth(25))) {
					spline.customDistance0 = 0.5f-((0.5f-(1.0f/3.0f))*0.5f);
				}
				GUILayout.EndHorizontal();
				
				GUILayout.BeginHorizontal();
				spline.customDistance1 = EditorGUILayout.FloatField("P2 Distance",spline.customDistance1);
				if (GUILayout.Button("Reset",GUILayout.MaxWidth(50))) {
					spline.customDistance1 = 1.0f/3.0f;
				}
				if (GUILayout.Button("O",GUILayout.MaxWidth(25))) {
					spline.customDistance1 = 0.5f-((0.5f-(1.0f/3.0f))*0.5f);
				}
				GUILayout.EndHorizontal();
			}
			
			EditorGUILayout.Space();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Reset Tangents")) {
				spline.SetLinearTangents();
			}
			GUILayout.EndHorizontal();
			spline.localNormal0 = EditorGUILayout.Vector3Field("P1 Tangent",spline.localNormal0);
			spline.localNormal1 = EditorGUILayout.Vector3Field("P2 Tangent",spline.localNormal1);
		}
		EditorGUI.indentLevel = EditorGUI.indentLevel-1;
		GUILayout.Space(4);
		EditorGUILayout.EndVertical();
		
	}
	
	public void OnSceneGUI() {
		Sprite[] sprites = GetSelectedSprites();
		Sprite targetSprite = (Sprite)target;
		if (targetSprite.splineTop != null) DrawEditBezierPatch(targetSprite.splineTop,targetSprite.actualWidthSegs);
		if (targetSprite.splineBottom != null) DrawEditBezierPatch(targetSprite.splineBottom,targetSprite.actualWidthSegs);
		if (targetSprite.splineLeft != null) DrawEditBezierPatch(targetSprite.splineLeft,targetSprite.actualHeightSegs);
		if (targetSprite.splineRight != null) DrawEditBezierPatch(targetSprite.splineRight,targetSprite.actualHeightSegs);
		
		Vector3 fixedPoint;
		float distance;
		Camera cam = SceneView.currentDrawingSceneView.camera;
		
		Vector3 original0 = targetSprite.localPoint0;
		Vector3 original1 = targetSprite.localPoint1;
		Vector3 original2 = targetSprite.localPoint2;
		Vector3 original3 = targetSprite.localPoint3;
			
		//Point handles
		distance = Vector3.Distance(targetSprite.point0,cam.transform.position);
		fixedPoint = Handles.FreeMoveHandle(targetSprite.point0,Quaternion.identity,0.01f*distance,Vector3.zero,Handles.RectangleCap);
		Handles.Label(targetSprite.point0," 1");
		if (targetSprite.lock2D) {
			fixedPoint = targetSprite.transform.InverseTransformPoint(fixedPoint);
			fixedPoint.z = 0.0f;
			fixedPoint = targetSprite.transform.TransformPoint(fixedPoint);
		}
		targetSprite.point0 = fixedPoint;
		targetSprite.localPoint0 = SnapVector3(targetSprite.localPoint0,SpriteWorldManager.current.smallestSpritePointAccuracy);
		
		distance = Vector3.Distance(targetSprite.point1,cam.transform.position);
		fixedPoint = Handles.FreeMoveHandle(targetSprite.point1,Quaternion.identity,0.01f*distance,Vector3.zero,Handles.RectangleCap);
		Handles.Label(targetSprite.point1," 2");
		if (targetSprite.lock2D) {
			fixedPoint = targetSprite.transform.InverseTransformPoint(fixedPoint);
			fixedPoint.z = 0.0f;
			fixedPoint = targetSprite.transform.TransformPoint(fixedPoint);
		}
		targetSprite.point1 = fixedPoint;
		targetSprite.localPoint1 = SnapVector3(targetSprite.localPoint1,SpriteWorldManager.current.smallestSpritePointAccuracy);
		
		distance = Vector3.Distance(targetSprite.point2,cam.transform.position);
		fixedPoint = Handles.FreeMoveHandle(targetSprite.point2,Quaternion.identity,0.01f*distance,Vector3.zero,Handles.RectangleCap);
		Handles.Label(targetSprite.point2," 3");
		if (targetSprite.lock2D) {
			fixedPoint = targetSprite.transform.InverseTransformPoint(fixedPoint);
			fixedPoint.z = 0.0f;
			fixedPoint = targetSprite.transform.TransformPoint(fixedPoint);
		}
		targetSprite.point2 = fixedPoint;
		targetSprite.localPoint2 = SnapVector3(targetSprite.localPoint2,SpriteWorldManager.current.smallestSpritePointAccuracy);
		
		distance = Vector3.Distance(targetSprite.point3,cam.transform.position);
		fixedPoint = Handles.FreeMoveHandle(targetSprite.point3,Quaternion.identity,0.01f*distance,Vector3.zero,Handles.RectangleCap);
		Handles.Label(targetSprite.point3," 4");
		if (targetSprite.lock2D) {
			fixedPoint = targetSprite.transform.InverseTransformPoint(fixedPoint);
			fixedPoint.z = 0.0f;
			fixedPoint = targetSprite.transform.TransformPoint(fixedPoint);
		}
		targetSprite.point3 = fixedPoint;
		targetSprite.localPoint3 = SnapVector3(targetSprite.localPoint3,SpriteWorldManager.current.smallestSpritePointAccuracy);
		
		//if (original0 != targetSprite.localPoint0) {
		//	needsUndo = true;
		//}
		//if (original1 != targetSprite.localPoint1) {
		//	needsUndo = true;
		//}
		//if (original2 != targetSprite.localPoint2) {
		//	needsUndo = true;
		//}
		//if (original3 != targetSprite.localPoint3) {
		//	needsUndo = true;
		//}
		
		if (Event.current.control) {
			if (original0 != targetSprite.localPoint0) {
				targetSprite.localPoint0 = SnapVector3(targetSprite.localPoint0,SpriteWorldManager.current.gridSize);
			}
			if (original1 != targetSprite.localPoint1) {
				targetSprite.localPoint1 = SnapVector3(targetSprite.localPoint1,SpriteWorldManager.current.gridSize);
			}
			if (original2 != targetSprite.localPoint2) {
				targetSprite.localPoint2 = SnapVector3(targetSprite.localPoint2,SpriteWorldManager.current.gridSize);
			}
			if (original3 != targetSprite.localPoint3) {
				targetSprite.localPoint3 = SnapVector3(targetSprite.localPoint3,SpriteWorldManager.current.gridSize);
			}
		}
		if (SpriteWorldManager.current.editorUpdateRate > 1) {
			foreach (Sprite sprite in sprites) {
				sprite.Rebuild();
			}
		}
	}
	
	Vector3 SnapVector3(Vector3 v, float snapSize) {
		return SnapVector3(v,new Vector3(snapSize,snapSize,snapSize));
	}
	Vector3 SnapVector3(Vector3 v, Vector3 snapSize) {
		if (snapSize.x != 0.0f) {
			v.x = Mathf.Round(v.x/snapSize.x)*snapSize.x;
		}
		if (snapSize.y != 0.0f) {
			v.y = Mathf.Round(v.y/snapSize.y)*snapSize.y;
		}
		if (snapSize.z != 0.0f) {
			v.z = Mathf.Round(v.z/snapSize.z)*snapSize.z;
		}
		return v;
	}
	
	void DrawEditBezierPatch(BezierSpline patch, int segs) {
		Sprite targetSprite = (Sprite)target;
		
		Vector3 fixedPoint;
		float distance;
		Camera cam = SceneView.currentDrawingSceneView.camera;
		

		//Tangent Handles
		if (!patch.linear) {
			Vector3 originalTangent0 = patch.tangetPos0;
			Vector3 originalTangent1 = patch.tangetPos1;
			distance = Vector3.Distance(patch.tangetPos0,cam.transform.position);
			fixedPoint = Handles.FreeMoveHandle(patch.tangetPos0,Quaternion.identity,0.01f*distance,Vector3.zero,Handles.RectangleCap);
			//Debug.Log(fixedPoint);

			if (originalTangent0 != fixedPoint) {
				//needsUndo = true;
				if (targetSprite.lock2D) {
					fixedPoint = targetSprite.transform.InverseTransformPoint(fixedPoint);
					fixedPoint.z = 0.0f;
					fixedPoint = targetSprite.transform.TransformPoint(fixedPoint);
				}
				patch.tangetPos0 = fixedPoint;
				if (Event.current.control) {
					patch.localNormal0 = SnapVector3(patch.localNormal0,SpriteWorldManager.current.gridSize*0.5f);
					patch.localNormal0.Normalize();
				}
				if (targetSprite.lock2D) {
					patch.localNormal0.z = 0.0f;
				}
			}
			
			distance = Vector3.Distance(patch.tangetPos1,cam.transform.position);
			fixedPoint = Handles.FreeMoveHandle(patch.tangetPos1,Quaternion.identity,0.01f*distance,Vector3.zero,Handles.RectangleCap);
			if (originalTangent1 != fixedPoint) {
				//needsUndo = true;
				if (targetSprite.lock2D) {
					fixedPoint = targetSprite.transform.InverseTransformPoint(fixedPoint);
					fixedPoint.z = 0.0f;
					fixedPoint = targetSprite.transform.TransformPoint(fixedPoint);
				}
				patch.tangetPos1 = fixedPoint;
				if (Event.current.control) {
					patch.localNormal1 = SnapVector3(patch.localNormal1,SpriteWorldManager.current.gridSize*0.5f);
					patch.localNormal0.Normalize();
				}
				if (targetSprite.lock2D) {
					patch.localNormal1.z = 0.0f;
				}
			}
		}
		
		DrawBezierPatch(patch,segs);
	}
	void DrawBezierPatch(BezierSpline patch,int segs) {
		Vector3 lastPoint = patch.point0;
		//Handles.DrawWireSphere(patch.point0,0.003f*Vector3.Distance(Camera.current.transform.position,patch.point0));
		//Handles.DrawWireSphere(patch.point1,0.003f*Vector3.Distance(Camera.current.transform.position,patch.point1));
		if (!patch.linear) {
			Handles.color = Color.red;
			Handles.DrawLine(patch.point0,patch.tangetPos0);
			Handles.DrawLine(patch.point1,patch.tangetPos1);
		}
		
		bool colorSwap = false;
		for (int i = 0; i < segs+1; i++) {
			float t = (float)i/(float)segs;
			
			Vector3 p = patch.GetPoint(t);
			
			if (colorSwap) {
				Handles.color =  new Color(1.0f,1.0f,1.0f,1.0f);
			}
			else {
				Handles.color =  new Color(0.2f,0.2f,0.2f,1.0f);
			}
			
			Handles.DrawLine(lastPoint,p);
			lastPoint = p;
			colorSwap = !colorSwap;
		}
		Handles.color = Color.white;
	}
}

