using UnityEngine;
using System.Collections;

public class AIMovement:MonoBehaviour {
	public LayerMask awarenessMask = -1;
	private Character _character;
	public Character character {
		get {
			if (!_character) {
				_character = gameObject.GetComponent<Character>();
			}
			return _character;
		}
	}
	
	public virtual void UpdateMovement() {
		
	}
}
