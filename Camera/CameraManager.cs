using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// バトル中のカメラ制御（Cinemachine v3対応）
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Cameras")]
    [Tooltip("全体を俯瞰で映すカメラ")]
    public CinemachineCamera overviewCamera;

    [Tooltip("全体を回転して映すカメラ")]
    public CinemachineCamera orbitCamera;
    private CinemachineOrbitalFollow orbitalFollow;

    [Tooltip("攻撃キャラを映すアクションカメラ")]
    public CinemachineCamera playerActionCamera;
    public CinemachineCamera playerActionCamera_1;
    public CinemachineCamera enemyActionCamera;
    public CinemachineCamera enemyActionCamera_1;


    [Tooltip("リザルト画面を映すカメラ")]
    public CinemachineCamera resultCamera;

    private CinemachineBrain brain;

    // ==============================
    // ? 俯瞰カメラ設定
    // ==============================
    [Header("Overview Rotation Settings")]
    [Tooltip("戦闘の中心点")]
    public Vector3 battleCenter = Vector3.zero;

    [Tooltip("回転速度（度/秒）")]
    public float rotationSpeed = 10f;

    [Tooltip("回転間隔（周/秒）")]
    public float rotationInterval = 10f;

    [Tooltip("回転半径（戦場中心からの距離）")]
    public float rotationRadius = 10f;

    [Tooltip("カメラの高さ")]
    public float baseHeight = 5f;

    [Tooltip("上下の揺れ幅")]
    public float heightAmplitude = 0.5f;

    [Tooltip("ズームの振れ幅")]
    public float zoomAmplitude = 1.0f;

    [Tooltip("ズーム変化速度")]
    public float zoomSpeed = 0.5f;

    [Tooltip("初期角度（度）")]
    public float initialAngle = 45f;

    private bool rotateOverview = false;
    private float angle;

    // ==============================
    // 初期化
    // ==============================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // シーンをまたいでも保持

        brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogWarning("CinemachineBrainがMainCameraに見つかりません。");
            return;
        }

        angle = initialAngle;
        // SetCameraInstant(overviewCamera);
        SetCameraInstant(orbitCamera);
        Debug.Log("[CameraManager] カメラ初期化完了");
    }

    // ==============================
    // 更新処理（俯瞰カメラ回転）
    // ==============================
    private void Update()
    {
        if (!rotateOverview) return;

        float speedMod = 1f + Mathf.Sin(Time.time * 0.3f) * 0.2f;
        angle += rotationSpeed * speedMod * Time.deltaTime;
        float rad = angle * Mathf.Deg2Rad;

        float yOffset = Mathf.Sin(Time.time * 0.5f) * heightAmplitude;
        float currentRadius = rotationRadius + Mathf.Sin(Time.time * zoomSpeed) * zoomAmplitude;

        Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * currentRadius;
        Vector3 pos = battleCenter + offset + Vector3.up * (baseHeight + yOffset);

        overviewCamera.transform.position = pos;
        overviewCamera.transform.LookAt(battleCenter + Vector3.up * 1.5f);
    }

    // ==============================
    // 公開制御関数
    // ==============================
    public void StartOverviewRotation() => rotateOverview = true;
    public void StopOverviewRotation() => rotateOverview = false;

    public void SwitchToFrontCamera(Transform target, bool isPlayer)
    {
        float distance = 10f;

        // if (isPlayer)
        // {
        //     SetupCameraFollow(actionCamera, target, isPlayer, Vector3.zero, distance);
        //     actionCamera.transform.rotation = Quaternion.Euler(30f, 200f, 0);
        // }
        // else
        // {
        //     SetupCameraFollow(actionCamera, target, isPlayer, Vector3.zero, distance);
        //     actionCamera.transform.rotation = Quaternion.Euler(30f, -20f, 0);
        // }

        // SetCameraInstant(actionCamera);
    }

    // ==============================
    // リザルトカメラ演出
    // ==============================
    public void SwitchToResultCamera()
    {
        StopOverviewRotation();

        // カメラ位置・向き設定（後方からやや上）
        // actionCamera.transform.position = new Vector3(0f, 5f, -20f);
        // actionCamera.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

        // カメラを即時切り替え
        SetCameraInstant(resultCamera);
    }

    // ==============================
    // 天体カメラ演出
    // ==============================
    public void SwitchToOrbitCamera()
    {
        orbitalFollow = orbitCamera.GetComponent<CinemachineOrbitalFollow>();
        SetCameraInstant(orbitCamera);

        // 0→360度を5秒でループ再生
        DOTween.To(() => orbitalFollow.HorizontalAxis.Value, x =>
        {
            orbitalFollow.HorizontalAxis.Value = x;
        }, 360f, rotationInterval)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart); // 無限ループ
    }


    public void SwitchToActionCamera(Transform newTarget, bool isPlayer)
    {
        CinemachineCamera actionCamera = isPlayer ? playerActionCamera : enemyActionCamera;
        actionCamera.Target.TrackingTarget = newTarget;
        actionCamera.Target.LookAtTarget = newTarget;
        SetCameraInstant(actionCamera);
    }


    public void SwitchToActionCamera1(Transform newTarget, bool isPlayer)
    {
        CinemachineCamera actionCamera = isPlayer ? playerActionCamera_1 : enemyActionCamera_1;
        actionCamera.Target.TrackingTarget = newTarget;
        actionCamera.Target.LookAtTarget = newTarget;
        SetCameraInstant(actionCamera);
    }

    /// <summary>
    /// 攻撃開始時などにアクションカメラへ即切り替え
    /// </summary>
    // public void SwitchToActionCamera2(Transform target, bool isPlayer)
    // {
    //     if (target == null || actionCamera == null) return;

    //     StopOverviewRotation();

    //     float distance = 15f;
    //     // SetupCameraFollow(actionCamera, target, isEnemy, Vector3.zero, distance);
    //     SetupCameraLookOnly(actionCamera, target);
    //     SetCameraInstant(actionCamera);

    // }

    /// <summary>
    /// 攻撃開始時などにアクションカメラへ即切り替え
    /// ターゲットを向き続けながら、Z座標のみ移動する
    /// </summary>
    // public void SwitchToActionCamera(Transform target, bool isPlayer, Transform attacker)
    // {
    //     if (target == null || actionCamera == null || attacker == null) return;


    //     actionCamera.Target.TrackingTarget = attacker;
    //     actionCamera.Target.LookAtTarget = attacker;
    //     // StopOverviewRotation();

    //     // // --- 1?? 現在のカメラ位置を取得 ---
    //     // Vector3 startPos = actionCamera.transform.position;

    //     // // --- 2?? 目標Z座標を計算（攻撃者のZ * 2）---
    //     // float targetZ = attacker.position.z * 2.5f;
    //     // float targetX = target.position.x + 3f;

    //     // // --- 3?? 新しい位置（Zだけ変更）---
    //     // Vector3 endPos = new Vector3(targetX, startPos.y, targetZ);

    //     // // --- 4?? カメラを即時切り替え（LookAt維持）---
    //     SetCameraInstant(actionCamera);
    //     // SetupCameraLookOnly(actionCamera, target);

    //     // // --- 5?? コルーチンでZ方向スライド ---
    //     // StartCoroutine(MoveCameraZSmooth(startPos, endPos, attacker, 0.7f));
    // }

    /// <summary>
    /// カメラのZ座標だけをスムーズに移動させる
    /// </summary>
    // private IEnumerator MoveCameraZSmooth(Vector3 startPos, Vector3 endPos, Transform lookTarget, float duration)
    // {
    //     float t = 0f;

    //     while (t < 1f)
    //     {
    //         t += Time.deltaTime / duration;

    //         // 現在位置を補間（Zのみ変化）
    //         float z = Mathf.Lerp(startPos.z, endPos.z, t);
    //         Vector3 newPos = new Vector3(startPos.x, startPos.y, z);
    //         actionCamera.transform.position = newPos;

    //         // ターゲットを向き続ける
    //         if (lookTarget != null)
    //             actionCamera.transform.LookAt(lookTarget.position + Vector3.up * 1.5f);

    //         yield return null;
    //     }
    // }

    /// <summary>
    /// 攻撃終了時などに俯瞰カメラへ即切り替え
    /// </summary>
    public void SwitchToOverviewCamera()
    {
        SetCameraInstant(overviewCamera);
        StartOverviewRotation();
    }

    // ==============================
    // 内部Cinemachine設定
    // ==============================
    private void SetupCameraFollow(CinemachineCamera cam, Transform target, bool isPlayer, Vector3 offset, float distance)
    {
        cam.Follow = target;
        cam.LookAt = target;

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
    /// カメラの位置はそのまま、向きだけターゲットを追う設定
    /// </summary>
    private void SetupCameraLookOnly(CinemachineCamera cam, Transform target)
    {
        if (cam == null || target == null) return;

        // Followは固定。LookAtのみターゲットへ
        cam.Follow = null;
        cam.LookAt = target;

        // 現在位置を維持したまま向きだけ更新
        if (cam.TryGetComponent(out CinemachinePositionComposer composer))
        {
            // 位置制御を無効化（距離などを動かさないようにする）
            composer.CameraDistance = Vector3.Distance(cam.transform.position, target.position);
            composer.TargetOffset = Vector3.zero;
        }

        // 即座にターゲット方向を向く（Transformで強制更新）
        Vector3 dir = (target.position - cam.transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    // ==============================
    // カメラ優先度切り替え
    // ==============================
    private void SetCameraInstant(CinemachineCamera cam)
    {
        overviewCamera.Priority = 0;
        orbitCamera.Priority = 0;
        playerActionCamera.Priority = 0;
        playerActionCamera_1.Priority = 0;
        enemyActionCamera.Priority = 0;
        enemyActionCamera_1.Priority = 0;
        resultCamera.Priority = 0;
        cam.Priority = 20;

        if (brain != null)
        {
            var blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            brain.DefaultBlend = blend;
        }
    }
}
