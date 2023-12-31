using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private enum TerrainState
    {
        Air,
        Water
    }
    private TerrainState terrainState;

    [SerializeField] private int jumpsRemaining;

//Inspector input
    [SerializeField] private float dashForce;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float boostDuration;
    [SerializeField] private float tripleJumpDuration;

    private int jumpsAvailable;
    private float boostFactor;

    [SerializeField] private float moveSpeed;

    public float MoveSpeed {
        get {return moveSpeed;}
        set {moveSpeed = value;}
    }

    [SerializeField] private float jumpVelocity;
    [SerializeField] private float coyoteTime; // Time between last grounded where the player can still jump midair; makes controller more fair and responsive
    [SerializeField] private KeyCode jumpKey;
    [SerializeField] private KeyCode dashKey;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask whatIsWater;
    [SerializeField] private Vector3 spawnPoint;
    [SerializeField] private float dashAmount;
    [SerializeField] private float dashCooldown;
    [SerializeField] private Color playerColor; // For death particles mainly 

    public Camera mainCam;
    public GameObject deathEffect;

    private Rigidbody2D rb2d;
    private float xInput; // Variable for the x-input (a&d or left & right)
    private bool isGrounded; // If the player is on the ground 
    private bool isInWater;
    private float lastGrounded; // Or airtime; time since the player was last grounded
    private bool canJump;
    private bool wishJump; // Jump queueing; no holding down the button to jump repeatedly, but pressing before the player is grouded will make the square jump as soon as it lands
    private float playerSize = 0.45f; // Appears to be 0.5 but hitbox (box collider) is slightly smaller to make it more fair
    private bool doubleJump;
    private bool canDash;
    private bool isDashing;

    public bool IsDashing
    {
        get {return isDashing;}
        set {isDashing = value;}
    }
    private float lastFacing;

    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        lastGrounded = 0f;
        canJump = true;
        canDash = true;
        lastFacing = 1;
        boostFactor = 1;
        jumpsRemaining = jumpsAvailable = 2;     
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics2D.BoxCast(transform.position, new Vector2(playerSize - 0.1f, playerSize), 0f, Vector2.down, 0.1f, whatIsGround);
        isInWater = Physics2D.BoxCast(transform.position, new Vector2(0.45f, 0.45f), 0f, Vector2.down, 0f, whatIsWater);
        Debug.Log(isInWater);   

        if(isInWater)
        {
            terrainState = TerrainState.Water;
        }
        else
        {
            terrainState = TerrainState.Air;
        }

        if(!isGrounded)
        {
            lastGrounded += Time.deltaTime;
        }

        else
        {
            lastGrounded = 0f;
            jumpsRemaining = jumpsAvailable;
        }
        
        getInput();

        transform.localScale = new Vector3(lastFacing * 0.5f, 0.5f, 0.5f);
    }

    void getInput()
    { 
        xInput = Input.GetAxisRaw("Horizontal");
        if(xInput != 0) lastFacing = xInput;

        if(Input.GetKeyDown(jumpKey) && !wishJump) wishJump = true; // Player can queue a jump as long as the jump key (w) is held
        if(Input.GetKeyUp(jumpKey)) wishJump = false;

        if(Input.GetKeyDown(dashKey) && canDash)
        {
            StartCoroutine(dash());
        }

        if(terrainState == TerrainState.Air) // Lol change this to a switch statement later
        {
            rb2d.gravityScale = 5f;
            rb2d.drag = 0f;

            if(isGrounded && !Input.GetKey(jumpKey)) doubleJump = false;

            if(wishJump && canJump && (lastGrounded < coyoteTime || doubleJump)) 
            {
                jump(); 
                wishJump = false;
                canJump = false; // canJump variable to prevent accidental double-jumping due to coyote time; implement a double-jumping mechanism that isn't actually a bug
                doubleJump = !doubleJump;

                Invoke("resetJump", coyoteTime + 0.1f); // Resets the jump after the coyote time period
            }
            if(Input.GetKeyDown(jumpKey) && jumpsRemaining > 0 && lastGrounded < coyoteTime){
            jumpsRemaining -= 1;
            rb2d.AddForce(Vector2.up * jumpHeight, ForceMode2D.Impulse);
            }
        }

        else if (terrainState == TerrainState.Water)
        {
            rb2d.gravityScale = 1f;
            rb2d.drag = 1f;

            if(wishJump)
            {
                jump();
            }
        }  
    }

    void resetJump() => canJump = true; // This is syntax for a one-line method

    void jump()
    {
        if(terrainState == TerrainState.Air)
            rb2d.velocity = new Vector2(rb2d.velocity.x, jumpVelocity);

        else if (terrainState == TerrainState.Water)
            rb2d.velocity = new Vector2(rb2d.velocity.x, jumpVelocity * 0.25f);
    }

    IEnumerator dash()
    {
        
        isDashing = true;
        canDash = false;

        rb2d.gravityScale = 0f;

        Vector2 distance = mainCam.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        Vector2 direction = distance.normalized;   

        rb2d.AddForce(direction * dashAmount, ForceMode2D.Impulse);

        lastFacing = direction.x / Mathf.Abs(direction.x);

        yield return new WaitForSeconds(0.2f);

        float afterDashVelo = direction.x / Mathf.Abs(direction.x) * moveSpeed;

        for(float t = 0.0f; t < 1f; t += Time.deltaTime / 0.1f)
        {
            rb2d.velocity = new Vector2(Mathf.Lerp(rb2d.velocity.x, afterDashVelo, t), Mathf.Lerp(rb2d.velocity.y, 0, t));
            yield return null;
        }

        isDashing = false;
        rb2d.gravityScale = 5f;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        // TODO: make a dash animation where the square turns into some sort of wind thing and follows the direction of the dash
    }
    
    void FixedUpdate()
    {
        if(isDashing)
            return;
        
        rb2d.velocity = new Vector2(xInput * moveSpeed * 100f * Time.fixedDeltaTime, rb2d.velocity.y);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if(col.gameObject.tag == "Kill")
        {   
            StopAllCoroutines();
            gameObject.SetActive(false);

            GameObject deathParticles = Instantiate(deathEffect);
            deathParticles.transform.position = col.contacts[0].point;

            ParticleSystem dpSystem = deathParticles.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule dpMain = dpSystem.main;

            dpMain.startColor = playerColor;

            Destroy(deathParticles, 2f);

            Invoke("respawn", 0.5f);

            if(col.gameObject.tag == "Powerup"){
                if(col.gameObject.name == "Boost"){
                    boostFactor = 5;
                    Invoke("deactivateBoost", boostDuration);
                } else if(col.gameObject.name == "Triple Jump"){
                    jumpsAvailable = 3;
                    Invoke("deactivateTripleJump", tripleJumpDuration);
                }
            }
        }
    }

    void respawn() 
    {
        canDash = true;
        rb2d.gravityScale = 5f;
        isDashing = false;
        gameObject.SetActive(true);
        transform.position = spawnPoint; // tp the player to the spawnpoint
        mainCam.transform.position = new Vector3(0, 0, -10f);

    } 
    void deactivateBoost(){
        boostFactor = 1;
    }
    void deactivateTripleJump(){
        jumpsAvailable = 2;
    }
}
