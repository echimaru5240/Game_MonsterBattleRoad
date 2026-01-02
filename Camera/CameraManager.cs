using UnityEngine;
using Unity.Cinemachine;
using DG.Tweening;

[System.Serializable]
public struct FollowShot
{
    public float yawDeg;        // 0=正面, +右, -左
    public float pitchDeg;      // +見下ろし, -見上げ
    public float distance;      // 距離
    public float lookAtHeight;  // 注視点の高さ（顔あたり）
    public float fov;
}

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Cameras (4)")]
    [SerializeField] private CinemachineCamera overviewCamera;
    [SerializeField] private CinemachineCamera orbitCamera;
    [SerializeField] private CinemachineCamera actionCamera;
    [SerializeField] private CinemachineCamera actionCameraCut;
    [SerializeField] private CinemachineCamera resultCamera;

    [Header("Brain Blend")]
    [SerializeField] private float easeBlendTime = 0.3f;

    [SerializeField] private CinemachineImpulseSource criticalImpulse;

    // Orbit
    [Header("Orbit")]
    [SerializeField] private Transform centerPos;
    [SerializeField] private float orbitSecondsPerRound = 10f;

    private CinemachineOrbitalFollow orbitOrbitalFollow;
    private Tween orbitTween;

    // Action rigs
    [Header("Action Rigs (auto created if null)")]
    [SerializeField] private Transform followRig;
    [SerializeField] private Transform lookAtRig;

    // ---- runtime mode ----
    private enum ActionMode
    {
        None,
        FollowAngles_OneTarget,     // 1体を角度＋距離で追尾（位置も追尾）
        FollowAngles_TwoTargets,    // 2体の中点を基準に角度＋距離で追尾（位置も追尾）
        FixedWorld_LookOnly_One,    // 定点（ワールド固定位置）で1体を見る（向きだけ）
        FixedWorld_LookOnly_Two     // 定点（ワールド固定位置）で中点を見る（向きだけ）
    }

    private ActionMode actionMode = ActionMode.None;

    // follow
    private Transform followTarget1; // attacker or single target
    private Transform followTarget2; // victim (optional)
    private FollowShot currentShot;

    // fixed
    private Vector3 fixedWorldPos;

    private CinemachineBrain brain;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;

        if (followRig == null)
        {
            followRig = new GameObject("ActionFollowRig").transform;
            DontDestroyOnLoad(followRig.gameObject);
        }
        if (lookAtRig == null)
        {
            lookAtRig = new GameObject("ActionLookAtRig").transform;
            DontDestroyOnLoad(lookAtRig.gameObject);
        }

        if (overviewCamera != null) CutToOverview(0f);
        else if (orbitCamera != null) StartOrbit(0f);
    }

    private void LateUpdate()
    {
        switch (actionMode)
        {
            case ActionMode.FollowAngles_OneTarget:
                // UpdateFollowAnglesOne();
                break;

            case ActionMode.FollowAngles_TwoTargets:
                UpdateFollowAnglesTwo();
                break;

            case ActionMode.FixedWorld_LookOnly_One:
                UpdateFixedLookOnlyOne();
                break;

            case ActionMode.FixedWorld_LookOnly_Two:
                UpdateFixedLookOnlyTwo();
                break;
        }
    }

    // ==============================
    // Public: Basic
    // ==============================
    public void CutToOverview(float duration = 0f)
    {
        StopAction();
        KillOrbitTween();
        SetActiveCam(overviewCamera, duration);
    }

    public void CutToResult(float duration = 0f)
    {
        StopAction();
        KillOrbitTween();
        SetActiveCam(resultCamera, duration);
    }

    // ==============================
    // Public: Orbit
    // ==============================
    public void StartOrbit(float duration = 0f)
    {
        StopAction();

        if (orbitCamera == null || centerPos == null) return;

        orbitOrbitalFollow = orbitCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitOrbitalFollow == null)
        {
            Debug.LogWarning("[CameraManager] CinemachineOrbitalFollow not found on orbitCamera.");
            return;
        }

        orbitCamera.Follow = centerPos;
        orbitCamera.LookAt = centerPos;

        KillOrbitTween();
        orbitOrbitalFollow.HorizontalAxis.Value = 0f;

        SetActiveCam(orbitCamera, duration);

        float sec = Mathf.Max(0.1f, orbitSecondsPerRound);
        orbitTween = DOTween.To(
                () => orbitOrbitalFollow.HorizontalAxis.Value,
                x => orbitOrbitalFollow.HorizontalAxis.Value = x,
                360f,
                sec
            )
            .SetEase(Ease.Linear)
            .SetRelative(true)
            .SetLoops(-1, LoopType.Restart);
    }

    public void StopOrbit() => KillOrbitTween();

    // ==============================
    // Public: Action 1体（角度＋距離で追尾）
    // ==============================
    public void CutAction_Follow(Transform target, Vector3 offset, float duration = 0f, float fov = 45f )
    {
        if (actionCameraCut == null || target == null) return;
        KillOrbitTween();

        var followOffset = actionCameraCut.GetComponent<CinemachineFollow>();
        if (followOffset == null) return;
        followOffset.FollowOffset = offset;
        actionMode = ActionMode.FollowAngles_OneTarget;

        actionCameraCut.Follow = target;
        actionCameraCut.LookAt = target;
        actionCameraCut.Lens.FieldOfView = fov;

        SetActiveCam(actionCameraCut, duration);
    }

    // ==============================
    // Public: Action 2体（中点を見る／中点基準で追尾）
    // ==============================
    public void CutAction_FollowByAnglesMidpoint(Transform attacker, Transform victim, FollowShot shot, float duration = 0f)
    {
        if (actionCamera == null || attacker == null || victim == null) return;

        KillOrbitTween();

        followTarget1 = attacker;
        followTarget2 = victim;
        currentShot = shot;

        actionMode = ActionMode.FollowAngles_TwoTargets;

        actionCamera.Follow = followRig;
        actionCamera.LookAt = lookAtRig;
        actionCamera.Lens.FieldOfView = shot.fov > 0 ? shot.fov : 45f;

        SetActiveCam(actionCamera, duration);
    }

    // ==============================
    // Public: 定点（向きだけ変える）1体
    // ==============================
    public void CutAction_FixedWorldLookOnly(Vector3 worldPos, Transform lookAtTarget, float lookAtHeight = 1.2f, float fov = 45f, float duration = 0f)
    {
        var camera = (duration == 0f) ? actionCameraCut : actionCamera;

        if (camera == null || lookAtTarget == null) return;

        KillOrbitTween();

        fixedWorldPos = worldPos;
        followTarget1 = lookAtTarget;
        followTarget2 = null;

        currentShot = new FollowShot
        {
            lookAtHeight = lookAtHeight,
            fov = fov
        };

        actionMode = ActionMode.FixedWorld_LookOnly_One;

        // 位置固定：Followを切る
        camera.Follow = null;
        camera.LookAt = lookAtRig;
        camera.transform.position = worldPos;
        camera.Lens.FieldOfView = fov;

        SetActiveCam(camera, duration);
    }

    // ==============================
    // Public: 定点（向きだけ変える）2体中点
    // ==============================
    public void CutAction_FixedWorldLookOnlyMidpoint(Vector3 worldPos, Transform attacker, Transform victim, float lookAtHeight = 1.2f, float fov = 45f, float duration = 0f)
    {
        if (actionCamera == null || attacker == null || victim == null) return;

        KillOrbitTween();

        fixedWorldPos = worldPos;
        followTarget1 = attacker;
        followTarget2 = victim;

        currentShot = new FollowShot
        {
            lookAtHeight = lookAtHeight,
            fov = fov
        };

        actionMode = ActionMode.FixedWorld_LookOnly_Two;

        actionCamera.Follow = null;
        actionCamera.LookAt = lookAtRig;
        actionCamera.transform.position = worldPos;
        actionCamera.Lens.FieldOfView = fov;

        SetActiveCam(actionCamera, duration);
    }

    public void EnableFirstPerson(Transform fpvAnchor, Vector3 offset, float duration = 0f)
    {
        if (actionCamera == null || fpvAnchor == null) return;

        // ★ 1人称は「位置も回転もアンカーに一致」が基本
        actionCamera.Follow = fpvAnchor;
        actionCamera.LookAt = null; // LookAtは不要（自分の向きで見る）

        // Bodyが邪魔しないように：CinemachineFollowのOffsetは0推奨
        var follow = actionCamera.GetComponent<CinemachineFollow>();
        if (follow != null)
        {
            follow.FollowOffset = offset;
            // follow.PositionDamping = Vector3.zero;
            // follow.RotationDamping = 0f;
        }

        actionCamera.Lens.FieldOfView = 45f;
        // 初回配置（ブレンドでズレるの防止）
        actionCamera.transform.SetPositionAndRotation(fpvAnchor.position, fpvAnchor.rotation);

        // カメラ切り替え（あなたのCameraManagerの関数）
        CameraManager.Instance.SetActiveCam(actionCamera, duration);
    }

    public void StopAction()
    {
        actionMode = ActionMode.None;
        followTarget1 = null;
        followTarget2 = null;
    }


    public void ShakeCritical()
    {
        if (criticalImpulse == null) return;
        criticalImpulse.GenerateImpulse();
        Debug.Log("ShakeCritical");
    }

    // ==============================
    // Update: Follow 1体
    // ==============================
    private void UpdateFollowAnglesOne()
    {
        if (followTarget1 == null) return;

        // LookAt（顔あたり）
        lookAtRig.position = followTarget1.position + Vector3.up * currentShot.lookAtHeight;

        // カメラ位置：targetのYaw基準で yaw/pitch/distance
        followRig.position = CalcFollowPosByAngles(followTarget1.position, followTarget1.forward, currentShot);
    }

    // ==============================
    // Update: Follow 2体（中点）
    // ==============================
    private void UpdateFollowAnglesTwo()
    {
        if (followTarget1 == null || followTarget2 == null) return;

        Vector3 a = followTarget1.position;
        Vector3 b = followTarget2.position;
        Vector3 mid = (a + b) * 0.5f;

        // LookAt：中点＋高さ
        lookAtRig.position = mid + Vector3.up * currentShot.lookAtHeight;

        // 向き基準： attacker→victim の方向（これが一番破綻しない）
        Vector3 dir = (b - a);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = followTarget1.forward;
        dir.Normalize();

        // カメラ位置：中点を中心に yaw/pitch/distance で置く
        followRig.position = CalcFollowPosByAngles(mid, dir, currentShot);
    }

    // ==============================
    // Update: Fixed LookOnly 1体
    // ==============================
    private void UpdateFixedLookOnlyOne()
    {
        if (followTarget1 == null) return;

        // 位置固定（念のため毎フレ固定してもOK）
        actionCamera.transform.position = fixedWorldPos;

        // 注視点だけ更新
        lookAtRig.position = followTarget1.position + Vector3.up * currentShot.lookAtHeight;
    }

    // ==============================
    // Update: Fixed LookOnly 2体中点
    // ==============================
    private void UpdateFixedLookOnlyTwo()
    {
        if (followTarget1 == null || followTarget2 == null) return;

        actionCamera.transform.position = fixedWorldPos;

        Vector3 mid = (followTarget1.position + followTarget2.position) * 0.5f;
        lookAtRig.position = mid + Vector3.up * currentShot.lookAtHeight;
    }

    // ==============================
    // Math: 角度＋距離→ワールド座標
    // baseForwardはYaw基準（y=0のforward推奨）
    // ==============================
    private Vector3 CalcFollowPosByAngles(Vector3 basePos, Vector3 baseForward, FollowShot shot)
    {
        Debug.Log($"basePos: {basePos}, baseForward: {baseForward}");
        Vector3 forward = baseForward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // yaw
        float yawRad = shot.yawDeg * Mathf.Deg2Rad;
        Vector3 dirYaw = Mathf.Cos(yawRad) * forward + Mathf.Sin(yawRad) * right;

        // pitch（right軸回転）
        Quaternion pitchRot = Quaternion.AngleAxis(shot.pitchDeg, right);
        Vector3 dir = pitchRot * dirYaw;

        float dist = Mathf.Max(0.01f, shot.distance);
        Debug.Log($"dir: {dir.normalized}, dist: {dist}, basePos + dir.normalized * dist: {basePos + dir.normalized * dist}");
        return basePos + dir.normalized * dist;
    }

    // ==============================
    // Internal
    // ==============================
    private void SetActiveCam(CinemachineCamera cam, float duration = 0f)
    {
        if (cam == null) return;

        if (brain == null && Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        if (brain != null)
        {
            brain.DefaultBlend = (duration == 0f)
                ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f)
                : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, duration);
        }

        if (overviewCamera != null) overviewCamera.Priority = 0;
        if (orbitCamera != null) orbitCamera.Priority = 0;
        if (actionCamera != null) actionCamera.Priority = 0;
        if (actionCameraCut != null) actionCameraCut.Priority = 0;
        if (resultCamera != null) resultCamera.Priority = 0;

        cam.Priority = 20;
    }

    private void KillOrbitTween()
    {
        if (orbitTween != null && orbitTween.IsActive())
        {
            orbitTween.Kill();
            orbitTween = null;
        }
    }
}
