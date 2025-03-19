using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "blobData", menuName = "Game/Blob Data")]
public class BlobData : ScriptableObject {
    [System.Serializable]
    public struct ConnectFilterData {
		public BlobData blobData;
		public OperatorType opType;        
    }

    [Header("Pool Info")]
    [SerializeField]
	Blob _template;
    [SerializeField]
    int _capacity;

    [Header("Connect Filters")]
    public ConnectFilterData[] connectFilters;

	public string templateName { get { return _template ? _template.name : ""; } }

    public float spawnPointCheckRadius { get { return _template ? _template.radius : 0f; } }

    public void InitPool(M8.PoolController pool) {
        if(_template)
            pool.AddType(_template.gameObject, _capacity, _capacity);
    }

    public OperatorType GetConnectOpType(BlobData otherBlobData) {
        for(int i = 0; i < connectFilters.Length; i++) {
            var dat = connectFilters[i];

            if(dat.blobData == otherBlobData)
                return dat.opType;
        }

        return OperatorType.None;
    }
}
