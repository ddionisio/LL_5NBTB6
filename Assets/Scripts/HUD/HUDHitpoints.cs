using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDHitpoints : MonoBehaviour {
	[Header("Config")]
	public M8.RangeFloat fillValueRange = new M8.RangeFloat(-1f, 0f);
	public float fillDelay = 0.5f;
	[Range(0f, 1f)]
	public float warningScale = 0.25f;

	[Header("Display")]
	public GameObject warningActiveGO;
	public GameObject deadActiveGO;
	public GameObject aliveActiveGO;
	public M8.RendererMaterialSetPropertyBlockProxy fillProxy;

	[Header("Animation")]
	public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeHurt = -1;

	[Header("Signal Listen")]
	public M8.Signal signalListenNewRound;
	public M8.Signal signalListenAttackFinish;

	private int mCurHP;

	private float mFillValue;
	private float mFillValueCur;
	private float mFillValueVel;
	private bool mIsUpdate;

	void OnDestroy() {
		if(signalListenNewRound) signalListenNewRound.callback -= OnRefreshHP;
		if(signalListenAttackFinish) signalListenAttackFinish.callback -= OnRefreshHP;
	}

	void OnEnable() {
		mFillValueCur = mFillValue;
		mFillValueVel = 0f;
		mIsUpdate = false;

		RefreshFill();
	}

	void Awake() {
		mCurHP = 0;
		mFillValue = fillValueRange.min;

		if(aliveActiveGO) aliveActiveGO.SetActive(true);
		if(deadActiveGO) deadActiveGO.SetActive(false);

		if(signalListenNewRound) signalListenNewRound.callback += OnRefreshHP;
		if(signalListenAttackFinish) signalListenAttackFinish.callback += OnRefreshHP;
	}

	void Update() {
		if(mIsUpdate) {
			if(M8.MathUtil.Approx(mFillValueCur, mFillValue, 0.001f)) {
				mFillValueCur = mFillValue;
				mFillValueVel = 0f;
				mIsUpdate = false;
			}
			else {
				mFillValueCur = Mathf.SmoothDamp(mFillValueCur, mFillValue, ref mFillValueVel, fillDelay);
			}

			RefreshFill();
		}
	}

	void OnRefreshHP() {
		var playCtrl = PlayController.instance;

		var updateHP = playCtrl.hitpoints;
		if(mCurHP != updateHP) {
			var delta = updateHP - mCurHP;

			mCurHP = updateHP;

			var t = playCtrl.hitpointsScale;

			mFillValue = fillValueRange.Lerp(t);

			mIsUpdate = true;

			//reduced?
			if(delta < 0) {
				if(takeHurt != -1)
					animator.Play(takeHurt);
			}

			//dead?
			var isAlive = mCurHP > 0;

			if(aliveActiveGO) aliveActiveGO.SetActive(isAlive);
			if(deadActiveGO) deadActiveGO.SetActive(!isAlive);
		}
	}

	private void RefreshFill() {
		if(fillProxy)
			fillProxy.SetFloat(mFillValueCur);

		var fillT = fillValueRange.GetT(mFillValue);

		var isAlive = mCurHP > 0;

		if(warningActiveGO)
			warningActiveGO.SetActive(isAlive && fillT <= warningScale);
	}
}
