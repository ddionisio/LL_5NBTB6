using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "blobData", menuName = "Game/Blob Data")]
public class BlobData : ScriptableObject {
	public enum Type {
		Dividend,
		Divisor,
		Quotient
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

    public Type type { get { return _type; } }

    public BlobData splitBlobData { get { return _splitBlobData; } }

    public BlobData mergeBlobData { get { return _mergeBlobData; } }

	public string templateName { get { return _template ? _template.name : ""; } }

    public float spawnPointCheckRadius { get { return _template ? _template.radius : 0f; } }

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
}
