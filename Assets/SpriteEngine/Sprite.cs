using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum SpriteUVMapping {LocalSpace,LocalSpaceProjected,WorldSpace,WorldSpaceProjected}
public enum SpriteCollision {None,Box,Mesh,MeshConvex}

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Sprite:MonoBehaviour {
	static public Dictionary<Texture,Material> materialDict = new Dictionary<Texture,Material>();
	public BezierSpline splineTop;
	public BezierSpline splineBottom;
	public BezierSpline splineLeft;
	public BezierSpline splineRight;
	
	public bool useCollisionTop = true;
	public bool useCollisionBottom = true;
	public bool useCollisionLeft = true;
	public bool useCollisionRight = true;
	
	public bool lock2D = true;
	
	public float collisionWidth = 4.0f;
	public SpriteUVMapping uvMapping = SpriteUVMapping.LocalSpace;
	
	public bool divideByWidth = false;
	public bool divideByHeight = false;
	public SpriteCollision collision = SpriteCollision.None;
	public float collisionSegMultiplyer = 1.0f;
	
	public int widthSegs = 1;
	public int heightSegs = 1;
	
	public int actualWidthSegs {
		get {
			if (!divideByWidth) {
				return widthSegs;
			}
			return Mathf.CeilToInt(Mathf.Max(size.x,1))*widthSegs;
		}
	}
	public int actualHeightSegs {
		get {
			if (!divideByHeight) {
				return heightSegs;
			}
			return Mathf.CeilToInt(Mathf.Max(size.y,1))*heightSegs;
		}
	}
	
	public Vector3 squarePoint0 {
		get {
			return  transform.TransformPoint(new Vector3(0,0,0));
		}
	}
	public Vector3 squarePoint1 {
		get {
			return transform.TransformPoint(new Vector3(0,size.y,0));
		}
	}
	public Vector3 squarePoint2 {
		get {
			return transform.TransformPoint(new Vector3(size.x,size.y,0));
		}
	}
	public Vector3 squarePoint3 {
		get {
			return transform.TransformPoint(new Vector3(size.x,0,0));
		}
	}
	
	public Vector3 localSquarePoint0 {
		get {
			return  new Vector3(0,0,0);
		}
	}
	public Vector3 localSquarePoint1 {
		get {
			return new Vector3(0,size.y,0);
		}
	}
	public Vector3 localSquarePoint2 {
		get {
			return new Vector3(size.x,size.y,0);
		}
	}
	public Vector3 localSquarePoint3 {
		get {
			return new Vector3(size.x,0,0);
		}
	}
	
	public Vector3 localPoint0;
	public Vector3 point0 {
		get {
			return transform.TransformPoint(localSquarePoint0+localPoint0);
		}
		set {
			if (splineBottom != null) splineBottom.point0 = value;
			if (splineLeft != null) splineLeft.point0 = value;
			localPoint0 = transform.InverseTransformPoint(value)-localSquarePoint0;
		}
	}
	public Vector3 localPoint1;
	public Vector3 point1 {
		get {
			return transform.TransformPoint(localSquarePoint1+localPoint1);
		}
		set {
			if (splineTop != null) splineTop.point0 = value;
			if (splineLeft != null) splineLeft.point1 = value;
			localPoint1 = transform.InverseTransformPoint(value)-localSquarePoint1;
		}
	}
	public Vector3 localPoint2;
	public Vector3 point2 {
		get {
			return transform.TransformPoint(localSquarePoint2+localPoint2);
		}
		set {
			if (splineTop != null) splineTop.point1 = value;
			if (splineRight != null) splineRight.point1 = value;
			localPoint2 = transform.InverseTransformPoint(value)-localSquarePoint2;
		}
	}
	public Vector3 localPoint3;
	public Vector3 point3 {
		get {
			return transform.TransformPoint(localSquarePoint3+localPoint3);
		}
		set {
			if (splineRight != null) splineRight.point0 = value;
			if (splineBottom != null) splineBottom.point1 = value;
			localPoint3 = transform.InverseTransformPoint(value)-localSquarePoint3;
		}
	}
	
	static private Material _baseMaterial;
	static public Material baseMaterial {
		get {
			if (!_baseMaterial) {
				_baseMaterial = (Material)Resources.Load("Sprite_AlphaBlend");
			}
			return _baseMaterial;
		}
		
	}
	//[System.NonSerialized]
	public Material material {
		get {
			return meshRenderer.sharedMaterial;
		}
		set {
			meshRenderer.sharedMaterial = value;
		}
	}
	
	[SerializeField]
	private Color _color = Color.white;
	public Color color {
		get {
			return _color;
		}
		set {
			_color = value;
			if (material) {
				material.color = _color;
			}
		}
	}
	[SerializeField]
	private float _saturation = 1.0f;
	public float saturation {
		get {
			return _saturation;
		}
		set {
			_saturation = value;
			if (material) {
				material.SetFloat("_Saturation",_saturation);
			}
		}
	}
	[SerializeField]
	private float _brightness = 1.0f;
	public float brightness {
		get {
			return _brightness;
		}
		set {
			_brightness = value;
			if (material) {
				material.SetFloat("_Brightness",_brightness);
			}
		}
	}
	[SerializeField]
	private float _contrast = 1.0f;
	public float contrast {
		get {
			return _contrast;
		}
		set {
			_contrast = value;
			if (material) {
				material.SetFloat("_Contrast",_contrast);
			}
		}
	}
	
	//[System.NonSerialized]
	public Mesh mesh;
	//[System.NonSerialized]
	public Mesh collisionMesh;
	
	[SerializeField]
	private float _pixelsPerUnit = 64.0f;
	public float pixelsPerUnit {
		get {
			return _pixelsPerUnit;
		}
		set {
			if (_pixelsPerUnit != value) {
				_pixelsPerUnit = value;
				RebuildMesh();
			}
		}
	}
	
	[SerializeField]
	private Vector2 _uvOffset = new Vector2(0,0);
	public Vector2 uvOffset {
		get {
			return _uvOffset;
		}
		set {
			if (_uvOffset != value) {
				_uvOffset = value;
				RebuildMesh();
			}
		}
	}
	
	[SerializeField]
	private Vector2 _size = new Vector2(1,1);
	public Vector2 size {
		get {
			return _size;
		}
		set {
			if (_size != value) {
				_size = value;
				RebuildMesh();
			}
		}
	}
	
	[SerializeField]
	private Vector2 _anchor = new Vector2(0.0f,0.0f);
	public Vector2 anchor {
		get {
			return _anchor;
		}
		set {
			if (_anchor != value) {
				_anchor = value;
				RebuildMesh();
			}
		}
	}
	
	private MeshRenderer _meshRenderer;
	public MeshRenderer meshRenderer {
		get {
			if (!_meshRenderer) {
				_meshRenderer = gameObject.GetComponent<MeshRenderer>();
			}
			return _meshRenderer;
		}
	}
	private MeshFilter _meshFilter;
	public MeshFilter meshFilter {
		get {
			if (!_meshFilter) {
				_meshFilter = gameObject.GetComponent<MeshFilter>();
			}
			return _meshFilter;
		}
	}
	public Texture _texture;
	public Texture texture {
		get {
			return _texture;
		}
		set {
			_texture = value;
			RebuildMesh();
			CreateMaterial();
		}
	}
	
	public Rect localRect {
		get {
			float xAnchorOffset = size.x*anchor.x;
			float yAnchorOffset = size.y*anchor.y;
			Rect rect = new Rect(-xAnchorOffset,-yAnchorOffset,size.x,size.y);
			return rect;
		}
	}
	
	public Rect uvRect {
		get {
			float xUV = size.x;
			float yUV = size.y;
			if (texture) {
				xUV = (pixelsPerUnit/texture.width)*size.x;
				yUV = (pixelsPerUnit/texture.height)*size.y;
			}
			Rect rect = new Rect(0,0,xUV,yUV);
			return rect;
		}
	}
	
	public void Awake() {
		//mesh = null;
	}
	public void Start() {
		if (Application.isPlaying) {
			this.enabled = false;
			return;
		}
		Rebuild();
	}
	
	public void OnEnable() {
		//Rebuild();
	}
	
	public void Rebuild() {
		
		//splineTop = null;
		//splineBottom = null;
		//splineLeft = null;
		//splineRight = null;
		
		if (splineTop == null) {
			splineTop = new BezierSpline();
			splineTop.transform = transform;
			UpdatePoints();
			splineTop.SetLinearTangents();
		}
		if (splineBottom == null) {
			splineBottom = new BezierSpline();
			splineBottom.transform = transform;
			UpdatePoints();
			splineBottom.SetLinearTangents();
		}
		if (splineLeft == null) {
			splineLeft = new BezierSpline();
			splineLeft.transform = transform;
			UpdatePoints();
			splineLeft.SetLinearTangents();
		}
		if (splineRight == null) {
			splineRight = new BezierSpline();
			splineRight.transform = transform;
			UpdatePoints();
			splineRight.SetLinearTangents();
		}
		
		//return;
		CreateMaterial();
		
		RebuildMesh();
		
		if (collision == SpriteCollision.None) {
			if (collider) {
				DestroyImmediate(collider);
			}
		}
		if (collision == SpriteCollision.Box) {
			//Debug.Log(anchor);
			RebuildBoxCollision();
		}
		if (collision == SpriteCollision.Mesh || collision == SpriteCollision.MeshConvex) {
			RebuildCollisionMesh();
		}
	}
	
	private void UpdatePoints() {
		point0 = point0;
		point1 = point1;
		point2 = point2;
		point3 = point3;
	}
	
	public void CreateMaterial() {
		CreateMaterial(false);
	}
	public void CreateMaterial(bool verifyOnly) {
		if (!texture) return;
		
		//if (baseMaterial == null) {
		//	baseMaterial = (Material)Resources.Load("Sprite_AlphaBlend");
		//}
		
		Material foundMat = null;
		
		materialDict.TryGetValue(texture,out foundMat);
		if (foundMat) {
			material = foundMat;
			//Debug.Log("NewMat "+texture);
			meshRenderer.material = material;
			return;
		}
		
		
		//if (material && material.shader != baseMaterial.shader) {
		//	DestroyImmediate(material);
		//}
		//if (material) {
		//	DestroyImmediate(material);
		//}
		
		//if (material == null) {
		//	material = (Material)Instantiate(baseMaterial);
		//}
		material = (Material)Instantiate(baseMaterial);
		material.mainTexture = texture;
		material.SetColor("_Color",color);
		material.SetFloat("_Saturation",saturation);
		material.SetFloat("_Brightness",brightness);
		material.SetFloat("_Contrast",contrast);
		
		if (materialDict.Keys.Contains(texture)) {
			materialDict[texture] = material;
		}
		else {
			materialDict.Add(texture,material);
		}
		
		meshRenderer.material = material;
		//if (!verifyOnly) {
		//	meshRenderer.sharedMaterial = material;
		//}
	}
	
	public void SetVertexColors(Color color) {
		Vector3[] vertices = mesh.vertices;
		Color[] colors = new Color[vertices.Length];
		for (int i = 0; i < colors.Length; i++) {
			colors[i] = color;
		}
		mesh.colors = colors;
	}
	
	public Mesh RebuildMesh() {
		if (mesh) {
			DestroyImmediate(mesh);
		}
		mesh = new Mesh();
		
		if (size.x == 0.0f || size.y == 0.0f) return null;
		
		int xSegs = actualWidthSegs;
		int ySegs = actualHeightSegs;
		Mesh[] meshes = new Mesh[(xSegs)*(ySegs)];
		
		Rect[,] quadRects = DivideRect(localRect,xSegs,ySegs);
		Rect[,] quadUVRects = DivideRect(uvRect,xSegs,ySegs);
		
        int[] triangles = new int[] {
            0, 1, 2,
            2, 3, 0,
        };
		
		Rect lr = localRect;
		float worldSpaceUVFactorX = size.x;
		float worldSpaceUVFactorY = size.y;
		if (texture) {
			worldSpaceUVFactorX = (pixelsPerUnit/texture.width);//*size.x;
			worldSpaceUVFactorY = (pixelsPerUnit/texture.height);//*size.y;
		}
		//Debug.Log(anchor.x*lr.width*worldSpaceUVFactorX);
		for (int x = 0; x < xSegs; x++) {
			for (int y = 0; y < ySegs; y++) {
				if (uvMapping == SpriteUVMapping.WorldSpace) {
					quadUVRects[x,y].x -= anchor.x*lr.width*worldSpaceUVFactorX;
					quadUVRects[x,y].y -= anchor.y*lr.height*worldSpaceUVFactorY;
					quadUVRects[x,y].x += transform.position.x*worldSpaceUVFactorX;
					quadUVRects[x,y].y += transform.position.y*worldSpaceUVFactorY;
				}
				
				Mesh newMesh = new Mesh();
				Vector3[] vertices = GetQuadPointsFromRect(quadRects[x,y]);
				
				for (int i = 0; i < vertices.Length; i++) {
					Vector3 ratio = vertices[i];
					ratio.x /= size.x;
					ratio.y /= size.y;
					
					Vector3 rightTangent0Local = BezierSpline.GetThirdTrianglePoint(splineRight.localPoint0,splineRight.localPoint1,splineRight.localTangetPos0,new Vector3(0,0,0),new Vector3(0,1,0));
					Vector3 rightTangent1Local = BezierSpline.GetThirdTrianglePoint(splineRight.localPoint0,splineRight.localPoint1,splineRight.localTangetPos1,new Vector3(0,0,0),new Vector3(0,1,0));
					
					Vector3 leftTangent0Local = BezierSpline.GetThirdTrianglePoint(splineLeft.localPoint0,splineLeft.localPoint1,splineLeft.localTangetPos0,new Vector3(0,0,0),new Vector3(0,1,0));
					Vector3 leftTangent1Local = BezierSpline.GetThirdTrianglePoint(splineLeft.localPoint0,splineLeft.localPoint1,splineLeft.localTangetPos1,new Vector3(0,0,0),new Vector3(0,1,0));
					
					Vector3 blendedTangent0Local = Vector3.Lerp(leftTangent0Local,rightTangent0Local,ratio.x);
					Vector3 blendedTangent1Local = Vector3.Lerp(leftTangent1Local,rightTangent1Local,ratio.x);
					
					Vector3 crossStart = splineBottom.GetPointLocal(ratio.x);
					Vector3 crossEnd = splineTop.GetPointLocal(ratio.x);
					
					Vector3 tangentStart = BezierSpline.GetThirdTrianglePoint(new Vector3(0,0,0),new Vector3(0,1,0),blendedTangent0Local,crossStart,crossEnd);
					Vector3 tangentEnd = BezierSpline.GetThirdTrianglePoint(new Vector3(0,0,0),new Vector3(0,1,0),blendedTangent1Local,crossStart,crossEnd);
					
					vertices[i] = BezierSpline.Interpolate(ratio.y,crossStart,tangentStart,tangentEnd,crossEnd);
					vertices[i].x -= anchor.x*lr.width;
					vertices[i].y -= anchor.y*lr.height;
				}
				
				Vector2[] uv;
				if (uvMapping == SpriteUVMapping.WorldSpaceProjected) {
					uv = new Vector2[vertices.Length];
					for (int i = 0; i < vertices.Length; i++) {
						Vector2 p = vertices[i];
						uv[i] = transform.TransformPoint(p);
						uv[i].x *= worldSpaceUVFactorX;
						uv[i].y *= worldSpaceUVFactorY;
						//uv[i].y *= aspect.y;
					}
				}
				else if (uvMapping == SpriteUVMapping.LocalSpaceProjected) {
					uv = new Vector2[vertices.Length];
					for (int i = 0; i < vertices.Length; i++) {
						Vector2 p = vertices[i];
						uv[i] = p;
						uv[i].x *= worldSpaceUVFactorX;
						uv[i].y *= worldSpaceUVFactorY;
						//uv[i].y *= aspect.y;
					}
				}
				else {
					uv = GetQuadPointsFromRectV2(quadUVRects[x,y]);
				}
				for (int i = 0; i < uv.Length; i++) {
					uv[i].x -= uvOffset.x*worldSpaceUVFactorX;
					uv[i].y -= uvOffset.y*worldSpaceUVFactorY;
				}
		        newMesh.vertices = vertices;
		        newMesh.uv = uv;
		        newMesh.triangles = triangles;
				
				meshes[x+xSegs*y] = newMesh;
			}
		}
		CombineInstance[] combine = new CombineInstance[meshes.Length];
		for (int i = 0; i < meshes.Length; i++){
			//Debug.Log(i+" "+meshes[i]);
			combine[i].mesh = meshes[i];
			//combine[i].transform = Matrix4x4.identity;
			combine[i].transform = transform.worldToLocalMatrix;
		}
		mesh.CombineMeshes(combine);
		
		for (int i = 0; i < meshes.Length; i++){
			DestroyImmediate(meshes[i]);
		}
		
		mesh.Optimize();
		mesh.RecalculateNormals();
		
		SetVertexColors(color);
		
		if (meshFilter.sharedMesh) {
			//DestroyImmediate(meshFilter.sharedMesh);
		}
		meshFilter.sharedMesh = mesh;
		
		return mesh;
	}
	
	public Mesh RebuildCollisionMesh() {
		if (collisionMesh) {
			DestroyImmediate(collisionMesh);
		}
		collisionMesh = new Mesh();
		
		int xSegs = actualWidthSegs;
		int ySegs = actualHeightSegs;
		xSegs = (int)((float)xSegs*collisionSegMultiplyer);
		ySegs = (int)((float)ySegs*collisionSegMultiplyer);
		xSegs = (int)Mathf.Max(xSegs,1);
		ySegs = (int)Mathf.Max(ySegs,1);
		
		Mesh topMesh = null;
		Mesh bottomMesh = null;
		Mesh leftMesh = null;
		Mesh rightMesh = null;
		
		int combineCounter = 0;
		if (useCollisionTop) combineCounter++;
		if (useCollisionBottom) combineCounter++;
		if (useCollisionLeft) combineCounter++;
		if (useCollisionRight) combineCounter++;
		CombineInstance[] combine = new CombineInstance[combineCounter];
		
		combineCounter = 0;
		if (useCollisionTop) {
			topMesh = CreateCollisionMeshStrip(splineTop,xSegs,false);
			combine[combineCounter].mesh = topMesh;
			//combine[combineCounter].transform = Matrix4x4.identity;
			combine[combineCounter].transform = transform.worldToLocalMatrix;
			combineCounter++;
		}
		if (useCollisionBottom) {
			bottomMesh = CreateCollisionMeshStrip(splineBottom,xSegs,true);
			combine[combineCounter].mesh = bottomMesh;
			//combine[combineCounter].transform = Matrix4x4.identity;
			combine[combineCounter].transform = transform.worldToLocalMatrix;
			combineCounter++;
		}
		if (useCollisionLeft) {
			leftMesh = CreateCollisionMeshStrip(splineLeft,ySegs,false);
			combine[combineCounter].mesh = leftMesh;
			//combine[combineCounter].transform = Matrix4x4.identity;
			combine[combineCounter].transform = transform.worldToLocalMatrix;
			combineCounter++;
		}
		if (useCollisionRight) {
			rightMesh = CreateCollisionMeshStrip(splineRight,ySegs,true);
			combine[combineCounter].mesh = rightMesh;
			//combine[combineCounter].transform = Matrix4x4.identity;
			combine[combineCounter].transform = transform.worldToLocalMatrix;
			combineCounter++;
		}
		
		collisionMesh.CombineMeshes(combine);
		
		
		if (topMesh) DestroyImmediate(topMesh);
		if (bottomMesh) DestroyImmediate(bottomMesh);
		if (leftMesh) DestroyImmediate(leftMesh);
		if (rightMesh) DestroyImmediate(rightMesh);
		
		MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
		if (!meshCollider && collider) {
			DestroyImmediate(collider);
		}
		if (!meshCollider) {
			meshCollider = gameObject.AddComponent<MeshCollider>();
			//DestroyImmediate(collider);
		}
		meshCollider.sharedMesh = null;
		if (collision == SpriteCollision.MeshConvex) {
			meshCollider.convex = true;
		}
		else {
			meshCollider.convex = false;
		}
		meshCollider.sharedMesh = collisionMesh;
		//meshFilter.sharedMesh = collisionMesh;
		
		return collisionMesh;
	}
	
	public Mesh CreateCollisionMeshStrip(BezierSpline spline,int segs,bool flip) {
		if (spline.linear) {
			segs = 1;
		}
		float anchorOffsetX = anchor.x*localRect.width;
		float anchorOffsetY = anchor.y*localRect.height;
		Mesh stripMesh = new Mesh();
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		for (int i = 0; i < segs+1; i++) {
			float t = (float)i/(float)segs;
			Vector3 v0 = spline.GetPointLocal(t);
			Vector3 v1 = v0;
			v0.x -= anchorOffsetX;
			v0.y -= anchorOffsetY;
			v1.x -= anchorOffsetX;
			v1.y -= anchorOffsetY;
			v0 -= new Vector3(0,0,collisionWidth*0.5f);
			v1 += new Vector3(0,0,collisionWidth*0.5f);
			vertices.Add(v0);
			vertices.Add(v1);
			if (i > 0) {
				if (flip) {
					triangles.Add(((i-0)*2)+0);
					triangles.Add(((i-1)*2)+1);
					triangles.Add(((i-1)*2)+0);
					
					triangles.Add(((i-1)*2)+1);
					triangles.Add(((i-0)*2)+0);
					triangles.Add(((i-0)*2)+1);
				}
				else {
					triangles.Add(((i-1)*2)+0);
					triangles.Add(((i-1)*2)+1);
					triangles.Add(((i-0)*2)+0);
					
					triangles.Add(((i-0)*2)+1);
					triangles.Add(((i-0)*2)+0);
					triangles.Add(((i-1)*2)+1);
				}
			}
		}
		stripMesh.vertices = vertices.ToArray();
		stripMesh.triangles = triangles.ToArray();
		return stripMesh;
	}
	
	public void RebuildBoxCollision() {
		BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
		if (!boxCollider && collider) {
			DestroyImmediate(collider);
		}
		if (!boxCollider) {
			boxCollider = gameObject.AddComponent<BoxCollider>();
		}
		Rect rect = localRect;
		Vector3 boxSize = new Vector3(0,0,collisionWidth);
		boxSize.x += rect.width;
		boxSize.y += rect.height;
		boxCollider.size = boxSize;
		Vector3 center = boxSize*0.5f;
		center.z = 0.0f;
		center.x -= anchor.x*localRect.width;
		center.y -= anchor.y*localRect.height;
		boxCollider.center = center;
	}
	
	public Rect[,] DivideRect(Rect rect, int xSlices, int ySlices) {
		//xSlices += 1;
		//ySlices += 1;
		Rect[,] outRects = new Rect[xSlices,ySlices];
		float newXSize = rect.width/xSlices;
		float newYSize = rect.height/ySlices;
		//float xIncrement = rect.width/(float)xSlices;
		//float yIncrement = rect.y/(float)ySlices;
		//Debug.Log(rect.y);
		for (int x = 0; x < xSlices; x++) {
			for (int y = 0; y < ySlices; y++) {
				outRects[x,y] = new Rect(newXSize*x,newYSize*y,newXSize,newYSize);
			}
		}
		return outRects;
	}
	
	public Vector3[] GetQuadPointsFromRect(Rect rect) {
        Vector3[] vertices = new Vector3[] {
            new Vector3(rect.x, rect.y, 0),
            new Vector3(rect.x, rect.y+rect.height, 0),
            new Vector3(rect.x+rect.width, rect.y+rect.height, 0),
            new Vector3(rect.x+rect.width, rect.y, 0)
        };
		return vertices;
	}
	public Vector2[] GetQuadPointsFromRectV2(Rect rect) {
        Vector2[] vertices = new Vector2[] {
            new Vector3(rect.x, rect.y),
            new Vector3(rect.x, rect.y+rect.height),
            new Vector3(rect.x+rect.width, rect.y+rect.height),
            new Vector3(rect.x+rect.width, rect.y)
        };
		return vertices;
	}
	
	public void OnDrawGizmosSelected() {
		Vector3[] points = GetQuadPointsFromRect(localRect);
		points[0] = transform.TransformPoint(points[0]);
		points[1] = transform.TransformPoint(points[1]);
		points[2] = transform.TransformPoint(points[2]);
		points[3] = transform.TransformPoint(points[3]);
		Gizmos.color = new Color(0.4f,0.4f,0.4f,0.25f);
		Gizmos.DrawLine(points[0],point0);
		Gizmos.DrawLine(points[1],point1);
		Gizmos.DrawLine(points[2],point2);
		Gizmos.DrawLine(points[3],point3);
		Gizmos.DrawLine(points[0],point0);
		Gizmos.color = new Color(0.4f,0.4f,0.4f,0.5f);
		Gizmos.DrawLine(points[0],points[1]);
		Gizmos.DrawLine(points[1],points[2]);
		Gizmos.DrawLine(points[2],points[3]);
		Gizmos.DrawLine(points[3],points[0]);
		
		//Vector3 rightTangent = 
	}
	
}
