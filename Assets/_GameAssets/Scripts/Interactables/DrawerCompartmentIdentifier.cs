using UnityEngine;

/// <summary>
/// Bir çekmece gözünün (kompartımanının) kimliğini ve ilişkili DrawerController'ı tutar.
/// Bu script, collider'a sahip olan her bir çekmece gözü GameObject'ine eklenmelidir.
/// </summary>
public class DrawerCompartmentIdentifier : MonoBehaviour
{
    [Tooltip("Bu kompartımanın ait olduğu DrawerController. Inspector'dan atanabilir veya otomatik olarak üst objelerden bulunmaya çalışılır.")]
    public DrawerController drawerController;

    [Tooltip("Bu kompartımanın DrawerController içindeki indeksi (0, 1, 2 vb.). Inspector'dan doğru şekilde ayarlanmalıdır.")]
    public int compartmentIndex = 0;

    void Start()
    {
        // Eğer drawerController Inspector'dan atanmamışsa, üst objelerde aramayı dene.
        if (drawerController == null)
        {
            drawerController = GetComponentInParent<DrawerController>();
            if (drawerController == null)
            {
                Debug.LogError($"DrawerCompartmentIdentifier ({gameObject.name}): DrawerController bulunamadı. Lütfen Inspector'dan atayın veya üst objelerden birine DrawerController ekleyin.", this);
            }
        }
    }
} 