using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "blobData", menuName = "Game/Blob Data")]
public class BlobData : ScriptableObject {
	public enum Type {
		Dividend,
		Divisor,
		Quotient,
	}

    public enum SplitMode {
        None,
        Tenths,
        PartialQuotient
    }

    [Header("Data")]
    [SerializeField]
    Type _type;
    [SerializeField]
	SplitMode _splitMode;
	[SerializeField]
    BlobData _splitBlobData;
	[SerializeField]
	BlobData _mergeBlobData;

	[Header("Pool Info")]
    [SerializeField]
	Blob _template;
    [SerializeField]
    int _capacity;

    [Header("Display")]
	[SerializeField]
	M8.ColorPalette _palette;

	public Type type { get { return _type; } }

    public SplitMode splitMode { get { return _splitMode; } }

    public BlobData splitBlobData { get { return _splitBlobData; } }

    public BlobData mergeBlobData { get { return _mergeBlobData; } }

	public string templateName { get { return _template ? _template.name : ""; } }

    public float spawnPointCheckRadius { get { return _template ? _template.radius : 0f; } }

    public Color color { get { return _palette.GetColor(Random.Range(0, _palette.count)); } }

    /// <summary>
    /// Return merge data from left or right, if merge data is not available, use left's or right's data
    /// </summary>
    public static BlobData GetMergeData(BlobData left, BlobData right) {
        if(left && left.mergeBlobData)
            return left.mergeBlobData;
        else if(right && right.mergeBlobData)
            return right.mergeBlobData;

        return left ? left : right;
	}

    public void InitPool(M8.PoolController pool) {
        if(_template)
            pool.AddType(_template.gameObject, _capacity, _capacity);

        if(_splitBlobData)
			_splitBlobData.InitPool(pool);

        if(_mergeBlobData)
            _mergeBlobData.InitPool(pool);
	}

    public OperatorType GetConnectOpType(BlobData otherBlobData) {
        switch(_type) {
            case Type.Dividend:
                if(otherBlobData._type == Type.Divisor)
                    return OperatorType.Divide;
                break;

            case Type.Divisor:
                if(otherBlobData.type == Type.Dividend)
                    return OperatorType.Divide;
                break;

            case Type.Quotient:
                if(otherBlobData.type == Type.Quotient)
                    return OperatorType.Add;
                break;
        }

        return OperatorType.None;
    }

    public bool CanSplit(int dividend, int divisor) {
        if(divisor == 0)
            return false;

        switch(_splitMode) {
            case SplitMode.Tenths:
				if(dividend % divisor != 0)
					return false; 

                //TODO: check permutations of digit swapping

                return WholeNumber.NonZeroDigitCount(dividend) > 1;

            case SplitMode.PartialQuotient:
                if(dividend % divisor != 0)
                    return false;

                return dividend / divisor > 1;
        }

        return false;
    }
}
