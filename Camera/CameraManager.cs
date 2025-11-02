using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// �o�g�����̃J��������iCinemachine v3�Ή��j
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Cameras")]
    [Tooltip("�S�̂���Ղŉf���J����")]
    public CinemachineCamera overviewCamera;

    [Tooltip("�S�̂���]���ĉf���J����")]
    public CinemachineCamera orbitCamera;
    private CinemachineOrbitalFollow orbitalFollow;

    [Tooltip("�U���L�������f���A�N�V�����J����")]
    public CinemachineCamera playerActionCamera;
    public CinemachineCamera playerActionCamera_1;
    public CinemachineCamera enemyActionCamera;
    public CinemachineCamera enemyActionCamera_1;


    [Tooltip("���U���g��ʂ��f���J����")]
    public CinemachineCamera resultCamera;

    private CinemachineBrain brain;

    // ==============================
    // ? ���ՃJ�����ݒ�
    // ==============================
    [Header("Overview Rotation Settings")]
    [Tooltip("�퓬�̒��S�_")]
    public Vector3 battleCenter = Vector3.zero;

    [Tooltip("��]���x�i�x/�b�j")]
    public float rotationSpeed = 10f;

    [Tooltip("��]�Ԋu�i��/�b�j")]
    public float rotationInterval = 10f;

    [Tooltip("��]���a�i��ꒆ�S����̋����j")]
    public float rotationRadius = 10f;

    [Tooltip("�J�����̍���")]
    public float baseHeight = 5f;

    [Tooltip("�㉺�̗h�ꕝ")]
    public float heightAmplitude = 0.5f;

    [Tooltip("�Y�[���̐U�ꕝ")]
    public float zoomAmplitude = 1.0f;

    [Tooltip("�Y�[���ω����x")]
    public float zoomSpeed = 0.5f;

    [Tooltip("�����p�x�i�x�j")]
    public float initialAngle = 45f;

    private bool rotateOverview = false;
    private float angle;

    // ==============================
    // ������
    // ==============================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // �V�[�����܂����ł��ێ�

        brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogWarning("CinemachineBrain��MainCamera�Ɍ�����܂���B");
            return;
        }

        angle = initialAngle;
        // SetCameraInstant(overviewCamera);
        SetCameraInstant(orbitCamera);
        Debug.Log("[CameraManager] �J��������������");
    }

    // ==============================
    // �X�V�����i���ՃJ������]�j
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
    // ���J����֐�
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
    // ���U���g�J�������o
    // ==============================
    public void SwitchToResultCamera()
    {
        StopOverviewRotation();

        // �J�����ʒu�E�����ݒ�i����������j
        // actionCamera.transform.position = new Vector3(0f, 5f, -20f);
        // actionCamera.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

        // �J�����𑦎��؂�ւ�
        SetCameraInstant(resultCamera);
    }

    // ==============================
    // �V�̃J�������o
    // ==============================
    public void SwitchToOrbitCamera()
    {
        orbitalFollow = orbitCamera.GetComponent<CinemachineOrbitalFollow>();
        SetCameraInstant(orbitCamera);

        // 0��360�x��5�b�Ń��[�v�Đ�
        DOTween.To(() => orbitalFollow.HorizontalAxis.Value, x =>
        {
            orbitalFollow.HorizontalAxis.Value = x;
        }, 360f, rotationInterval)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart); // �������[�v
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
    /// �U���J�n���ȂǂɃA�N�V�����J�����֑��؂�ւ�
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
    /// �U���J�n���ȂǂɃA�N�V�����J�����֑��؂�ւ�
    /// �^�[�Q�b�g�����������Ȃ���AZ���W�݈̂ړ�����
    /// </summary>
    // public void SwitchToActionCamera(Transform target, bool isPlayer, Transform attacker)
    // {
    //     if (target == null || actionCamera == null || attacker == null) return;


    //     actionCamera.Target.TrackingTarget = attacker;
    //     actionCamera.Target.LookAtTarget = attacker;
    //     // StopOverviewRotation();

    //     // // --- 1?? ���݂̃J�����ʒu���擾 ---
    //     // Vector3 startPos = actionCamera.transform.position;

    //     // // --- 2?? �ڕWZ���W���v�Z�i�U���҂�Z * 2�j---
    //     // float targetZ = attacker.position.z * 2.5f;
    //     // float targetX = target.position.x + 3f;

    //     // // --- 3?? �V�����ʒu�iZ�����ύX�j---
    //     // Vector3 endPos = new Vector3(targetX, startPos.y, targetZ);

    //     // // --- 4?? �J�����𑦎��؂�ւ��iLookAt�ێ��j---
    //     SetCameraInstant(actionCamera);
    //     // SetupCameraLookOnly(actionCamera, target);

    //     // // --- 5?? �R���[�`����Z�����X���C�h ---
    //     // StartCoroutine(MoveCameraZSmooth(startPos, endPos, attacker, 0.7f));
    // }

    /// <summary>
    /// �J������Z���W�������X���[�Y�Ɉړ�������
    /// </summary>
    // private IEnumerator MoveCameraZSmooth(Vector3 startPos, Vector3 endPos, Transform lookTarget, float duration)
    // {
    //     float t = 0f;

    //     while (t < 1f)
    //     {
    //         t += Time.deltaTime / duration;

    //         // ���݈ʒu���ԁiZ�̂ݕω��j
    //         float z = Mathf.Lerp(startPos.z, endPos.z, t);
    //         Vector3 newPos = new Vector3(startPos.x, startPos.y, z);
    //         actionCamera.transform.position = newPos;

    //         // �^�[�Q�b�g������������
    //         if (lookTarget != null)
    //             actionCamera.transform.LookAt(lookTarget.position + Vector3.up * 1.5f);

    //         yield return null;
    //     }
    // }

    /// <summary>
    /// �U���I�����Ȃǂɘ��ՃJ�����֑��؂�ւ�
    /// </summary>
    public void SwitchToOverviewCamera()
    {
        SetCameraInstant(overviewCamera);
        StartOverviewRotation();
    }

    // ==============================
    // ����Cinemachine�ݒ�
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
    /// �J�����̈ʒu�͂��̂܂܁A���������^�[�Q�b�g��ǂ��ݒ�
    /// </summary>
    private void SetupCameraLookOnly(CinemachineCamera cam, Transform target)
    {
        if (cam == null || target == null) return;

        // Follow�͌Œ�BLookAt�̂݃^�[�Q�b�g��
        cam.Follow = null;
        cam.LookAt = target;

        // ���݈ʒu���ێ������܂܌��������X�V
        if (cam.TryGetComponent(out CinemachinePositionComposer composer))
        {
            // �ʒu����𖳌����i�����Ȃǂ𓮂����Ȃ��悤�ɂ���j
            composer.CameraDistance = Vector3.Distance(cam.transform.position, target.position);
            composer.TargetOffset = Vector3.zero;
        }

        // �����Ƀ^�[�Q�b�g�����������iTransform�ŋ����X�V�j
        Vector3 dir = (target.position - cam.transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    // ==============================
    // �J�����D��x�؂�ւ�
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
