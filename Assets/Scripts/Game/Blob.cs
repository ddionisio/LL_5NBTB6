using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using TMPro;

/// <summary>
/// Blob number
/// </summary>
public class Blob : MonoBehaviour, M8.IPoolSpawn, M8.IPoolDespawn {
    public const string parmData = "dat";
    public const string parmNumber = "number";
    public const string parmState = "state";
    public const string parmDivisor = "divisor";
    public const string parmSplitCount = "splitC";
    public const string parmLock = "lock";

    public enum State {
        None,
        Normal,
        Solved, //special material, no input, ready for 'correct' state
        Spawning, //animate and set to normal
        Despawning, //animate and release
        Error, //error highlight for a bit
        Correct //animate and release
    }

    [Header("Allocation")]
    public int capacity;

    [Header("Jelly")]
    public UnityJellySprite jellySprite;
    public float radius; //estimate radius

    [Header("Face Display")]
    public SpriteRenderer[] eyeSpriteRenders;
    public SpriteRenderer mouthSpriteRender;

    [Header("Face States")]
    public Sprite eyeSpriteNormal;
    public Sprite eyeSpriteClose;

    public float eyeBlinkOpenDelayMin = 0.5f;
    public float eyeBlinkOpenDelayMax = 4f;
    public float eyeBlinkCloseDelay = 0.3f;

    public Sprite mouthSpriteNormal;
    public Sprite mouthSpriteDragging;
    public Sprite mouthSpriteConnected;
    public Sprite mouthSpriteError;
    public Sprite mouthSpriteCorrect;

    [Header("Material States")]
    public Material normalMaterial;
	public Material solvedMaterial;
	public Material hoverDragMaterial;
    public Material errorMaterial;
    public Material correctMaterial;
	public Material lockMaterial;

	[Header("Error Settings")]
    public float errorDuration = 1f;

    [Header("Solved Settings")]
    public ParticleSystem solvedFX;

    [Header("Correct Settings")]
    public float correctStartDelay = 0.5f;

    [Header("Spawn Settings")]
    public M8.RangeFloat spawnCenterImpulse;
    public M8.RangeFloat spawnEdgeImpulse;
    public bool spawnIgnoreColorParam;
    public ParticleSystem spawnFX;

    [Header("Highlight Display")]
    public GameObject highlightGO; //active during enter and dragging
    public GameObject highlightLockGO; //active if isHighlightLock is true
    
    [Header("Split Display")]
	public GameObjectSetActiveDelay splitActive; //active if splittable
    public ParticleSystem splitFX;

	[Header("UI")]    
    public TMP_Text numericText;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public int takeSpawn = -1;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public int takeDespawn = -1;
    [M8.Animator.TakeSelector(animatorField = "animator")]
    public int takeCorrect = -1;
	[M8.Animator.TakeSelector(animatorField = "animator")]
	public int takeUnlock = -1;

	[Header("Sfx")]
    [M8.SoundPlaylist]
    public string soundSpawn;

    [Header("Signal Listens")]
    public M8.Signal signalListenDespawn; //use to animate and then despawn

    [Header("Signal Invokes")]
    public SignalBlob signalInvokeClick;
	public SignalBlob signalInvokeDragBegin;
    public SignalBlob signalInvokeDragEnd;
    public SignalBlob signalInvokeDespawn;

    public static bool dragDisabled { get; set; } //dangerous, make sure to properly reset it!

    public BlobData data { get; private set; }

    public Vector2 position {
        get {
            if(jellySprite && jellySprite.CentralPoint != null && jellySprite.CentralPoint.transform)
                return jellySprite.CentralPoint.transform.position;

            return transform.position;
		}
    }

    public int number {
        get { return mNumber; }
        set {
            if(mNumber != value) {
                mNumber = value;

				mCanSplit = divisor > 0 && data.CanSplit(mNumber, divisor);

				ApplyNumberDisplay();
            }
        }
    }

    public int divisor { get; private set; } //this is used for checking if this blob is divisible, 0 if not a dividend

    public int splitCount { get; private set; }

    public Vector2 dragPoint { get; private set; } //world
    public bool isDragging { get; private set; }
    public GameObject dragPointerGO { get; private set; } //current GameObject on pointer during drag
    public Blob dragPointerBlob { get; private set; } //blob that dragPointerGO/dragPointerJellySpriteRefPt is part of

	public M8.PoolDataController poolData {
        get {
            if(!mPoolDataCtrl)
                mPoolDataCtrl = GetComponent<M8.PoolDataController>();
            return mPoolDataCtrl;
        }
    }

    public bool inputLocked {
        get { return mInputLocked || mInputLockedInternal || mIsLocked; }
        set {
            if(mInputLocked != value) {
                mInputLocked = value;
                ApplyInputLocked();
            }
        }
    }

    public State state {
        get { return mState; }
        set {
            if(mState != value) {
                mState = value;
                ApplyState();
            }
        }
    }

    public bool isConnected {
        get { return mIsConnected; }
        set {
            if(mIsConnected != value) {
                mIsConnected = value;

                RefreshMouthSprite();

                //highlight
            }
        }
    }

    public Color color {
        get { return jellySprite ? jellySprite.m_Color : Color.clear; }
        set {
            if(jellySprite)
                jellySprite.SetColor(value);
        }
    }

    public float colorAlpha {
        get { return jellySprite ? jellySprite.m_Color.a : 0f; }
        set {
            if(jellySprite) {
                var clr = jellySprite.m_Color;
                clr.a = value;
                jellySprite.SetColor(clr);
            }
        }
    }

    public bool isHighlighted { get { return mIsHighlight; } }

    public bool highlightLock {
        get { return mIsHighlightLocked; }
        set {
            if(mIsHighlightLocked != value) {
                mIsHighlightLocked = value;

                if(highlightLockGO)
                    highlightLockGO.SetActive(mIsHighlightLocked);
            }
        }
    }

    public bool canSplit { get { return mCanSplit && !mIsLocked; } }

    public bool isLocked {
        get { return mIsLocked; }
        set {
            if(mIsLocked != value) {
                mIsLocked = value;

                if(mIsLocked) {
                    ApplyInputLocked();
                    ApplyJellySpriteMaterial(lockMaterial);
                }
                else {
                    switch(mState) {
                        case State.Normal:
                            ApplyJellySpriteMaterial(normalMaterial);
                            break;
                        case State.Correct:
                            ApplyJellySpriteMaterial(correctMaterial);
                            break;
                    }

                    if(takeUnlock != -1)
                        animator.Play(takeUnlock);

                    if(spawnFX) spawnFX.Play();
				}
			}
        }
    }

    private int mNumber;

    private M8.PoolDataController mPoolDataCtrl;

    private Camera mDragCamera;

    private Coroutine mRout;
    private Coroutine mEyeBlinkRout;

    private State mState = State.None;

    private RaycastHit2D[] mHitCache = new RaycastHit2D[16];

    private bool mInputLocked;
    private bool mInputLockedInternal;

    private bool mIsConnected;

    private bool mIsHighlight;
    private bool mIsHighlightLocked;

    private Sprite mLastSprite;
    private Color mLastColor;

    private State mSpawnToState;

    private bool mCanSplit;

    private bool mIsLocked;

    public void ReduceSplitCounter() {
        if(splitCount > 0)
            splitCount--;
    }

    /// <summary>
    /// Get an approximate edge towards given point, relies on reference points to provide edge.
    /// </summary>
    public bool GetEdge(Vector2 toPos, out Vector2 refPtPos, out int refPtIndex) {
        Vector2 sPos = jellySprite.CentralPoint.Body2D.position;
        Vector2 dpos = sPos - toPos;
        float dist = dpos.magnitude;

        if(dist <= 0f) {
            refPtPos = sPos;
            refPtIndex = 0;
            return false;
        }

        Vector2 dir = dpos / dist;

        var centralPointParent = jellySprite.CentralPoint.GameObject.transform.parent;

        var hitCount = Physics2D.RaycastNonAlloc(toPos, dir, mHitCache, dist, (1<<gameObject.layer));
        if(hitCount == 0) {
            refPtPos = sPos;
            refPtIndex = 0;
            return false;
        }

        //Collider2D edgeColl = null;
        Vector2 edgePt = sPos;
        int edgeInd = 0;

        for(int i = 0; i < hitCount; i++) {
            var hit = mHitCache[i];
            var coll = hit.collider;

            if(!coll)
                continue;

            //only consider hits from own reference pts.
            if(coll.transform.parent != centralPointParent)
                continue;

            edgePt = hit.point;
            edgeInd = coll.transform.GetSiblingIndex();
            break;
        }

        refPtPos = edgePt;
        refPtIndex = edgeInd;
        return true;
    }

    public void ApplyJellySpriteMaterial(Material mat) {
        if(mIsLocked)
            mat = lockMaterial;

        if(jellySprite.m_Material != mat) {
            jellySprite.m_Material = mat;
            jellySprite.ReInitMaterial();
        }
    }

    public OperatorType GetConnectOpType(Blob otherBlob) {
        if(isLocked)
            return OperatorType.None;

        if(otherBlob.isLocked)
            return OperatorType.None;

        if(!(data && otherBlob.data))
            return OperatorType.None;

        return data.GetConnectOpType(otherBlob.data);
    }

    public void AddForce(Vector2 force, ForceMode2D mode) {
        if(jellySprite.CentralPoint != null && jellySprite.CentralPoint.Body2D) {
            jellySprite.CentralPoint.Body2D.AddForce(force, mode);
		}
    }

    void OnApplicationFocus(bool isActive) {
        if(!isActive) {
            if(isDragging)
                DragInvalidate();
        }
    }

    /*void Awake() {
        //apply children to jelly attach
        Transform[] attaches = new Transform[transform.childCount];
        for(int i = 0; i < transform.childCount; i++)
            attaches[i] = transform.GetChild(i);

        if(jellySprite.m_AttachPoints == null)
            jellySprite.m_AttachPoints = attaches;
        else {
            int prevAttachPointLen = jellySprite.m_AttachPoints.Length;
            System.Array.Resize(ref jellySprite.m_AttachPoints, prevAttachPointLen + attaches.Length);
            System.Array.Copy(attaches, 0, jellySprite.m_AttachPoints, prevAttachPointLen, attaches.Length);
        }

        jellySprite.m_NumAttachPoints = jellySprite.m_AttachPoints.Length;
    }*/

    void M8.IPoolDespawn.OnDespawned() {        
        state = State.None;
                
        if(signalInvokeDespawn)
            signalInvokeDespawn.Invoke(this);
    }

    void M8.IPoolSpawn.OnSpawned(M8.GenericParams parms) {
        if(!jellySprite.isInit) {
            jellySprite.Init();

            mLastSprite = jellySprite.m_Sprite;
            mLastColor = jellySprite.m_Color;
        }

        data = null;
		mNumber = 0;
        mInputLocked = false;
        mInputLockedInternal = false;

        divisor = 0;
        splitCount = 0;

        Sprite spr = mLastSprite;
        Color clr = mLastColor;

        mSpawnToState = State.Normal;

        mIsLocked = false;

		Vector2 pos = Vector2.zero;
        float rot = 0f;

        if(parms != null) {
            if(parms.ContainsKey(JellySpriteSpawnController.parmPosition)) pos = parms.GetValue<Vector2>(JellySpriteSpawnController.parmPosition);
            if(parms.ContainsKey(JellySpriteSpawnController.parmRotation)) rot = parms.GetValue<float>(JellySpriteSpawnController.parmRotation);
            if(parms.ContainsKey(JellySpriteSpawnController.parmSprite)) spr = parms.GetValue<Sprite>(JellySpriteSpawnController.parmSprite);
            if(!spawnIgnoreColorParam && parms.ContainsKey(JellySpriteSpawnController.parmColor)) clr = parms.GetValue<Color>(JellySpriteSpawnController.parmColor);

            if(parms.ContainsKey(parmData))
                data = parms.GetValue<BlobData>(parmData);

            if(parms.ContainsKey(parmNumber))
                mNumber = parms.GetValue<int>(parmNumber);

            if(parms.ContainsKey(parmDivisor))
                divisor = parms.GetValue<int>(parmDivisor);

			if(parms.ContainsKey(parmSplitCount))
				splitCount = parms.GetValue<int>(parmSplitCount);

			if(parms.ContainsKey(parmState)) {
                var toState = parms.GetValue<State>(parmState);
                if(toState != State.None)
                    mSpawnToState = toState;
			}

            if(parms.ContainsKey(parmLock)) mIsLocked = parms.GetValue<bool>(parmLock);
		}

		mCanSplit = divisor > 0 && splitCount > 0 && data.CanSplit(mNumber, divisor);

        if(splitFX) {
            var dat = splitFX.main;
            dat.startColor = clr;
        }

		bool isInit = jellySprite.CentralPoint != null;
        if(isInit) {
            var mat = mIsLocked ? lockMaterial : normalMaterial;

            //need to reinitialize mesh/material?
            bool isMaterialChanged = jellySprite.m_Material != mat;
            bool isSpriteChanged = jellySprite.m_Sprite != spr;
            bool isColorChanged = jellySprite.m_Color != clr;

            jellySprite.m_Material = mat;
            jellySprite.m_Color = mLastColor = clr;

            if(isSpriteChanged)
                jellySprite.SetSprite(spr);
            else if(isMaterialChanged)
                jellySprite.ReInitMaterial();

			if(isColorChanged)
				jellySprite.RefreshColor();

			//reset and apply telemetry
			jellySprite.Reset(pos, new Vector3(0f, 0f, rot));
        }
        else {
            //directly apply telemetry
            var trans = jellySprite.transform;
            trans.position = pos;
            trans.eulerAngles = new Vector3(0f, 0f, rot);
        }

        //apply color to face
        for(int i = 0; i < eyeSpriteRenders.Length; i++) {
            if(eyeSpriteRenders[i])
                eyeSpriteRenders[i].color = clr;
        }

        if(mouthSpriteRender)
            mouthSpriteRender.color = clr;
        //

        ApplyNumberDisplay();

        state = State.Spawning;
    }

    /////////////////////////////////////////////////////////////
    // NOTE: Add a child gameObject with EventClick, EventListener, EventDragListener components and hook these up with their corresponding callbacks

    public void OnPointerClick() {
        if(inputLocked)
            return;

        signalInvokeClick?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if(inputLocked)
            return;

        mIsHighlight = true;

        if(state == State.Normal) {
            //highlight on
            if(hoverDragMaterial)
				ApplyJellySpriteMaterial(hoverDragMaterial);

            if(highlightGO) highlightGO.SetActive(!isDragging);
            if(splitActive) splitActive.isActive = !isDragging && mCanSplit;
		}
    }

    public void OnPointerExit(PointerEventData eventData) {
        if(inputLocked)
            return;

        mIsHighlight = false;

        //highlight off
        if(state == State.Normal) {
            if(!isDragging)
				ApplyJellySpriteMaterial(normalMaterial);

            if(highlightGO) highlightGO.SetActive(false);
            if(splitActive) splitActive.isActive = false;
		}
    }

    public void OnDragBegin(PointerEventData eventData) {
        if(dragDisabled)
            return;

        if(inputLocked)
            return;

        DragStart();

        DragUpdate(eventData);

        if(signalInvokeDragBegin)
            signalInvokeDragBegin.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData) {
        if(!isDragging)
            return;

        DragUpdate(eventData);
    }

    public void OnDragEnd(PointerEventData eventData) {
        if(!isDragging)
            return;

        isDragging = false;
        
        //signal
        if(signalInvokeDragEnd)
            signalInvokeDragEnd.Invoke(this);

        DragEnd();
    }

    IEnumerator DoSpawn() {
        if(!string.IsNullOrEmpty(soundSpawn))
            M8.SoundPlaylist.instance.Play(soundSpawn, false);

        if(takeSpawn != -1)
            yield return animator.PlayWait(takeSpawn);
        else
            yield return null;

		if(spawnFX) spawnFX.Play();

		mRout = null;

        state = mSpawnToState;

        //impulse center
        var centerDir = M8.MathUtil.RotateAngle(Vector2.up, Random.Range(0f, 360f));
        jellySprite.CentralPoint.Body2D.AddForce(centerDir * spawnCenterImpulse.random, ForceMode2D.Impulse);

        //impulse edges
        if(jellySprite.ReferencePoints.Count > 1) {
            var edgeDir = Vector2.right;
            var rotAmt = 360f / (jellySprite.ReferencePoints.Count - 1);
            for(int i = 1; i < jellySprite.ReferencePoints.Count; i++) {
                var refPt = jellySprite.ReferencePoints[i];
                refPt.Body2D.AddForce(edgeDir * spawnEdgeImpulse.random, ForceMode2D.Impulse);
                edgeDir = M8.MathUtil.RotateAngle(edgeDir, rotAmt);
            }
        }
    }

    IEnumerator DoDespawn() {
		ApplyJellySpriteMaterial(normalMaterial);

        if(takeDespawn != -1)
            yield return animator.PlayWait(takeDespawn);
        else
            yield return null;

        mRout = null;

        poolData.Release();
    }

    IEnumerator DoCorrect() {
        if(correctMaterial)
			ApplyJellySpriteMaterial(correctMaterial);

        if(correctStartDelay > 0f)
            yield return new WaitForSeconds(correctStartDelay);
        else
            yield return null;

		ApplyJellySpriteMaterial(normalMaterial);

        //something fancy
        if(takeCorrect != -1)
            yield return animator.PlayWait(takeCorrect);

        mRout = null;

        poolData.Release();
    }

    IEnumerator DoError() {
        //error highlight
        if(errorMaterial)
            ApplyJellySpriteMaterial(errorMaterial);

        yield return new WaitForSeconds(errorDuration);

        ApplyJellySpriteMaterial(normalMaterial);

        mRout = null;

        state = State.Normal;
    }

    IEnumerator DoEyeBlinking() {
        var blinkCloseWait = new WaitForSeconds(eyeBlinkCloseDelay);

        while(true) {
            SetEyesSprite(eyeSpriteNormal);

            yield return new WaitForSeconds(Random.Range(eyeBlinkOpenDelayMin, eyeBlinkOpenDelayMax));

            SetEyesSprite(eyeSpriteClose);

            yield return blinkCloseWait;
        }
    }
        
    private void RefreshMouthSprite() {
        if(!mouthSpriteRender)
            return;

        Sprite spr;
                
        switch(mState) {
			case State.Solved:
			case State.Correct:            
                spr = mouthSpriteCorrect;
                break;

            case State.Error:
                spr = mouthSpriteError;
                break;

            default:
                if(isDragging)
                    spr = mouthSpriteDragging;
                else if(isConnected)
                    spr = mouthSpriteConnected;
                else
                    spr = mouthSpriteNormal;
                break;
        }

        mouthSpriteRender.sprite = spr;
    }

    private void SetEyeBlinking(bool active) {
        if(active) {
            if(mEyeBlinkRout == null)
                mEyeBlinkRout = StartCoroutine(DoEyeBlinking());
        }
        else {
            if(mEyeBlinkRout != null) {
                StopCoroutine(mEyeBlinkRout);
                mEyeBlinkRout = null;

                SetEyesSprite(eyeSpriteNormal);
            }
        }
    }

    private void SetEyesSprite(Sprite spr) {
        for(int i = 0; i < eyeSpriteRenders.Length; i++) {
            if(eyeSpriteRenders[i])
                eyeSpriteRenders[i].sprite = spr;
        }
    }

    private Vector2 GetWorldPoint(Vector2 screenPos) {
        if(!mDragCamera)
            mDragCamera = Camera.main;

        if(mDragCamera)
            return mDragCamera.ScreenToWorldPoint(screenPos);

        return Vector2.zero;
    }

    private void DragStart() {
        isDragging = true;

        //display stuff, sound, etc.
        RefreshMouthSprite();

        if(hoverDragMaterial)
            ApplyJellySpriteMaterial(hoverDragMaterial);

		if(highlightGO) highlightGO.SetActive(false);
		if(splitActive) splitActive.isActive = false;
	}

    private void DragUpdate(PointerEventData eventData) {
        var prevDragPointerGO = dragPointerGO;
        dragPointerGO = eventData.pointerCurrentRaycast.gameObject;

        if(dragPointerGO) {
            //update ref.
            if(dragPointerGO != prevDragPointerGO) {
                dragPointerBlob = dragPointerGO.GetComponentInParent<Blob>();
			}

            dragPoint = eventData.pointerCurrentRaycast.worldPosition;
        }
        else {
            dragPointerBlob = null;

			//grab point from main camera
			dragPoint = GetWorldPoint(eventData.position);
        }
    }

    private void DragInvalidate() {
        if(isDragging) {
            //signal with no dragPointer
            dragPointerGO = null;
			dragPointerBlob = null;

			if(signalInvokeDragEnd)
                signalInvokeDragEnd.Invoke(this);
        }

        DragEnd();
    }

    private void DragEnd() {
        isDragging = false;
        dragPointerGO = null;
		dragPointerBlob = null;

		//hide display, etc.
		RefreshMouthSprite();

        if(state == State.Normal)
            ApplyJellySpriteMaterial(normalMaterial);
    }

    private void ClearRout() {
        if(mRout != null) {
            StopCoroutine(mRout);
            mRout = null;
        }
    }
        
    private void ApplyNumberDisplay() {
        if(numericText)
            numericText.text = mNumber.ToString();
    }

    private void ApplyInputLocked() {
        if(inputLocked) {
            //clear pointer highlight
            //update highlight
            if(highlightGO) highlightGO.SetActive(false);
            mIsHighlight = false;

            if(isDragging)
                DragInvalidate();
        }
    }
        
    private void ApplyState() {
        ClearRout();

        bool isDragInvalid = false;
                
        switch(mState) {
            case State.Normal:
                ApplyJellySpriteMaterial(normalMaterial);
                SetEyeBlinking(true);
                break;

            case State.Solved:
                ApplyJellySpriteMaterial(solvedMaterial);
                SetEyeBlinking(true);

				mInputLockedInternal = true;
				ApplyInputLocked();
				break;

			case State.Spawning:
                SetEyeBlinking(false);
                //animate, and then set state to normal
                mRout = StartCoroutine(DoSpawn());
                break;

            case State.Despawning:
                SetEyeBlinking(false);
                mInputLockedInternal = true;
                ApplyInputLocked();

                //animate and then release
                mRout = StartCoroutine(DoDespawn());
                break;

            case State.Correct:
                SetEyeBlinking(false);
                mInputLockedInternal = true;
                ApplyInputLocked();

                mRout = StartCoroutine(DoCorrect());
                break;

            case State.Error:
                SetEyeBlinking(false);
                mRout = StartCoroutine(DoError());
                break;

            default:
                SetEyeBlinking(false);
                isDragInvalid = true;
                break;
        }

        if(solvedFX) {
			var fxMain = solvedFX.main;

			if(mState == State.Solved) {                
                fxMain.loop = true;
                solvedFX.Play();
            }
            else
                fxMain.loop = false;
        }

        if(splitFX) {
            var fxMain = splitFX.main;

            if(mState == State.Normal && mCanSplit) {
                fxMain.loop = true;
                splitFX.Play();
            }
            else
				fxMain.loop = false;
		}

		if(highlightGO) highlightGO.SetActive(false);
        mIsHighlight = false;

        if(highlightLockGO) highlightLockGO.SetActive(false);
        mIsHighlightLocked = false;

        if(splitActive) splitActive.ForceSetActive(false);

		if(isDragInvalid)
            DragInvalidate();

        mIsConnected = false;

        RefreshMouthSprite();
    }

    void OnDrawGizmos() {
        if(radius > 0f) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
