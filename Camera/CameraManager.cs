using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// �o�g�����̃J��������iCinemachine v3�Ή��j
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("�S�̂���Ղŉf���J����")]
    public CinemachineCamera overviewCamera;

    [Tooltip("�U���L�������f���A�N�V�����J����")]
    public CinemachineCamera actionCamera;

    private CinemachineBrain brain;
    private Coroutine currentRoutine;

    // ==============================
    // ? ���ՃJ�����ݒ�iInspector�Œ����\�j
    // ==============================
    [Header("Overview Rotation Settings")]
    [Tooltip("�퓬�̒��S�_")]
    public Vector3 battleCenter = Vector3.zero;

    [Tooltip("��]���x�i�x/�b�j")]
    public float rotationSpeed = 10f;

    [Tooltip("��]���a�i��ꒆ�S����̋����j")]
    public float rotationRadius;
    public float bossRotationRadius;

    [Tooltip("�J�����̍���")]
    public float baseHeight = 5f;

    [Tooltip("�{�X�����X�^�[���B��Ƃ���Z���I�t�Z�b�g")]
    public float lookAtZOffsetBoss = -2f;

    [Tooltip("�㉺�̗h�ꕝ")]
    public float heightAmplitude = 0.5f;

    [Tooltip("�Y�[���̐U�ꕝ")]
    public float zoomAmplitude = 1.0f;

    [Tooltip("�Y�[���ω����x")]
    public float zoomSpeed = 0.5f;

    [Tooltip("�����p�x�i�x�j")]
    public float initialAngle = 45f;

    [Tooltip("�{�X���f���Ƃ��ɋ��������{�ɂ��邩")]
    public float bossDistanceMultiplier = 2.0f;

    private bool rotateOverview = false;
    private float angle;
    private bool isBossMode = false;


    void Start()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogWarning("CinemachineBrain��MainCamera�Ɍ�����܂���B");
            return;
        }

        angle = initialAngle; // �� Inspector�Ŏw�肵�������p�x����X�^�[�g
        if (isBossMode) {
            rotationRadius = bossRotationRadius;
        }
        SetCameraInstant(overviewCamera);
        Debug.Log("[CameraManager] �J��������������");
    }

    void Update()
    {
        if (rotateOverview)
        {
            // ��]�p�x���X�V�i�ɂ₩�Ȃ�炬�j
            float speedMod = 1f + Mathf.Sin(Time.time * 0.3f) * 0.2f;
            angle += rotationSpeed * speedMod * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;

            // �㉺���
            float yOffset = Mathf.Sin(Time.time * 0.5f) * heightAmplitude;

            // �Y�[���ω�
            float currentRadius = rotationRadius + Mathf.Sin(Time.time * zoomSpeed) * zoomAmplitude;

            // �J�����ʒu
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * currentRadius;
            Vector3 pos = battleCenter + offset + Vector3.up * (baseHeight + yOffset);

            overviewCamera.transform.position = pos;
            overviewCamera.transform.LookAt(battleCenter + Vector3.up * 1.5f);
        }
    }

    public void SetBossMode(bool isBoss)
    {
        isBossMode = isBoss;
        Debug.Log($"[CameraManager] �{�X���[�h: {isBoss}");
    }

    // ==============================
    // ��]����
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
    /// �U�����̃J�������o
    /// </summary>
    public void PlayAttackCamera(Transform attacker, bool isEnemy, float duration = 0f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToAction(attacker, isEnemy, duration));
    }

    /// <summary>
    /// ��e���̃J������艉�o
    /// </summary>
    public void PlayHitReactionCamera(Transform target, bool isEnemy, float duration = 1f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToHit(target, isEnemy, duration));
    }

    // ==============================
    // �������o�R���[�`��
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
    // Cinemachine�ݒ�
    // ==============================
    private void SetupCameraFollow(CinemachineCamera cam, Transform target, bool isEnemy,  Vector3 offset, float distance)
    {
        cam.Follow = target;
        cam.LookAt = target;

        if (isBossMode && isEnemy) {
            // ���݂̃J�����ʒu���擾
            Vector3 camPos = cam.transform.position;

            // target��forward�����ɁAZ�I�t�Z�b�g�����Z�i�ʒu�݈̂ړ��j
            camPos += target.forward * lookAtZOffsetBoss;
            camPos += target.right * -5;

            // �J�����̈ʒu���X�V�i�����͕ύX���Ȃ��j
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
    /// �D��x�ŃJ������؂�ւ���
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
