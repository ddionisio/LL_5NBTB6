using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobWidgetTemplateMatch : MonoBehaviour {
	[System.Serializable]
	public struct ItemData {
		public Blob blobTemplate; //pointer to blob prefab
		public BlobWidget blobWidget; //pointer to widget in the hierarchy

		public bool widgetActive {
			get { return blobWidget ? blobWidget.active : false; }
			set {
				if(blobWidget)
					blobWidget.active = value;
			}
		}
	}

	[SerializeField]
	public ItemData[] _items;

	public BlobWidget blobWidget { get; private set; }

	public void ApplyMatchingWidget(BlobData blobDat) {
		blobWidget = null;

		for(int i = 0; i < _items.Length; i++) {
			var itm = _items[i];
			if(itm.blobTemplate == blobDat.template) {
				blobWidget = itm.blobWidget;

				itm.widgetActive = true;
			}
			else
				itm.widgetActive = false;
		}
	}
}
