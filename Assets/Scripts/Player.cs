using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed;
    public float jumpPower;
    public GameObject[] weapons;
    public bool[] hasWeapons;
    public GameObject[] grenades;
    public GameObject grenadeObj;
    public Camera followCamera;
    public GameManager manager;

    public int ammo;
    public int coin;
    public int health;
    public int score;
    public int hasGrenades;

    public int maxAmmo;
    public int maxCoin;
    public int maxHealth;
    public int maxHasGrenades;

    float hAxis;
    float vAxis;
    bool walkDown;
    bool jumpDown;
    bool interactionDown;
    bool fireDown;
    bool grenadeDown;
    bool reloadDown;
    bool swapDown1;
    bool swapDown2;
    bool swapDown3;

    bool isJump;
    bool isDodge;
    bool isSwap;
    bool isReload;
    bool isFireReady = true;
    bool isBorder;
    bool isDamaged;
    bool isShop;
    bool isDead;

    Vector3 moveVec;
    Vector3 dodgeVec;

    Rigidbody rb;
    Animator anim;
    MeshRenderer[] meshRenderers;

    GameObject nearObject;
    public Weapon equipWeapon;
    int equipWeaponIdx = -1;
    float fireDelay;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();

        Debug.Log(PlayerPrefs.GetInt("MaxScore"));
        //PlayerPrefs.SetInt("MaxScore", 112500);
    }

    void Update()
    {
        GetInput();
        Move();
        Turn();

        if (jumpDown && moveVec != Vector3.zero && !isJump && !isDodge && !isSwap && !isShop && !isDead) {
            Dodge();
        }

        if ((swapDown1 || swapDown2 || swapDown3) && !isJump && !isDodge && !isSwap && !isShop && !isDead)
        {
            Swap();
        }

        if (interactionDown && nearObject != null && !isJump && !isDodge && !isSwap && !isShop && !isDead)
        {
            Interaction();
        }

        Attack();
        Grenade();

        if (reloadDown && !isJump && !isDodge && !isSwap && isFireReady && !isShop && !isDead)
        {
            Reload();
        }
    }

    void FixedUpdate()
    {
        if (jumpDown && moveVec == Vector3.zero && !isJump && !isDodge && !isSwap && !isShop && !isDead)
        {
            Jump();
        }

        FreezeRotation();
        StopAtWall();
    }

    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        walkDown = Input.GetButton("Walk");
        jumpDown = Input.GetButton("Jump");
        interactionDown = Input.GetButtonDown("Interaction");
        fireDown = Input.GetButton("Fire1");
        grenadeDown = Input.GetButtonDown("Fire2");
        reloadDown = Input.GetButtonDown("Reload");
        swapDown1 = Input.GetButtonDown("Swap1");
        swapDown2 = Input.GetButtonDown("Swap2");
        swapDown3 = Input.GetButtonDown("Swap3");
    }

    void Move()
    {
        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        if (isDodge)
            moveVec = dodgeVec;

        if (isSwap || isReload || !isFireReady || isDead)
            moveVec = Vector3.zero;

        if (!isBorder)
            transform.position += moveVec * moveSpeed * (walkDown ? 0.3f : 1f) * Time.deltaTime;

        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", walkDown);
    }

    void Turn()
    {
        transform.LookAt(transform.position + moveVec);

        if (fireDown && !isDead)
        {
            Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit;

            if (Physics.Raycast(ray, out rayHit, 100))
            {
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 0;
                transform.LookAt(transform.position + nextVec);
            }
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        anim.SetBool("isJump", true);
        anim.SetTrigger("doJump");
        isJump = true;
    }

    void Attack()
    {
        if (equipWeapon == null)
            return;

        fireDelay += Time.deltaTime;
        isFireReady = equipWeapon.attackRate < fireDelay;

        if (fireDown && isFireReady && !isDodge && !isSwap && !isDead)
        {
            equipWeapon.Use();
            anim.SetTrigger(equipWeapon.type == Weapon.Type.Melee ? "doSwing" : "doShot");
            fireDelay = 0;
        }

    }

    void Grenade()
    {
        if (hasGrenades == 0)
        {
            return;
        }

        if (grenadeDown && !isReload && !isSwap && !isDead)
        {
            Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit;

            if (Physics.Raycast(ray, out rayHit, 100))
            {
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 2;

                GameObject instantGrenade = Instantiate(grenadeObj, transform.position, transform.rotation);
                Rigidbody rigidGrenade = instantGrenade.GetComponent<Rigidbody>();
                rigidGrenade.AddForce(nextVec, ForceMode.Impulse);
                rigidGrenade.AddTorque(Vector3.back * 10, ForceMode.Impulse);

                hasGrenades--;
                grenades[hasGrenades].SetActive(false);
            }
        }
    }

    void Reload()
    {
        if (equipWeapon == null)
            return;

        if (equipWeapon.type == Weapon.Type.Melee)
            return;

        if (ammo == 0)
            return;

        anim.SetTrigger("doReload");
        isReload = true;

        Invoke("ReloadOut", 3f);
    }

    void ReloadOut()
    {
        int reAmmo = ammo < equipWeapon.maxAmmo ? ammo : equipWeapon.maxAmmo;
        equipWeapon.curAmmo = reAmmo;
        ammo -= reAmmo;
        isReload = false;
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
            equipWeapon.gameObject.SetActive(false);

        equipWeaponIdx = weaponIdx;
        equipWeapon = weapons[weaponIdx].GetComponent<Weapon>();
        equipWeapon.gameObject.SetActive(true);

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
            else if (nearObject.tag == "Shop")
            {
                Shop shop = nearObject.GetComponent<Shop>();
                shop.Enter(this);
                isShop = true;
            }
        }
    }

    void FreezeRotation()
    {
        rb.angularVelocity = Vector3.zero;
    }

    void StopAtWall()
    {
        isBorder = Physics.Raycast(transform.position, transform.forward, 5, LayerMask.GetMask("Wall"));
    }

    IEnumerator OnDamage(bool isBossAtk)
    {
        isDamaged = true;
        foreach (MeshRenderer mesh in meshRenderers)
        {
            mesh.material.color = Color.yellow;
        }

        if (isBossAtk)
            rb.AddForce(transform.forward * -25, ForceMode.Impulse);

        if (health <= 0 && !isDead)
        {
            OnDie();
        }

        yield return new WaitForSeconds(1f);

        isDamaged = false;
        foreach (MeshRenderer mesh in meshRenderers)
        {
            mesh.material.color = Color.white;
        }

        if (isBossAtk)
            rb.velocity = Vector3.zero;
    }

    void OnDie()
    {
        anim.SetTrigger("doDie");
        isDead = true;
        manager.GameOver();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            anim.SetBool("isJump", false);
            isJump = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Item")
        {
            Item item = other.GetComponent<Item>();

            switch (item.type)
            {
                case Item.Type.Ammo:
                    ammo += item.value;
                    if (ammo > maxAmmo)
                        ammo = maxAmmo;
                    break;
                case Item.Type.Coin:
                    coin += item.value;
                    if (coin > maxCoin)
                        coin = maxCoin;
                    break;
                case Item.Type.Heart:
                    health += item.value;
                    if (health > maxHealth)
                        health = maxHealth;
                    break;
                case Item.Type.Grenade:
                    if (hasGrenades == maxHasGrenades)
                        return;
                    grenades[hasGrenades].SetActive(true);
                    hasGrenades += item.value;
                    break;
            }
            Destroy(other.gameObject);
        }
        else if (other.tag == "EnemyBullet")
        {
            if (!isDamaged)
            {
                Bullet enemyBullet = other.GetComponent<Bullet>();
                health -= enemyBullet.damage;

                bool isBossAtk = other.name == "Boss Melee Area";
                StartCoroutine(OnDamage(isBossAtk));
            }

            if (other.GetComponent<Rigidbody>() != null)
            {
                Destroy(other.gameObject);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Weapon" || other.tag == "Shop")
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
        else if (other.tag == "Shop")
        {
            Shop shop = other.GetComponent<Shop>();
            shop.Exit();
            isShop = false;
            nearObject = null;
        }
    }
}
