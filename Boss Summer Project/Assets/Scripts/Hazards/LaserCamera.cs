using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserCamera : MonoBehaviour
{
    [SerializeField] private GameObject laserEye;
    [SerializeField] private GameObject laser;
    [SerializeField] private float aimRadius;
    private GameObject playerObj;
    [SerializeField] private LayerMask shootableLayers;     //Layer mask that contains any layers which can be shot by the laser
    [SerializeField] private LayerMask detectableLayers;    //Layer mask that contains any layers which can be detected by the camera
    [SerializeField] private LayerMask disableLayers;
    [SerializeField] private float laserEyeFadeInDuration;
    [SerializeField] private float cameraHiddenTint;
    [SerializeField] private float laserDelay;
    [SerializeField] private float laserFadeOutTime;
    [SerializeField] private Transform laserStartPosition;

    private PlayerController player;
    private LineRenderer laserRenderer;
    private bool playerDetected;
    private bool openFire = true;

    // Start is called before the first frame update
    void Start()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj.GetComponent<PlayerController>();
        laserRenderer = laser.GetComponent<LineRenderer>();

        VisualEffects.SetAlpha(laserEye, 0);
        VisualEffects.SetAlpha(laserRenderer, 0);
    }

    // Update is called once per frame
    void Update()
    {
        // Just don't do anything if le electrick block is in range
        if (Physics2D.OverlapCircle(transform.position, aimRadius, disableLayers)) return;

        playerDetected = (player.transform.position.y > transform.position.y) ? false : Physics2D.OverlapCircle(transform.position, aimRadius, detectableLayers);

        if (playerDetected)
        {

            //If the laser eye is hidden, reveal the camera and laser eye
            if (VisualEffects.GetAlpha(laserEye) == 0)
            {
                StartCoroutine(VisualEffects.FadeIn(laserEye, laserEyeFadeInDuration));
                StartCoroutine(VisualEffects.FadeToColor(gameObject, laserEyeFadeInDuration, Color.white));
            }

            //Make the laser point to the player
            laserEye.transform.right = playerObj.transform.position - laserEye.transform.position;
            laserEye.transform.Rotate(new Vector3(0, 0, 90));    //offset 90 degrees in z axis (fix)

            if (openFire)
                StartCoroutine(ShootSequence());
        }
        else
        {

            //If the laser eye is revealed, hide the camera and laser eye
            if (VisualEffects.GetAlpha(laserEye) == 1)
            {
                StartCoroutine(VisualEffects.FadeOut(laserEye, laserEyeFadeInDuration));
                StartCoroutine(VisualEffects.FadeToColor(gameObject, laserEyeFadeInDuration, Color.black));
            }
        }
    }

    private IEnumerator ShootSequence()
    {
        openFire = false;

        //Wait, then shoot the laser
        yield return new WaitForSeconds(laserDelay);

        //If the player is out of range now, don't shoot
        if (!playerDetected)
        {
            openFire = true;
            yield break;
        }

        //Fire a laser at the player or shield, whichever comes first
        StartCoroutine(FireLaser());

        openFire = true;
    }

    private IEnumerator FireLaser()
    {

        laser.SetActive(true);

        //Draw the laser from the laser eye to the player
        laserRenderer.SetPosition(0, laserStartPosition.position);

        Vector2 direction = (Vector2)playerObj.transform.position - (Vector2)laserStartPosition.position;
        RaycastHit2D hit = Physics2D.Raycast(laserStartPosition.position, direction.normalized, direction.magnitude, shootableLayers);

        if (hit)
        {

            laserRenderer.SetPosition(1, hit.point);

            GameObject target = hit.collider.gameObject;
            Damageable targetScript = target.GetComponent<Damageable>();

            //Laser appears while tinting the target red, inflicting damage (if target is damageable)
            VisualEffects.SetAlpha(laserRenderer, 1);
            if (targetScript != null)
            {
                VisualEffects.SetColor(target, Color.red);
                targetScript.TakeDamage(30);
            }

            //Laser immediately starts fading out along with red tint (white tint = restore original colour)
            if (targetScript != null)
                StartCoroutine(VisualEffects.FadeToColor(target, laserFadeOutTime, Color.white));
            yield return StartCoroutine(VisualEffects.FadeOut(laserRenderer, laserFadeOutTime));

        }

        laser.SetActive(false);

    }
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.layer == 16)
        {
            gameObject.SetActive(false);
        }
    }

    public void Respawn()
    {
        gameObject.SetActive(true);
    }
}
