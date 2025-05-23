using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlobWidget : MonoBehaviour {
	public enum State {
		Normal,
		Error,
		Correct
	}

	[Header("Anchor")]
	[SerializeField]
	Transform _root;
	[SerializeField]
	RectTransform _anchor;
	[SerializeField]
	float _radius;

	[Header("Display")]
	[SerializeField]
	M8.UI.Graphics.ColorGroup _colorGroup;
	[SerializeField]
	Image _mouthImage;
	[SerializeField]
	Image[] _bodyImages;

	[Header("Materials")]
	[SerializeField]
	Material _matNormal;
	[SerializeField]
	Material _matError;
	[SerializeField]
	Material _matCorrect;

	[Header("Sprites")]
	[SerializeField]
	Sprite _spriteMouthNormal;
	[SerializeField]
	Sprite _spriteMouthError;
	[SerializeField]
	Sprite _spriteMouthCorrect;

	[Header("Animation")]
	[SerializeField]
	M8.Animator.Animate _animator;
	[SerializeField]
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[Tooltip("Set to loop")]
	int _takeNormal = -1;
	[SerializeField]
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[Tooltip("Set to play once, or not loop!")]
	int _takeError = -1;
	[SerializeField]
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[Tooltip("Set to play once, or not loop!")]
	int _takeCorrect = -1;
	[SerializeField]
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[Tooltip("Set to play once, or not loop!")]
	int _takePulse = -1;

	public bool active {
		get { return gameObject.activeSelf; }
		set { gameObject.SetActive(value); }
	}

	public Vector2 position {
		get { return transform.position; }
		set { transform.position = value; }
	}

	public float radius { get { return _radius; } }

	public Color color {
		get { return mColor; }
		set {
			if(mColor != value) {
				mColor = value;

				if(_colorGroup)
					_colorGroup.ApplyColor(new Color(mColor.r, mColor.g, mColor.b, mColor.a * mColorAlpha));
			}
		}
	}

	public float colorAlpha {
		get { return mColorAlpha; }
		set {
			if(mColorAlpha != value) {
				mColorAlpha = value;

				if(_colorGroup)
					_colorGroup.ApplyColor(new Color(mColor.r, mColor.g, mColor.b, mColor.a * mColorAlpha));
			}
		}
	}

	public Transform root { get { return _root ? _root : transform; } }

	public RectTransform anchor { get { return _anchor; } }

	public bool isBusy { get { return mRout != null; } }

	private Color mColor = Color.white;
	private float mColorAlpha = 1f;

	private Coroutine mRout;

	public static Vector2 GetMidpoint(BlobWidget left, BlobWidget right) {
		Vector2 leftPos = left._root.position;
		Vector2 rightPos = right._root.position;

		var dpos = rightPos - leftPos;
		var len = dpos.magnitude;

		if(len > 0) {
			var dir = dpos / len;

			var leftEdge = leftPos + dir * left._radius;
			var rightEdge = rightPos - dir * right._radius;

			return Vector2.Lerp(leftEdge, rightEdge, 0.5f);
		}
		else
			return leftPos;
	}

	public void SetToAnchor(RectTransform rTrans, bool applyStretch) {
		rTrans.SetParent(anchor);

		if(applyStretch) {			
			rTrans.anchorMin = Vector2.zero;
			rTrans.anchorMax = Vector2.one;
			rTrans.pivot = new Vector2(0.5f, 0.5f);
			rTrans.sizeDelta = Vector2.zero;
			rTrans.anchoredPosition = Vector2.zero;
		}
		else
			rTrans.localPosition = Vector3.zero;
	}

	public void Pulse() {
		if(mRout != null)
			StopCoroutine(mRout);

		ApplyState(State.Normal);

		if(_takePulse != -1)
			_animator.Play(_takePulse);
	}

	public void Correct() {
		if(mRout != null)
			StopCoroutine(mRout);

		mRout = StartCoroutine(DoStateToNormal(State.Correct));
	}

	public void Error() {
		if(mRout != null)
			StopCoroutine(mRout);

		mRout = StartCoroutine(DoStateToNormal(State.Error));
	}

	void OnEnable() {
		ApplyState(State.Normal);
	}

	private void OnDisable() {
		if(mRout != null) {
			StopCoroutine(mRout);
			mRout = null;
		}
	}

	private void ApplyState(State state) {
		Material mat = null;
		int takeInd = -1;
		Sprite mouthSpr = null;

		switch(state) {
			case State.Normal:
				mat = _matNormal;
				takeInd = _takeNormal;
				mouthSpr = _spriteMouthNormal;
				break;

			case State.Error:
				mat = _matError;
				takeInd = _takeError;
				mouthSpr = _spriteMouthError;
				break;

			case State.Correct:
				mat = _matCorrect;
				takeInd = _takeCorrect;
				mouthSpr = _spriteMouthCorrect;
				break;
		}

		if(_mouthImage && mouthSpr) {
			_mouthImage.sprite = mouthSpr;
		}

		if(_bodyImages != null && mat) {
			for(int i = 0; i < _bodyImages.Length; i++) {
				var img = _bodyImages[i];
				if(img)
					img.material = mat;
			}
		}

		if(takeInd != -1)
			_animator.Play(takeInd);
	}

	IEnumerator DoStateToNormal(State state) {
		ApplyState(state);

		if(_animator) {
			while(_animator.isPlaying)
				yield return null;
		}
		else
			yield return null;

		ApplyState(State.Normal);

		mRout = null;
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(position, _radius);
	}
}
