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
    // ? ��]�J�����ݒ�
    // ==============================
    [Header("Overview Rotation Settings")]
    public Vector3 battleCenter = Vector3.zero; // �Œ蒆�S
    public float rotationSpeed = 10f;  // ��]���x�i�x/�b�j
    public float rotationRadius = 10f; // ��ꒆ�S����̋���
    public float baseHeight = 5f;      // ��{����
    public float heightAmplitude = 0.5f; // �㉺��ꕝ
    public float zoomAmplitude = 1.0f;   // �Y�[���̐U�ꕝ
    public float zoomSpeed = 0.5f;       // �Y�[���ω����x
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
            // ��]�p�x���X�V�i���x����邭�h�炷�j
            float speedMod = 1f + Mathf.Sin(Time.time * 0.3f) * 0.2f;
            angle += rotationSpeed * speedMod * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;

            // ��邢�㉺�h��
            float yOffset = Mathf.Sin(Time.time * 0.5f) * heightAmplitude;

            // ��邢�Y�[���ω�
            float currentRadius = rotationRadius + Mathf.Sin(Time.time * zoomSpeed) * zoomAmplitude;

            // �J�����ʒu�X�V
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * currentRadius;
            Vector3 pos = battleCenter + offset + Vector3.up * (baseHeight + yOffset);

            overviewCamera.transform.position = pos;
            overviewCamera.transform.LookAt(battleCenter + Vector3.up * 1.5f);
        }
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

    public void PlayFrontShot(Transform attacker, bool isEnemy, float duration = 1.5f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToFront(attacker, isEnemy, duration));
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
    public void PlayHitReactionCamera(Transform target, float duration = 1f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchToHit(target, duration));
    }

    private IEnumerator SwitchToFront(Transform target, bool isEnemy, float duration)
    {
        if (isEnemy) {
            SetupCameraFollow(actionCamera, target, new Vector3(0, 0, 0), 8f); // �ߋ���
            actionCamera.transform.rotation = Quaternion.Euler(30f, -30f, 0); // ���ʂ���B�e
        }
        else {
            SetupCameraFollow(actionCamera, target, new Vector3(0, 0, 0), 8f); // �ߋ���
            actionCamera.transform.rotation = Quaternion.Euler(30f, 210f, 0); // ���ʂ���B�e
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
        StartOverviewRotation(); // �U���I����A�Ăщ�]���ĊJ
    }

    private IEnumerator SwitchToHit(Transform target, float duration)
    {
        SetupCameraFollow(actionCamera, target, new Vector3(0, 0, 0), 3.5f);
        SetCamera(actionCamera);
        yield return new WaitForSeconds(duration);
        SetCamera(overviewCamera);
    }

    /// <summary>
    /// CinemachineCamera�ɒǏ]�ݒ��K�p
    /// </summary>
    private void SetupCameraFollow(CinemachineCamera cam, Transform target, Vector3 offset, float distance)
    {
        cam.Follow = target;
        cam.LookAt = target;

        // ���݂�Cinemachine v3�ł́AFramingTransposer�́gBody�h�ɒ��ڃA�N�Z�X
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
        // �D��x����u�Ő؂�ւ���
        overviewCamera.Priority = 0;
        actionCamera.Priority = 0;
        cam.Priority = 20;

        // CinemachineBrain��Blend���ꎞ�I�ɖ�����
        if (brain != null)
        {
            var blend = new Unity.Cinemachine.CinemachineBlendDefinition(
                Unity.Cinemachine.CinemachineBlendDefinition.Styles.Cut, 0f);
            brain.DefaultBlend = blend;
        }

    }

}
