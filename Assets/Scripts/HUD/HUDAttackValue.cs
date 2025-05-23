using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class HUDAttackValue : MonoBehaviour {
    [Header("Config")]
    public float changeDelay = 0.5f;

    [Header("Display")]
    public TMP_Text text;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public int takeUpdate = -1;

    [Header("Signal Listen")]
    public M8.Signal signalListenNewRound;
    public M8.Signal signalListenUpdate;

    public int attackValue {
        get { return mAttackValue; }
        set {
            var val = value < 0 ? 1 : value;

            if(attackValue != val) {
                mAttackValue = val;
                mIsUpdate = true;

                if(takeUpdate != -1)
                    animator.Play(takeUpdate);
            }
        }
    }

    private int mAttackValue;
    private int mCurAttackValue;
	private float mCurAttackValueF;
    private float mCurAttackValueVel;

    private bool mIsUpdate;

	void OnDestroy() {
		if(signalListenNewRound) signalListenNewRound.callback -= OnRefreshAttackValue;
		if(signalListenUpdate) signalListenUpdate.callback -= OnRefreshAttackValue;
	}

	void OnEnable() {
		mCurAttackValueF = mCurAttackValue = mAttackValue;
        mCurAttackValueVel = 0f;
        mIsUpdate = false;

        RefreshValueDisplay();
	}

	void Awake() {
        mAttackValue = 0;

        if(signalListenNewRound) signalListenNewRound.callback += OnRefreshAttackValue;
        if(signalListenUpdate) signalListenUpdate.callback += OnRefreshAttackValue;
	}

	void Update() {
        if(mIsUpdate) {
            var lastCurAttackValue = mCurAttackValue;

			mCurAttackValue = Mathf.RoundToInt(mCurAttackValueF);
            if(mCurAttackValue != mAttackValue) {
                mCurAttackValueF = Mathf.SmoothDamp(mCurAttackValueF, mAttackValue, ref mCurAttackValueVel, changeDelay);
            }
            else {
                mCurAttackValueF = mCurAttackValue;
                mCurAttackValueVel = 0f;
                mIsUpdate = false;
            }

            if(mCurAttackValue != lastCurAttackValue)
                RefreshValueDisplay();
		}
    }

    void OnRefreshAttackValue() {
        attackValue = PlayController.instance.attackValue;
    }

    private void RefreshValueDisplay() {
        if(text)
			text.text = mCurAttackValue.ToString();
	}
}
