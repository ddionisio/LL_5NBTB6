using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackEntity : MonoBehaviour {
	[Header("Config")]
	[SerializeField]
	float _moveDelay = 0.67f;
	[SerializeField]
	M8.RangeFloat _moveHeightOfs;
	[SerializeField]
	DG.Tweening.Ease _moveEase;

	[Header("Animation")]
	[SerializeField]
	M8.Animator.Animate _animator;
	[SerializeField]
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	int _takePlay = -1;

	public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

	public bool isBusy {
		get {
			if(_animator && _animator.isPlaying)
				return true;

			return mIsMoving;
		}
	}

	private bool mIsMoving;
	private float mCurMoveTime;

	private Vector2 mFrom;
	private Vector2 mMid;
	private Vector2 mTo;

	private DG.Tweening.EaseFunction mMoveEase;

	private ParticleSystem mFXPlayOnFinish;

	public void Move(Vector2 from, Vector2 to, ParticleSystem fxPlayOnFinish) {
		if(_takePlay != -1)
			_animator.Play(_takePlay);

		mFrom = from;
		mTo = to;

		if(Random.Range(0, 2) == 1) { //up?
			var yMax = Mathf.Max(mFrom.y, mTo.y) + _moveHeightOfs.random;

			mMid = new Vector2(Mathf.Lerp(mFrom.x, mTo.x, 0.5f), yMax);
		}
		else {
			var yMin = Mathf.Min(mFrom.y, mTo.y) - _moveHeightOfs.random;

			mMid = new Vector2(Mathf.Lerp(mFrom.x, mTo.x, 0.5f), yMin);
		}

		transform.position = mFrom;

		mFXPlayOnFinish = fxPlayOnFinish;

		mCurMoveTime = 0f;

		mIsMoving = true;
	}

	void Awake() {
		mMoveEase = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(_moveEase);
	}

	void Update() {
		if(mIsMoving) {
			mCurMoveTime += Time.deltaTime;

			var t = mMoveEase(mCurMoveTime, _moveDelay, 0f, 0f);

			transform.position = M8.MathUtil.Bezier(mFrom, mMid, mTo, t);

			if(t >= 1f) {
				if(mFXPlayOnFinish) {
					mFXPlayOnFinish.Play();
					mFXPlayOnFinish = null;
				}

				mIsMoving = false;
			}
		}
	}
}
