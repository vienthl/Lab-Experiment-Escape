using System;
using UnityEngine;
using UnityEngine.Events;

// Khóa 1 nhóm cửa cho tới khi diệt hết quái theo từng đợt (wave), có giới hạn thời gian.
// Đặt trên 1 GameObject rỗng trong phòng; gán Door cần khóa + chia quái có sẵn vào từng wave.
public class LockdownRoomController : MonoBehaviour
{
    [Serializable]
    public class Wave
    {
        public EnemyHealth[] enemies;
        public Boss2Health[] bosses;
    }

    [Header("Cửa bị khóa cho tới khi xong")]
    public DoorController[] doorsToLock;

    [Header("Các đợt quái (đợt sau chỉ xuất hiện khi đợt trước chết hết)")]
    public Wave[] waves;

    [Header("Bắt đầu lockdown")]
    [Tooltip("Bật: khóa cửa ngay khi vào scene. Tắt: cần Collider2D (Is Trigger) trên object này, khóa khi player bước vào")]
    public bool autoStartOnLevelLoad = true;

    [Header("Giới hạn thời gian (giây, 0 = không giới hạn)")]
    public float timeLimit = 0f;

    [Header("Sự kiện")]
    public UnityEvent onTimeExpired;
    public UnityEvent onAllWavesCleared;

    int currentWaveIndex = -1;
    int aliveInCurrentWave;
    float remainingTime;
    bool started;
    bool finished;
    bool timeExpiredFired;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waves != null ? waves.Length : 0;
    public int EnemiesRemainingInWave => aliveInCurrentWave;
    public float RemainingTime => remainingTime;
    public bool HasTimeLimit => timeLimit > 0f;
    public bool IsCleared => finished;

    void Start()
    {
        // Ẩn hết quái mọi wave trước, tránh chúng active sẵn ngoài ý muốn khi mở scene.
        if (waves != null)
        {
            foreach (var wave in waves)
            {
                if (wave?.enemies != null)
                {
                    foreach (var enemy in wave.enemies)
                        if (enemy != null) enemy.gameObject.SetActive(false);
                }

                if (wave?.bosses != null)
                {
                    foreach (var boss in wave.bosses)
                        if (boss != null) boss.gameObject.SetActive(false);
                }
            }
        }

        if (autoStartOnLevelLoad)
            StartLockdown();
    }

    void Update()
    {
        if (!started || finished || timeExpiredFired || timeLimit <= 0f) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            timeExpiredFired = true;
            onTimeExpired?.Invoke();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (autoStartOnLevelLoad || started) return;
        if (!other.CompareTag("Player")) return;

        StartLockdown();
    }

    public void StartLockdown()
    {
        if (started) return;
        started = true;

        foreach (var door in doorsToLock)
        {
            if (door != null) door.SetLocked(true);
        }

        remainingTime = timeLimit;
        ActivateNextWave();
    }

    void ActivateNextWave()
    {
        currentWaveIndex++;

        if (waves == null || currentWaveIndex >= waves.Length)
        {
            Finish();
            return;
        }

        var wave = waves[currentWaveIndex];
        aliveInCurrentWave = 0;

        if (wave?.enemies != null)
        {
            foreach (var enemy in wave.enemies)
            {
                if (enemy == null) continue;

                enemy.gameObject.SetActive(true);
                enemy.OnDied += HandleEnemyDied;
                aliveInCurrentWave++;
            }
        }

        if (wave?.bosses != null)
        {
            foreach (var boss in wave.bosses)
            {
                if (boss == null) continue;

                boss.gameObject.SetActive(true);
                boss.OnDied += HandleBossDied;
                aliveInCurrentWave++;
            }
        }

        // Wave rỗng (designer để trống) — coi như xong ngay, qua wave kế.
        if (aliveInCurrentWave == 0)
            ActivateNextWave();
    }

    void HandleEnemyDied(EnemyHealth enemy)
    {
        enemy.OnDied -= HandleEnemyDied;
        aliveInCurrentWave--;

        if (aliveInCurrentWave <= 0)
            ActivateNextWave();
    }

    void HandleBossDied(Boss2Health boss)
    {
        boss.OnDied -= HandleBossDied;
        aliveInCurrentWave--;

        if (aliveInCurrentWave <= 0)
            ActivateNextWave();
    }

    void Finish()
    {
        finished = true;

        foreach (var door in doorsToLock)
        {
            if (door != null) door.SetLocked(false);
        }

        onAllWavesCleared?.Invoke();
    }
}
