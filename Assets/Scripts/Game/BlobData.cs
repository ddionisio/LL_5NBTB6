using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "blobTemplateData", menuName = "Game/Blob Template Data")]
public class BlobData : ScriptableObject {
    [Header("Pool Info")]
    [SerializeField]
	Blob _template;
    [SerializeField]
    int _capacity;

    //TODO: connect filter

    public string templateName { get { return _template ? _template.name : ""; } }

    public float spawnPointCheckRadius { get { return _template ? _template.radius : 0f; } }

    public void InitPool(M8.PoolController pool) {
        if(_template)
            pool.AddType(_template.gameObject, _capacity, _capacity);
    }
}
