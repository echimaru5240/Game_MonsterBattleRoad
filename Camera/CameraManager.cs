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
    // ? 回転カメラ設定
    // ==============================
    [Header("Overview Rotation Settings")]
    public Vector3 battleCenter = Vector3.zero; // 固定中心
    public float rotationSpeed = 10f;  // 回転速度（度/秒）
    public float rotationRadius = 10f; // 戦場中心からの距離
    public float baseHeight = 5f;      // 基本高さ
    public float heightAmplitude = 0.5f; // 上下ゆれ幅
    public float zoomAmplitude = 1.0f;   // ズームの振れ幅
    public float zoomSpeed = 0.5f;       // ズーム変化速度
    private bool rotateOverview = false;
    private float angle = 0f;

    void Start()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
        SetCamera(overviewCamera);
    }

    void Update()
    {
        if (rotateOverview)
        {
            // 回転角度を更新（速度をゆるく揺らす）
            float speedMod = 1f + Mathf.Sin(Time.time * 0.3f) * 0.2f;
            angle += rotationSpeed * speedMod * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;

            // ゆるい上下揺れ
            float yOffset = Mathf.Sin(Time.time * 0.5f) * heightAmplitude;

            // ゆるいズーム変化
            float currentRadius = rotationRadius + Mathf.Sin(Time.time * zoomSpeed) * zoomAmplitude;

            // カメラ位置更新
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * currentRadius;
            Vector3 pos = battleCenter + offset + Vector3.up * (baseHeight + yOffset);

            overviewCamera.transform.position = pos;
            overviewCamera.transform.LookAt(battleCenter + Vector3.up * 1.5f);
        }
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

    public void PlayFrontShot(Transform attacker, bool isEnemy, float duration = 1.5f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToFront(attacker, isEnemy, duration));
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
    public void PlayHitReactionCamera(Transform target, float duration = 1f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToHit(target, duration));
    }

    private IEnumerator SwitchToFront(Transform target, bool isEnemy, float duration)
    {
        if (isEnemy) {
            SetupCameraFollow(actionCamera, target, new Vector3(0, 0, 0), 8f); // 近距離
            actionCamera.transform.rotation = Quaternion.Euler(30f, -30f, 0); // 正面から撮影
        }
        else {
            SetupCameraFollow(actionCamera, target, new Vector3(0, 0, 0), 8f); // 近距離
            actionCamera.transform.rotation = Quaternion.Euler(30f, 210f, 0); // 正面から撮影
        }
        SetCameraInstant(actionCamera);
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator SwitchToAction(Transform target, bool isEnemy, float duration)
    {
        StopOverviewRotation();

        SetupCameraFollow(actionCamera, target, new Vector3(0, 0, 0), 10f);
        SetCameraInstant(actionCamera);
        yield return new WaitForSeconds(duration);
        SetCamera(overviewCamera);
        StartOverviewRotation(); // 攻撃終了後、再び回転を再開
    }

    private IEnumerator SwitchToHit(Transform target, float duration)
    {
        SetupCameraFollow(actionCamera, target, new Vector3(0, 0, 0), 3.5f);
        SetCamera(actionCamera);
        yield return new WaitForSeconds(duration);
        SetCamera(overviewCamera);
    }

    /// <summary>
    /// CinemachineCameraに追従設定を適用
    /// </summary>
    private void SetupCameraFollow(CinemachineCamera cam, Transform target, Vector3 offset, float distance)
    {
        cam.Follow = target;
        cam.LookAt = target;

        // 現在のCinemachine v3では、FramingTransposerは“Body”に直接アクセス
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
        // 優先度を一瞬で切り替える
        overviewCamera.Priority = 0;
        actionCamera.Priority = 0;
        cam.Priority = 20;

        // CinemachineBrainのBlendを一時的に無効化
        if (brain != null)
        {
            var blend = new Unity.Cinemachine.CinemachineBlendDefinition(
                Unity.Cinemachine.CinemachineBlendDefinition.Styles.Cut, 0f);
            brain.DefaultBlend = blend;
        }

    }

}
