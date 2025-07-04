using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LoLExt {
	public class DialogPopupWidget : MonoBehaviour {
		[Header("Config")]
		[M8.Localize]
		public string stringRef;
		public bool openOnEnable = true;
		public bool closeOnEnd = true;
		public float closeOnEndDelay = 1f;
		public bool isRealtime;

		[Header("Display")]
		public GameObject displayGO;
		public GameObject textProcessActiveGO;
		public TMP_Text textLabel;
		public float textCharDelay = 0.04f;
				
		[Header("Animation")]
		public M8.Animator.Animate animator;
		[M8.Animator.TakeSelector]
		public int takeEnter = -1;
		[M8.Animator.TakeSelector]
		public int takeExit = -1;

		public bool isBusy { get { return mRout != null; } }

		public bool isOpen { get { return mIsOpen; } }

		private System.Text.StringBuilder mTextProcessSB = new System.Text.StringBuilder();
		private string mTextDialog;

		private Coroutine mRout;

		private bool mIsOpen;

		public void Open() {
			Open(stringRef);
		}

		public void Open(string strRef) {
			if(mRout != null)
				StopCoroutine(mRout);

			mRout = StartCoroutine(DoPlay(strRef));
		}

		public void Close() {
			if(mIsOpen) {
				if(mRout != null)
					StopCoroutine(mRout);

				mRout = StartCoroutine(DoClose());
			}
		}

		public void CloseImmediate() {
			if(displayGO) displayGO.SetActive(false);

			mIsOpen = false;

			if(mRout != null) {
				StopCoroutine(mRout);
				mRout = null;
			}
		}

		void OnEnable() {
			if(openOnEnable)
				Open();
		}

		void OnDisable() {
			CloseImmediate();
		}

		void Awake() {
			if(displayGO) displayGO.SetActive(false);
		}

		IEnumerator DoPlay(string strRef) {
			var lastOpen = mIsOpen;
			mIsOpen = true;

			textLabel.text = "";

			if(!lastOpen) {
				if(displayGO) displayGO.SetActive(true);

				if(textProcessActiveGO) textProcessActiveGO.SetActive(false);

				if(takeEnter != -1)
					yield return animator.PlayWait(takeEnter);
			}
			else {
				//in case we are in the process of opening and decided to change text
				if(takeEnter != -1) {
					while(animator.currentPlayingTakeIndex == takeEnter)
						yield return null;
				}
			}

			//start text thing
			if(LoLManager.isInstantiated && !string.IsNullOrEmpty(strRef))
				LoLManager.instance.SpeakText(strRef);

			mTextDialog = M8.Localize.Get(strRef);

			WaitForSeconds wait = null;
			WaitForSecondsRealtime waitRT = null;

			if(isRealtime)
				waitRT = new WaitForSecondsRealtime(textCharDelay);
			else
				wait = new WaitForSeconds(textCharDelay);

			if(textProcessActiveGO) textProcessActiveGO.SetActive(true);

			mTextProcessSB.Clear();

			int count = mTextDialog.Length;
			for(int i = 0; i < count; i++) {
				if(isRealtime)
					yield return waitRT;
				else
					yield return wait;

				if(mTextDialog[i] == '<') {
					int endInd = -1;
					bool foundEnd = false;
					for(int j = i + 1; j < mTextDialog.Length; j++) {
						if(mTextDialog[j] == '>') {
							endInd = j;
							if(foundEnd)
								break;
						}
						else if(mTextDialog[j] == '/')
							foundEnd = true;
					}

					if(endInd != -1 && foundEnd) {
						mTextProcessSB.Append(mTextDialog, i, (endInd - i) + 1);
						i = endInd;
					}
					else
						mTextProcessSB.Append(mTextDialog[i]);
				}
				else
					mTextProcessSB.Append(mTextDialog[i]);

				textLabel.text = mTextProcessSB.ToString();
			}

			if(textProcessActiveGO) textProcessActiveGO.SetActive(false);

			if(closeOnEnd) {
				if(closeOnEndDelay > 0f)
					yield return new WaitForSeconds(closeOnEndDelay);

				yield return DoClose();
			}

			mRout = null;
		}

		IEnumerator DoClose() {
			mIsOpen = false;

			if(textProcessActiveGO) textProcessActiveGO.SetActive(false);

			if(takeExit != -1)
				yield return animator.PlayWait(takeExit);

			displayGO.SetActive(false);

			mRout = null;
		}
	}
}