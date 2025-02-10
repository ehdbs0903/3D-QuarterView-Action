using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed;
    public float jumpPower;
    public GameObject[] weapons;
    public bool[] hasWeapons;

    float hAxis;
    float vAxis;
    bool walkDown;
    bool jumpDown;
    bool interactionDown;
    bool swapDown1;
    bool swapDown2;
    bool swapDown3;

    bool isJump;
    bool isDodge;
    bool isSwap;

    Vector3 moveVec;
    Vector3 dodgeVec;

    Rigidbody rb;
    Animator anim;

    GameObject nearObject;
    GameObject equipWeapon;
    int equipWeaponIdx = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        GetInput();
        Move();
        Turn();

        if (jumpDown && moveVec != Vector3.zero && !isJump && !isDodge && !isSwap) {
            Dodge();
        }

        if ((swapDown1 || swapDown2 || swapDown3) && !isJump && !isDodge && !isSwap)
        {
            Swap();
        }

        if (interactionDown && nearObject != null && !isJump && !isDodge && !isSwap)
        {
            Interaction();
        }
    }

    void FixedUpdate()
    {
        if (jumpDown && moveVec == Vector3.zero && !isJump && !isDodge && !isSwap)
        {
            Jump();
        }
    }

    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        walkDown = Input.GetButton("Walk");
        jumpDown = Input.GetButton("Jump");
        interactionDown = Input.GetButtonDown("Interaction");
        swapDown1 = Input.GetButtonDown("Swap1");
        swapDown2 = Input.GetButtonDown("Swap2");
        swapDown3 = Input.GetButtonDown("Swap3");
    }

    void Move()
    {
        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        if (isDodge)
            moveVec = dodgeVec;

        if (isSwap)
            moveVec = Vector3.zero;

        transform.position += moveVec * moveSpeed * (walkDown ? 0.3f : 1f) * Time.deltaTime;

        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", walkDown);
    }

    void Turn()
    {
        transform.LookAt(transform.position + moveVec);
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        anim.SetBool("isJump", true);
        anim.SetTrigger("doJump");
        isJump = true;
    }

    void Dodge()
    {
        dodgeVec = moveVec;
        moveSpeed *= 2;
        anim.SetTrigger("doDodge");
        isDodge = true;
        StartCoroutine(DodgeOutCoroutine());
    }

    void DodgeOut()
    {
        moveSpeed *= 0.5f;
        isDodge = false;
    }

    IEnumerator DodgeOutCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        DodgeOut();
    }

    void Swap()
    {
        if (swapDown1 && (!hasWeapons[0] || equipWeaponIdx == 0))
            return;
        if (swapDown2 && (!hasWeapons[1] || equipWeaponIdx == 1))
            return;
        if (swapDown3 && (!hasWeapons[2] || equipWeaponIdx == 2))
            return;


        int weaponIdx = -1;

        if (swapDown1) weaponIdx = 0;
        if (swapDown2) weaponIdx = 1;
        if (swapDown3) weaponIdx = 2;

        if (equipWeapon != null)
            equipWeapon.SetActive(false);

        equipWeaponIdx = weaponIdx;
        equipWeapon = weapons[weaponIdx];
        equipWeapon.SetActive(true);

        anim.SetTrigger("doSwap");

        isSwap = true;

        StartCoroutine (SwapOutCoroutine());
    }

    void SwapOut()
    {
        isSwap = false;
    }

    IEnumerator SwapOutCoroutine()
    {
        yield return new WaitForSeconds(0.4f);

        SwapOut();
    }

    void Interaction()
    {
        if (interactionDown && nearObject != null && !isJump && !isDodge)
        {
            if (nearObject.tag == "Weapon")
            {
                Item item = nearObject.GetComponent<Item>();
                int weaponIdx = item.value;
                hasWeapons[weaponIdx] = true;

                Destroy(nearObject);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            anim.SetBool("isJump", false);
            isJump = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Weapon")
        {
            nearObject = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Weapon")
        {
            nearObject = null;
        }
    }
}
