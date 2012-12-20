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
	
	public void PlayAnimation(string animName, float speed) {
		PlayAnimation(animName,speed,0.0f);
	}
	public void PlayAnimation(string animName, float speed, float startTime) {
		if (!anim) return;
		AnimationClip clip = anim.GetClip(animName);
		if (anim.clip != clip || !anim.isPlaying) {
			//anim.Blend(animName,1,0.5f);
			anim.wrapMode = WrapMode.Loop;
			anim.clip = clip;
			anim.Blend(anim.clip.name, 1.0f, 0.5f);
			anim[animName].time = startTime;
			anim[animName].speed = speed;
		}
	}
}
