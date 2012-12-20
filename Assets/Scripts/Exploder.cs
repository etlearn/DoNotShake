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
			if (detachChildren) {
				transform.DetachChildren();
			}
			Destroy(gameObject);
		}
		return ex;
	}
}
