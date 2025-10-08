using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// バトル中のカメラ制御（Cinemachine v3対応）
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("全体を俯瞰で映すカメラ")]
    public CinemachineCamera overviewCamera;

    [Tooltip("攻撃キャラを映すアクションカメラ")]
    public CinemachineCamera actionCamera;

    private CinemachineBrain brain;
    private Coroutine currentRoutine;

    // ==============================
    // ? 俯瞰カメラ設定（Inspectorで調整可能）
    // ==============================
    [Header("Overview Rotation Settings")]
    [Tooltip("戦闘の中心点")]
    public Vector3 battleCenter = Vector3.zero;

    [Tooltip("回転速度（度/秒）")]
    public float rotationSpeed = 10f;

    [Tooltip("回転半径（戦場中心からの距離）")]
    public float rotationRadius;
    public float bossRotationRadius;

    [Tooltip("カメラの高さ")]
    public float baseHeight = 5f;

    [Tooltip("ボスモンスターを撮るときのZ軸オフセット")]
    public float lookAtZOffsetBoss = -2f;

    [Tooltip("上下の揺れ幅")]
    public float heightAmplitude = 0.5f;

    [Tooltip("ズームの振れ幅")]
    public float zoomAmplitude = 1.0f;

    [Tooltip("ズーム変化速度")]
    public float zoomSpeed = 0.5f;

    [Tooltip("初期角度（度）")]
    public float initialAngle = 45f;

    [Tooltip("ボスを映すときに距離を何倍にするか")]
    public float bossDistanceMultiplier = 2.0f;

    private bool rotateOverview = false;
    private float angle;
    private bool isBossMode = false;


    void Start()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogWarning("CinemachineBrainがMainCameraに見つかりません。");
            return;
        }

        angle = initialAngle; // ← Inspectorで指定した初期角度からスタート
        if (isBossMode) {
            rotationRadius = bossRotationRadius;
        }
        SetCameraInstant(overviewCamera);
        Debug.Log("[CameraManager] カメラ初期化完了");
    }

    void Update()
    {
        if (rotateOverview)
        {
            // 回転角度を更新（緩やかなゆらぎ）
            float speedMod = 1f + Mathf.Sin(Time.time * 0.3f) * 0.2f;
            angle += rotationSpeed * speedMod * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;

            // 上下ゆれ
            float yOffset = Mathf.Sin(Time.time * 0.5f) * heightAmplitude;

            // ズーム変化
            float currentRadius = rotationRadius + Mathf.Sin(Time.time * zoomSpeed) * zoomAmplitude;

            // カメラ位置
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * currentRadius;
            Vector3 pos = battleCenter + offset + Vector3.up * (baseHeight + yOffset);

            overviewCamera.transform.position = pos;
            overviewCamera.transform.LookAt(battleCenter + Vector3.up * 1.5f);
        }
    }

    public void SetBossMode(bool isBoss)
    {
        isBossMode = isBoss;
        Debug.Log($"[CameraManager] ボスモード: {isBoss}");
    }

    // ==============================
    // 回転制御
    // ==============================
    public void StartOverviewRotation()
    {
        rotateOverview = true;
    }

    public void StopOverviewRotation()
    {
        rotateOverview = false;
    }

    public void PlayFrontShot(Transform attacker, bool isEnemy, float duration = 1.5f, bool isBoss = false)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToFront(attacker, isEnemy, duration, isBoss));
    }

    /// <summary>
    /// 攻撃時のカメラ演出
    /// </summary>
    public void PlayAttackCamera(Transform attacker, bool isEnemy, float duration = 0f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToAction(attacker, isEnemy, duration));
    }

    /// <summary>
    /// 被弾時のカメラ寄り演出
    /// </summary>
    public void PlayHitReactionCamera(Transform target, bool isEnemy, float duration = 1f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToHit(target, isEnemy, duration));
    }

    // ==============================
    // 内部演出コルーチン
    // ==============================
    private IEnumerator SwitchToFront(Transform target, bool isEnemy, float duration, bool isBoss)
    {
        float distance = 8f;

        if (isEnemy)
        {
            if (isBossMode) {
                distance = 10f * bossDistanceMultiplier;
            }
            SetupCameraFollow(actionCamera, target, isEnemy, Vector3.zero, distance);
            actionCamera.transform.rotation = Quaternion.Euler(30f, -20f, 0);
        }
        else
        {
            SetupCameraFollow(actionCamera, target, isEnemy, Vector3.zero, distance);
            actionCamera.transform.rotation = Quaternion.Euler(30f, 200f, 0);
        }

        SetCameraInstant(actionCamera);
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator SwitchToAction(Transform target, bool isEnemy, float duration)
    {
        float distance = 10f;

        StopOverviewRotation();
        if (isEnemy && isBossMode) {
            distance *= bossDistanceMultiplier;
        }
        SetupCameraFollow(actionCamera, target, isEnemy, Vector3.zero, distance);
        SetCameraInstant(actionCamera);
        yield return new WaitForSeconds(duration);
        SetCamera(overviewCamera);
        StartOverviewRotation();
    }

    private IEnumerator SwitchToHit(Transform target, bool isEnemy, float duration)
    {
        SetupCameraFollow(actionCamera, target, isEnemy, Vector3.zero, 3.5f);
        SetCamera(actionCamera);
        yield return new WaitForSeconds(duration);
        SetCamera(overviewCamera);
    }

    // ==============================
    // Cinemachine設定
    // ==============================
    private void SetupCameraFollow(CinemachineCamera cam, Transform target, bool isEnemy,  Vector3 offset, float distance)
    {
        cam.Follow = target;
        cam.LookAt = target;

        if (isBossMode && isEnemy) {
            // 現在のカメラ位置を取得
            Vector3 camPos = cam.transform.position;

            // targetのforward方向に、Zオフセットを加算（位置のみ移動）
            camPos += target.forward * lookAtZOffsetBoss;
            camPos += target.right * -5;

            // カメラの位置を更新（向きは変更しない）
            cam.transform.position = camPos;
        }

        if (cam.TryGetComponent(out CinemachinePositionComposer composer))
        {
            composer.TargetOffset = offset;
            composer.CameraDistance = distance;
        }
        else if (cam.TryGetComponent(out CinemachineThirdPersonFollow thirdPersonFollow))
        {
            thirdPersonFollow.CameraDistance = distance;
        }
    }

    /// <summary>
    /// 優先度でカメラを切り替える
    /// </summary>
    private void SetCamera(CinemachineCamera cam)
    {
        overviewCamera.Priority = 0;
        actionCamera.Priority = 0;
        cam.Priority = 10;
    }

    public void SetCameraInstant(CinemachineCamera cam)
    {
        overviewCamera.Priority = 0;
        actionCamera.Priority = 0;
        cam.Priority = 20;

        if (brain != null)
        {
            var blend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.Cut, 0f);
            brain.DefaultBlend = blend;
        }
    }
}
