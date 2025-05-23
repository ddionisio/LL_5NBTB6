using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDErrorItem : MonoBehaviour {
	[Header("Config")]
	public int errorCountMin;

	[Header("Animator")]
	public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeEmpty = -1;
	[M8.Animator.TakeSelector]
	public int takeFill = -1;

	[Header("Signal Listen")]
	public M8.Signal signalListenNewRound;
	public M8.Signal signalListenFail;

	private bool mIsEmpty = false;

	void OnDestroy() {
		if(signalListenNewRound) signalListenNewRound.callback -= OnSignalNewRound;
		if(signalListenFail) signalListenFail.callback -= OnSignalFail;
	}

	void Awake() {
		if(signalListenNewRound) signalListenNewRound.callback += OnSignalNewRound;
		if(signalListenFail) signalListenFail.callback += OnSignalFail;
	}

	void OnSignalNewRound() {
		mIsEmpty = CheckEmpty();
		PlayFill();
	}

	void OnSignalFail() {
		var empty = CheckEmpty();
		if(mIsEmpty != empty) {
			mIsEmpty = empty;
			PlayFill();
		}
	}

	private bool CheckEmpty() {
		int failCount = PlayController.instance.failRoundCount;

		return failCount > errorCountMin;
	}

	private void PlayFill() {
		if(mIsEmpty) {
			if(takeEmpty != -1)
				animator.Play(takeEmpty);
		}
		else {
			if(takeFill != -1)
				animator.Play(takeFill);
		}
	}
}
