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

    [System.Serializable]
    public struct SpawnInfo {
        public BlobData data;
        public int digitLimit; //set to 0 for no limit
    }

    [Header("Data")]
    [SerializeField]
    Type _type;
    [SerializeField]
	SplitMode _splitMode;
    [SerializeField]
    int _splitCount;
    [SerializeField]
    SpawnInfo[] _splitInfos; //ensure order is least to most
	[SerializeField]
	SpawnInfo[] _reduceInfos; //used for partial quotient split, ensure order is least to most
	[SerializeField]
    SpawnInfo[] _mergeInfos; //ensure order is least to most

	[Header("Pool Info")]
    [SerializeField]
	Blob _template;

    [Header("Display")]
	[SerializeField]
	M8.ColorPalette _palette;

	public Type type { get { return _type; } }

    public SplitMode splitMode { get { return _splitMode; } }

    public int splitCount { get { return _splitCount; } }

	public string templateName { get { return _template ? _template.name : ""; } }

    public Blob template { get { return _template; } }

    public float spawnPointCheckRadius { get { return _template ? _template.radius : 0f; } }

    public Color color { get { return _palette.GetColor(Random.Range(0, _palette.count)); } }

    public static BlobData GetMergeData(Blob blobLeft, Blob blobRight, int val) {
        if(blobLeft && blobLeft.data._mergeInfos != null && blobLeft.data._mergeInfos.Length > 0)
            return blobLeft.data.GetMergeData(val);

        if(blobRight && blobRight.data._mergeInfos != null && blobRight.data._mergeInfos.Length > 0)
            return blobRight.data.GetMergeData(val);

        if(blobLeft)
            return blobLeft.data;

        if(blobRight)
            return blobRight.data;

        return null;
	}

    public BlobData GetSplitData(int val) {
		return GetData(_splitInfos, val);
    }

    public BlobData GetReduceData(int val) {
        //if no reduce info, just use split info
		return _reduceInfos != null && _reduceInfos.Length > 0 ? GetData(_reduceInfos, val) : GetData(_splitInfos, val);
	}

    public BlobData GetMergeData(int val) {
        return GetData(_mergeInfos, val);
	}

    private BlobData GetData(SpawnInfo[] infos, int val) {
		var digitCount = WholeNumber.DigitCount(val);

        if(infos != null) {
            for(int i = 0; i < infos.Length; i++) {
                var inf = infos[i];
                if(inf.digitLimit == 0 || digitCount <= inf.digitLimit)
                    return inf.data;
            }
        }

		return this;
	}

    public void InitPool(M8.PoolController pool) {
        var processed = new M8.CacheList<BlobData>(16);
        ProcessPool(pool, processed);
	}

    private void ProcessPool(M8.PoolController pool, M8.CacheList<BlobData> processed) {
        if(processed.Exists(this)) return;

		if(_template)
			pool.AddType(_template.gameObject, _template.capacity, _template.capacity);

        processed.Add(this);

        if(_splitInfos != null) {
            for(int i = 0; i < _splitInfos.Length; i++) {
                var inf = _splitInfos[i];
                if(inf.data)
                    inf.data.ProcessPool(pool, processed);
            }
        }

        if(_reduceInfos != null) {
            for(int i = 0; i < _reduceInfos.Length; i++) {
                var inf = _reduceInfos[i];
                if(inf.data)
                    inf.data.ProcessPool(pool, processed);
            }
        }

        if(_mergeInfos != null) {
            for(int i = 0; i < _mergeInfos.Length; i++) {
                var inf = _mergeInfos[i];
                if(inf.data)
                    inf.data.ProcessPool(pool, processed);
            }
        }
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

		/*switch(_splitMode) {
            case SplitMode.Tenths:
				if(dividend % divisor != 0)
					return false;

                //TODO: check permutations of digit swapping
                //TODO: check if the only non-zero number > 1
                var nonZeroCount = WholeNumber.NonZeroDigitCount(dividend);
                if(nonZeroCount > 1)
                    return true;
                else if(nonZeroCount == 1)
                    return dividend / divisor > 1;
                break;

            case SplitMode.PartialQuotient:
                if(dividend % divisor != 0)
                    return false;

                return dividend / divisor > 1;
        }

        return false;*/

        switch(_splitMode) {
            case SplitMode.Tenths:
            case SplitMode.PartialQuotient:
				if(dividend % divisor != 0)
					return false;

				return dividend / divisor > 1;
		}

		return false;
	}
}
