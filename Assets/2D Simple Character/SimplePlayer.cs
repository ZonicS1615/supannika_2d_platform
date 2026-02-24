using UnityEngine;

public class SimplePlayer : MonoBehaviour
{
    private Rigidbody2D rigid; //สำหรับการเคลื่อนที่
    private Animator anim; //สำหรับอนิเมชัน
    private ParticleSystem grassPar; // Particle หญ้า
    private ParticleSystem.EmissionModule emission; // สั่งจำนวนของ Particle

    [Header("Ground And Wall Check")]
    [SerializeField] private float groundDistCheck = 1f; //SerializeField ให้ไปโผล่ที่ Inspector //ระยะ sensor ที่วิ่งไปชนพื้น
    [SerializeField] private float wallDistCheck = 1f; //ระยะ sensor ที่วิ่งไปชนผนัง
    [SerializeField] private LayerMask groundLayer; //หาเฉพาะ layer ของพื้น
    public bool isGrounded = false; // ตรวจชนพื้น
    public bool isWalled = false; // ตรวจชนกำแพง

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f; // ความเร็วในการเคลื่อนที่แนวราบ
    public float X_input; // ปุ่ม A,D
    public float Y_input; // ปุ่ม S กดให้ Slide เร็ว

    [Header("Jump")]
    [SerializeField] private float jumpForce = 20f; // แรงกระโดด
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f); // แรงกระโดด WallJump
    public bool isJumping = false; 
    public bool isWallJumping = false; 
    public bool isWallSliding = false; 
    public bool canDoubleJump = false; // doubleJump ได้เพียงครั้งเดียวต่อการกระโดด
    public int facing = 1; // หันหน้าตรงข้ามกับผนัง เวลา wallJumping

    [SerializeField] private float coyoteTimeLimit = .5f; // ระยะเวลาที่สามารถกดเพื่อโดดกลางอากาศได้
    [SerializeField] private float bufferTimeLimit = .5f; // ระยะเวลาที่สามารถกดเพื่อโดดก่อนจะถึงพื้นได้
    public float coyoteTime = -10000f; // เวลาเริ่มที่ยอมให้กดกระโดดกลางอากาศได้
    public float bufferTime = -10000f; // เวลาเริ่มที่ยอมให้กดกระโดดก่อนจะถึงพื้นได้

    private void Awake() //ทำงานก่อนที่จะเข้ามา play ในเกม // Start สีฟ้า เป็นของUnity 
    {
        rigid = GetComponent<Rigidbody2D>();// ถูก เพราะอยู่ที่ตัวแม่ gameObject นี้
        anim = GetComponentInChildren<Animator>();// ใช้ InChidren เพราะ Animator อยู่ที่ลูก
        grassPar = GetComponentInChildren<ParticleSystem>(); // เอาตัว Particle จากลูก
        emission = grassPar.emission; // ดึงข้อมูล emission
    }

    private void Update() //ทำงานทุก frame
    {
        JumpState(); //ตรวจสถานะว่า อยู่บนพื้น กระโดด ลงพื้น หรือ wallSlide
        Jump(); //สั่งกระโดด
        WallSlide(); //สั่ง wallSlide
        InputVal(); //ตรวจ input จากผู้เล่น
        Move(); //สั่งเคลื่อนไหวทั้งบนพื้นและอากาศ
        Flip(); //สั่งหันหน้าไปทางทิศการเคลื่อนที่อัตโนมัติ
        GroundAndWallCheck(); //ตรวจจับพื้นและผนัง
        Animation(); //สั่ง animation
    }

    private void JumpState() // ตรวจสถานะตัวละคร
    {
        if(!isGrounded && !isJumping) // fall, takeoff
        {
            isJumping = true; // โดดอยู่

            if(rigid.linearVelocityY <= 0f) // fall หล่นอยู่
            {
                coyoteTime = Time.time; // เริ่มนับเวลา coyote
            }
        }
        if(isGrounded && isJumping) // landing
        {
            isJumping= false;
            isWallJumping = false;
            isWallSliding = false;
            canDoubleJump = false;
        }
        if (isWalled) // ตรวจ wallSliding
        {
            isJumping = false;
            isWallJumping = false;
            canDoubleJump = false;
            if (isGrounded) // ถ้าอยู๋บนพื้น
            {
                isWallSliding = false;
            }
            else // ถ้าไม่อยู๋บนพื้น
            {
                isWallSliding = true;
            }
        }
        else // ถ้าไม่ติดกำแพง
        {
            isWallSliding = false;
        }
    }

    private void Jump()
    {
        if(Input.GetKeyDown(KeyCode.Space)) // ถ้ากด spacebar
        {
            if (!isWalled) // ถ้าไม่ติดกำแพง
            {
                if (isGrounded) // ถ้าอยู่บนพื้น
                {
                    canDoubleJump = true;
                    rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); // ***normalJump
                }
                else  // ถ้าไม่อยู่บนพื้น // doubleJump / coyoteJump
                {
                    if(rigid.linearVelocityY > 0f && canDoubleJump) // ***doubleJump
                    {
                        canDoubleJump = false; // ใช้โควตาโดดไปแล้วเลยเป็น false doubleJump ซ้ำไม่ได้
                        rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce);
                    }
                    if (rigid.linearVelocityY <= 0f)
                    {
                        if (Time.time < coyoteTime + coyoteTimeLimit) // *** coyoteJump
                        {
                            coyoteTime = 0f;
                            rigid.linearVelocity = new Vector2(rigid.linearVelocityX, jumpForce); //โดด
                        }
                        else 
                        {
                            bufferTime = Time.time; // เริ่มจับ bufferJump
                        }
                    }
                }
            }
            else // ถ้าติดกำแพง แสดงว่าเป็น wallJump
            {
                isWallJumping = true; // จะได้ออกจาก wallSliding
                rigid.linearVelocity = new Vector2(wallJumpForce.x * facing, wallJumpForce.y); //***wallJump
            }
        }
        else // ถ้าไม่กด จะเป็น bufferJump
        {
            if(isGrounded && Time.time < bufferTime + bufferTimeLimit) // ถ้าอยู่ที่พื้น และอยู๋ในเวลาที่ buffer ได้
            {
                rigid.linearVelocity = new Vector2 (rigid.linearVelocityX, jumpForce); // ***bufferJump
            }
        }
    }
    
    private void WallSlide()
    {
        if (!isWalled || isGrounded || isWallJumping || rigid.linearVelocityY > 0f)
            return; //ข้ามบรรทัดที่เหลือ

        float Y_slide = Y_input < 0f ? 1f : .5f; // ถ้ากด s ตกเร็วขึ้น
        rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY * Y_slide); // ตกช้าลง
    }

    private void InputVal()
    {
        X_input = Input.GetAxisRaw("Horizontal"); // GetAxisRaw() ข้อมูลแบบหยาบ
        Y_input = Input.GetAxisRaw("Vertical");
    }

    private void Move()
    {
        if(isWallJumping) // ถ้า isWallJumping อยู่ ให้ออกจากการควบคุมจาก player
            return; // มันจะไม่อ่านบรรทัดถัดไปเลย

        if (isGrounded) // ถ้าอยู่บนพื้น
        {
            rigid.linearVelocity = new Vector2(X_input * moveSpeed, rigid.linearVelocityY); // ออกแรงผลัก rigid ให้เคลื่อนที่
        }
        else // ถ้าลอยกลางอากาศ
        {
            float X_airMove = X_input != 0f ? X_input * moveSpeed : rigid.linearVelocityX; // แกน x ถ้าไม่กดจะเคลื่อนที่ตามแรง physic
            rigid.linearVelocity = new Vector2(X_airMove, rigid.linearVelocityY);
        }
    }

    private void Flip() // หมุนตัวละคร ตามการทิศเคลื่อนที่
    {
        if(rigid.linearVelocityX > 0.01f) // ถ้าผลักไปทางขวามือ
        {
            facing = -1; // หันหน้าออกจากกำแพง
            transform.rotation = Quaternion.identity; // หันตอนเริ่ม
        }
        else if(rigid.linearVelocityX < -0.01f)// ถ้าไม่ผลักไปทางขวา
        {
            facing = 1; // หันหน้าออกจากกำแพง
            transform.rotation = Quaternion.Euler(0f ,180f ,0f); // หันไป 180 องศา
        }
    }

    private void GroundAndWallCheck()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundDistCheck, groundLayer); // sensor พื้น
        isWalled = Physics2D.Raycast(transform.position, transform.right, wallDistCheck, groundLayer); // sensor ผนัง
    }

    private void OnDrawGizmos() // กราฟฟิกแสดงผลของ sensor ตรวจจับพื้นและผนัง
    {
        Gizmos.color = Color.blue; //เส้นสีน้ำเงิน
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistCheck); //เส้น sensor ตรวจพื้น
        Gizmos.color = Color.red; //เส้นสีแดง
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallDistCheck); //เส้น sensor ตรวจผนัง
    }

    private void Animation()
    {
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallSliding", isWallSliding);
        /*if (isWalled) //ถ้าติดกำแพงจะหยุดเดิน
        {
            anim.SetFloat("velX", 0f); //จะหยุด run
        }
        else 
        {
            anim.SetFloat("velX", rigid.linearVelocityX); จะ idle หรือ run
        }*/

        anim.SetFloat("velX", rigid.linearVelocityX); // จะ idle หรือ run
        anim.SetFloat("velY", rigid.linearVelocityY); // จะ กระโดด หรือ ร่วง

        emission.enabled = isGrounded;

    }
}
