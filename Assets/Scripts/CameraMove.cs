using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    [SerializeField] bool VerticalMovement;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float lookSpeed = 1f;

    float pitch = 0f;
    Vector2 moveInput;
    Vector2 lookInput;

    void Update()
    {
        Vector4 moveVector = moveSpeed * ((transform.forward * moveInput.y) + (transform.right * moveInput.x)) * Time.deltaTime;
        transform.position += new Vector3(moveVector.x, VerticalMovement ? moveVector.y : 0, moveVector.z);

        float yaw = lookInput.x * lookSpeed;
        float pitchChange = lookInput.y * lookSpeed;
        pitch = Mathf.Clamp(pitch - pitchChange, -90, 90);
        transform.eulerAngles = new Vector3(pitch, transform.eulerAngles.y + yaw, 0);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}
