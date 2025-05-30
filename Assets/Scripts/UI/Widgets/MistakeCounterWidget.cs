using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MistakeCounterWidget : MonoBehaviour {
	public const string userDataKeyWarning = "healthWarning";

	[Header("Fill Info")]
	[SerializeField]
	Slider _fillBar;
	[SerializeField]
	DG.Tweening.Ease _fillChangeEase = DG.Tweening.Ease.OutSine;
	[SerializeField]
	float _fillChangeDelay = 0.5f;
	[SerializeField]
	int _fillDangerMinCount = 1;
	[SerializeField]
	GameObject _fillDangerGO;

	[Header("Display Warning")]
	public GameObject warningGO;
	[M8.Localize]
	public string warningTextRef;
	public float warningDelay = 5f;

	[Header("Animation")]
	[SerializeField]
	M8.Animator.Animate _animator;
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[SerializeField]
	int _takeHurt = -1;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxHurt;

	public bool isBusy { get { return mRout != null; } }

	private DG.Tweening.EaseFunction mFillChangeEaseFunc;

	private Coroutine mRout;

	public void Init(int curMistakeCount, int maxMistakeCount) {
		var mistakeFillCount = maxMistakeCount - curMistakeCount;

		var fillVal = ((float)mistakeFillCount) / maxMistakeCount;

		//setup initial display
		if(_fillBar)
			_fillBar.normalizedValue = fillVal;

		if(_fillDangerGO)
			_fillDangerGO.SetActive(mistakeFillCount <= _fillDangerMinCount);

		if(mFillChangeEaseFunc == null)
			mFillChangeEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(_fillChangeEase);
	}

	public void UpdateMistakeCount(int curMistakeCount, int maxMistakeCount) {
		if(mRout != null)
			StopCoroutine(mRout);

		mRout = StartCoroutine(DoUpdate(curMistakeCount, maxMistakeCount));
	}

	void OnDisable() {
		if(mRout != null) {
			StopCoroutine(mRout);
			mRout = null;
		}
	}

	void OnEnable() {
		if(warningGO) warningGO.SetActive(false);
	}

	IEnumerator DoUpdate(int curMistakeCount, int maxMistakeCount) {
		var mistakeFillCount = maxMistakeCount - curMistakeCount;

		if(_fillBar) {
			var curFillVal = _fillBar.normalizedValue;
			var newFillVal = ((float)mistakeFillCount) / maxMistakeCount;

			if(newFillVal < curFillVal) {
				if(!string.IsNullOrEmpty(sfxHurt))
					M8.SoundPlaylist.instance.Play(sfxHurt, false);

				if(_takeHurt != -1)
					_animator.Play(_takeHurt);
			}

			var curTime = 0f;
			while(curTime < _fillChangeDelay) {
				yield return null;

				curTime += Time.deltaTime;

				var t = mFillChangeEaseFunc(curTime, _fillChangeDelay, 0f, 0f);

				_fillBar.normalizedValue = Mathf.Lerp(curFillVal, newFillVal, t);
			}
		}

		if(_fillDangerGO)
			_fillDangerGO.SetActive(mistakeFillCount <= _fillDangerMinCount);

		var isWarningDone = LoLExt.LoLManager.instance.userData.GetInt(userDataKeyWarning, 0) > 0;
		if(!isWarningDone) {
			LoLExt.LoLManager.instance.userData.SetInt(userDataKeyWarning, 1);
			StartCoroutine(DoWarning());
		}

		mRout = null;
	}

	IEnumerator DoWarning() {
		if(warningGO) warningGO.SetActive(true);

		if(!string.IsNullOrEmpty(warningTextRef))
			LoLExt.LoLManager.instance.SpeakText(warningTextRef);

		yield return new WaitForSeconds(warningDelay);

		if(warningGO) warningGO.SetActive(false);
	}
}
