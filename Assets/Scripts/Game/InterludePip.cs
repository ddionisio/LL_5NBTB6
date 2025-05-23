using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterludePip : MonoBehaviour {
	public enum Mode {
		Empty,
		EmptySelect,
		Fill
	}

	[SerializeField]
	Mode _mode;

	[Header("Animation")]
	[SerializeField]
	M8.Animator.Animate _animator;
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[SerializeField]	
	int _takeEmpty = -1;
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[SerializeField]
	int _takeEmptySelect = -1;
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[SerializeField]
	int _takeFill = -1;
	[M8.Animator.TakeSelector(animatorField = "_animator")]
	[SerializeField]
	int _takeFilled = -1;

	public Mode mode {
		get { return _mode; }
		set {
			if(_mode != value) {
				_mode = value;
				ApplyMode();
			}
		}
	}

	public bool isBusy { get { return mRout != null; } }

	private Coroutine mRout;

	public void InitMode(Mode mode) {
		StopRout();

		_mode = mode;

		switch(_mode) {
			case Mode.Empty:
				if(_takeEmpty != -1)
					_animator.Play(_takeEmpty);
				break;

			case Mode.EmptySelect:
				if(_takeEmptySelect != -1)
					_animator.Play(_takeEmptySelect);
				break;

			case Mode.Fill:
				if(_takeFilled != -1)
					_animator.Play(_takeFilled);
				break;
		}
	}

	void OnEnable() {
		InitMode(_mode);
	}

	void OnDisable() {
		StopRout();
	}

	IEnumerator DoFill() {
		if(_takeFill != -1)
			yield return _animator.PlayWait(_takeFill);

		if(_takeFilled != -1)
			_animator.Play(_takeFilled);

		mRout = null;
	}

	private void ApplyMode() {
		StopRout();

		switch(_mode) {
			case Mode.Empty:
				if(_takeEmpty != -1)
					_animator.Play(_takeEmpty);
				break;

			case Mode.EmptySelect:
				if(_takeEmptySelect != -1)
					_animator.Play(_takeEmptySelect);
				break;

			case Mode.Fill:
				mRout = StartCoroutine(DoFill());
				break;
		}
	}

	private void StopRout() {
		if(mRout != null) {
			StopCoroutine(mRout);
			mRout = null;
		}
	}
}
