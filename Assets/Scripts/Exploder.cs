using UnityEngine;
using System.Collections;

public class Exploder:MonoBehaviour {
	
	public Explosion explosion;
	public bool destroyOnExplode = true;
	public bool detachChildren = false;
	
	[System.NonSerializedAttribute]
	private Actor _owner;
	
	private Actor owner {
		get {
			if (!_owner) {
				_owner = gameObject.GetComponent<Actor>();
			}
			return _owner;
		}
	}
	
	public Explosion Explode() {
		Explosion ex = null;
		if (explosion != null) {
			ex = (Explosion)Instantiate(explosion,transform.position,transform.rotation);
		}
		if (destroyOnExplode) {
			ExploderDetatcher[] detatchers = gameObject.GetComponentsInChildren<ExploderDetatcher>();
			for (int i = 0; i < detatchers.Length; i++) {
				if (detatchers[i].gameObject == gameObject) continue;
				detatchers[i].transform.parent = null;
				detatchers[i].gameObject.SendMessage("OnDetatched",SendMessageOptions.DontRequireReceiver);
			}
			if (detachChildren) {
				transform.DetachChildren();
			}
			Destroy(gameObject);
		}
		return ex;
	}
}
