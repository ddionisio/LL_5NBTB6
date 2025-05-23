using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LoLExt {
	public class DragToGuide : MonoBehaviour {
		public GameObject rootGO;
		public Transform cursor;

		[Header("Animation")]
		public SpriteRenderer cursorRenderer;
		public SpriteRenderer lineRenderer; //ensure anchor is bottom, and mode is set to sliced or tiled
		public Sprite cursorIdleSprite;
		public Sprite cursorPressSprite;
		public float cursorFadeDelay = 0.3f;
		public float cursorIdleDelay = 0.3f;
		public float cursorMoveDelay = 0.5f;

		//[0, 1]
		public float dragPosition {
			get { return mDragPosition; }
			set {
				mDragPosition = value;

				if(!Application.isPlaying)
					return;

				cursor.position = Vector2.Lerp(mDragStart, mDragEnd, mDragPosition);
			}
		}

		public bool isActive { get { return rootGO ? rootGO.activeSelf : false; } }

		public Vector2 dragStart { get { return mDragStart; } }
		public Vector2 dragEnd { get { return mDragEnd; } }

		private Vector2 mDragStart;
		private Vector2 mDragEnd;

		private float mDragPosition;
		private bool mIsPaused;

		public void UpdatePositions(Vector2 start, Vector2 end) {
			mDragStart = start;
			mDragEnd = end;

			mDragStart = start;
			mDragEnd = end;

			var dpos = mDragEnd - mDragStart;
			var dist = dpos.magnitude;

			//set line position, rotation, and size
			var lineTrans = lineRenderer.transform;

			lineTrans.position = mDragStart;

			if(dist > 0f)
				lineTrans.up = dpos / dist;

			var lineSize = lineRenderer.size;

			lineSize.y = dist;

			lineRenderer.size = lineSize;
			//

			cursor.position = Vector2.Lerp(mDragStart, mDragEnd, mDragPosition);
		}

		public void Show(bool pause, Vector2 start, Vector2 end) {
			StopAllCoroutines();

			SetPause(pause);

			mDragPosition = 0f;

			UpdatePositions(start, end);

			if(rootGO) rootGO.SetActive(true);

			StartCoroutine(DoCursorMove());
		}

		public void Follow(Transform from, Transform to) {
			if(!(from && to)) return;

			StopAllCoroutines();

			SetPause(false);

			mDragPosition = 0f;

			UpdatePositions(from.position, to.position);

			if(rootGO) rootGO.SetActive(true);

			StartCoroutine(DoCursorMove());
			StartCoroutine(DoFollow(from, to));
		}

		public void Hide() {
			SetPause(false);

			if(rootGO) rootGO.SetActive(false);

			StopAllCoroutines();
		}

		void OnDisable() {
			Hide();
		}

		void Awake() {
			if(rootGO) rootGO.SetActive(false);
		}

		IEnumerator DoFollow(Transform from, Transform to) {
			while(from && to) {
				yield return null;

				UpdatePositions(from.position, to.position);
			}

			Hide();
		}

		IEnumerator DoCursorMove() {
			var moveEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(DG.Tweening.Ease.InOutSine);

			while(true) {
				dragPosition = 0f;
				cursorRenderer.sprite = cursorIdleSprite;

				float curTime;
				float lastTime = Time.realtimeSinceStartup;

				//fade in            
				do {
					curTime = Time.realtimeSinceStartup - lastTime;
					float t = moveEaseFunc(curTime, cursorFadeDelay, 0f, 0f);

					var clr = cursorRenderer.color;
					clr.a = t;

					cursorRenderer.color = clr;

					yield return null;
				} while(curTime < cursorFadeDelay);
				//

				yield return new WaitForSecondsRealtime(cursorIdleDelay);

				cursorRenderer.sprite = cursorPressSprite;

				yield return new WaitForSecondsRealtime(cursorIdleDelay);

				//move
				lastTime = Time.realtimeSinceStartup;
				do {
					curTime = Time.realtimeSinceStartup - lastTime;
					float t = moveEaseFunc(curTime, cursorMoveDelay, 0f, 0f);

					dragPosition = t;

					yield return null;
				} while(curTime < cursorMoveDelay);
				//

				yield return new WaitForSecondsRealtime(cursorIdleDelay);

				cursorRenderer.sprite = cursorIdleSprite;

				yield return new WaitForSecondsRealtime(cursorIdleDelay);

				//fade out
				lastTime = Time.realtimeSinceStartup;
				do {
					curTime = Time.realtimeSinceStartup - lastTime;
					float t = moveEaseFunc(curTime, cursorFadeDelay, 0f, 0f);

					var clr = cursorRenderer.color;
					clr.a = 1.0f - t;

					cursorRenderer.color = clr;

					yield return null;
				} while(curTime < cursorFadeDelay);
				//

				yield return new WaitForSecondsRealtime(cursorIdleDelay);
			}
		}

		private void SetPause(bool pause) {
			if(mIsPaused != pause) {
				mIsPaused = pause;
				if(M8.SceneManager.isInstantiated) {
					if(mIsPaused)
						M8.SceneManager.instance.Pause();
					else
						M8.SceneManager.instance.Resume();
				}
			}
		}
	}
}