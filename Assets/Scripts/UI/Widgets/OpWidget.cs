using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class OpWidget : MonoBehaviour {
    [SerializeField]
    TMP_Text _opLabel;
    [SerializeField]
    OperatorType _initialOperator;

    public OperatorType operatorType {
        get { return mOp; }
        set {
            if(mOp != value) {
                mOp = value;
                RefreshDisplay();
            }
        }
    }

    public RectTransform rectTransform {
        get {
            if(!mRectTrans)
                mRectTrans = GetComponent<RectTransform>();
            return mRectTrans;
        }
    }

    public Vector2 position {
        get { return rectTransform.position; }
        set { rectTransform.position = value; }
    }

    private OperatorType mOp;
    private RectTransform mRectTrans;

    void OnEnable() {
		RefreshDisplay();
	}

    void Awake() {
        mOp = _initialOperator;        
    }

    private void RefreshDisplay() {
        if(_opLabel) {
            if(mOp == OperatorType.None) {
                _opLabel.gameObject.SetActive(false);
            }
            else {
				_opLabel.gameObject.SetActive(true);
				_opLabel.text = Operation.GetOperatorTypeString(mOp);
            }
        }
    }
}
