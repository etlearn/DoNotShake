using UnityEngine;
using System.Collections;

public class Actor:MonoBehaviour {
	private Animation _anim;
	public Animation anim {
		get {
			if (!_anim) {
				_anim = gameObject.GetComponentInChildren<Animation>();
			}
			return _anim;
		}
	}
	private Game _game;
	public Game game {
		get {
			if (!_game) {
				_game = (Game)FindObjectOfType(typeof(Game));
			}
			return _game;
		}
	}
	private Exploder _exploder;
	public Exploder exploder {
		get {
			if (!_exploder) {
				_exploder = gameObject.GetComponent<Exploder>();
			}
			return _exploder;
		}
	}
	
	private Character _character;
	public Character character {
		get {
			if (!_character) {
				_character = gameObject.GetComponent<Character>();
			}
			return _character;
		}
	}
	
	public void PlayAnimation(string animName) {
		PlayAnimation(animName,1.0f);
	}
	public void PlayAnimation(string animName, float speed) {
		PlayAnimation(animName,speed,0.0f);
	}
	public void PlayAnimation(string animName, float speed,float startTime) {
		PlayAnimation(animName,speed,startTime,WrapMode.Loop);
	}
	public void PlayAnimation(string animName, float speed, float startTime, WrapMode wrapMode) {
		if (!anim) return;
		AnimationClip clip = anim.GetClip(animName);
		if (anim.clip != clip || !anim.isPlaying) {
			anim.wrapMode = wrapMode;
			anim.clip = clip;
			anim.CrossFade(anim.clip.name, 0.5f);
			anim[animName].time = startTime;
			anim[animName].speed = speed;
		}
	}
}
