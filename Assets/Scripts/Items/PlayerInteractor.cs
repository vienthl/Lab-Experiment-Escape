using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("Bán kính tìm vật phẩm quanh player")]
    public float interactRadius = 0.6f;

    PlayerInventory inventory;
    WorldItem currentTarget;

    public bool HasTarget => currentTarget != null;

    void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        WorldItem nearest = FindNearestItem();

        if (nearest != currentTarget)
        {
            if (currentTarget != null) currentTarget.SetHighlighted(false);
            if (nearest != null) nearest.SetHighlighted(true);
            currentTarget = nearest;
        }
    }

    WorldItem FindNearestItem()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);

        WorldItem nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var item = hit.GetComponentInParent<WorldItem>();
            if (item == null) continue;

            float dist = Vector2.Distance(transform.position, item.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = item;
            }
        }

        return nearest;
    }

    // Gọi giữa PickUpRoutine (PlayerMovement) khi F được bấm và HasTarget == true.
    public void Interact()
    {
        if (currentTarget == null || inventory == null) return;

        var item = currentTarget;
        currentTarget = null;

        var data = item.Collect(out int quantity);
        inventory.Add(data, quantity);
    }
}
