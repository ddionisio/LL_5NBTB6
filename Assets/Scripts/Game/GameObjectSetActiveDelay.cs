using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectSetActiveDelay : MonoBehaviour {
	public GameObject targetGO;

	public float delay = 0.1f;

	public bool isActive {
		get { return mIsActive; }
		set {
			mIsActive = value;
			mLastTime = Time.time;
		}
	}

	private bool mIsActive;
	private float mLastTime;

	public void ForceSetActive(bool active) {
		mIsActive = active;
		mLastTime = Time.time;

		if(targetGO) targetGO.SetActive(active);
	}

	void OnEnable() {
		mIsActive = targetGO ? targetGO.activeSelf : false;
		mLastTime = Time.time;
	}

	void Update() {
		if(!targetGO) return;

		if(targetGO.activeSelf != mIsActive && Time.time - mLastTime >= delay)
			targetGO.SetActive(mIsActive);
	}
}
