using UnityEngine;

// The SpriteRenderer is an asset slot for a 2D street illustration. Moving placeholders remain optional foreground.
public class OfficeStreetMotion : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backdropImage;
    [SerializeField] private GameObject placeholderCity;
    [SerializeField] private Vector2 backdropWorldSize = new Vector2(4.3f, 2.4f);
    [SerializeField] private Transform[] pedestrians;
    [SerializeField] private Transform[] cars;
    [SerializeField] private float pedestrianSpeed = .45f;
    [SerializeField] private float carSpeed = 1.15f;
    [SerializeField] private float halfWidth = 2.25f;
    private void Start()
    {
        RefreshBackdrop();
    }
    public void RefreshBackdrop()
    {
        bool hasImage = backdropImage != null && backdropImage.sprite != null;
        if (placeholderCity != null) placeholderCity.SetActive(!hasImage);
        if (!hasImage) return;
        Vector2 spriteSize = backdropImage.sprite.bounds.size;
        if (spriteSize.x > 0 && spriteSize.y > 0)
            backdropImage.transform.localScale = new Vector3(backdropWorldSize.x / spriteSize.x,
                backdropWorldSize.y / spriteSize.y, 1f);
    }
    private void Update()
    {
        for (int i = 0; i < pedestrians.Length; i++) Move(pedestrians[i], (i % 2 == 0 ? 1f : -1f) * pedestrianSpeed);
        for (int i = 0; i < cars.Length; i++) Move(cars[i], (i % 2 == 0 ? 1f : -1f) * carSpeed);
    }
    private void Move(Transform mover, float velocity)
    {
        if (mover == null) return;
        Vector3 position = mover.localPosition;
        position.x += velocity * Time.deltaTime;
        if (position.x > halfWidth) position.x = -halfWidth;
        else if (position.x < -halfWidth) position.x = halfWidth;
        mover.localPosition = position;
    }
}
