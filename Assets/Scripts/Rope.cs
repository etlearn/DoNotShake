using UnityEngine;
using System.Collections;

public class Rope : MonoBehaviour {
	public Transform target;
	
	private LineRenderer _lineRenderer;
	public LineRenderer lineRenderer {
		get {
			if (!_lineRenderer) {
				_lineRenderer = gameObject.GetComponent<LineRenderer>();
			}
			return _lineRenderer;
		}
	}
	
	void Update() {
		lineRenderer.useWorldSpace = true;
		lineRenderer.SetPosition(0, transform.position);
		if (target == null) {
			lineRenderer.SetPosition(1, transform.position);
		}
		else {
			lineRenderer.SetPosition(1, target.transform.position);
		}
	}
}
