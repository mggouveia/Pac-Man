using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacMan : MonoBehaviour
{
    public AudioClip chomp1;
    public AudioClip chomp2;

    public Vector2 orientation;

    public float speed = 4.0f;
    
    public Sprite idleSprite;

    private bool playedChomp1 = false;

    private AudioSource audio;

    private Vector2 direction = Vector2.zero;
    private Vector2 nextDirection;

    private int pelletsConsumed;

    private Node currentNode, previousNode, targetNode;

    // Start is called before the first frame update
    void Start()
    {
        audio = transform.GetComponent<AudioSource>();

        Node node = GetNodeAtPosition(transform.localPosition);

        if (node != null)
        {
            currentNode = node;
            //Debug.Log(currentNode);
        }
        direction = Vector2.left;
        orientation = Vector2.left;
        ChangePosition (direction);

    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();

        Move();

        UpdateOrientation();

        UpdateAnimationState();

        ConsumePellet();
    }

    void PlayChompSound()
    {
        if (playedChomp1)
        {
            //- Play chomp 2, set playedChomp1 to false;
            audio.PlayOneShot(chomp2);
            playedChomp1 = false;

        }
        else
        {
            //-Play chomp 1, set playedChomp1 to true;
            audio.PlayOneShot(chomp1);
            playedChomp1 = true;
        }
    }


    void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangePosition(Vector2.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangePosition(Vector2.right);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangePosition(Vector2.up);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangePosition(Vector2.down);
        }
       
    }

    void ChangePosition (Vector2 d)
    {
        if (d != direction)
            nextDirection = d;

        if (currentNode != null)
        {
            Node moveToNode = CanMove(d);

            if (moveToNode != null)
            {
                direction = d;
                targetNode = moveToNode;
                previousNode = currentNode;
                currentNode = null;
            }
        }
    }

    void Move()
    {
        if (targetNode != currentNode && targetNode != null)
        {
            if (nextDirection == direction * -1)
            {
                direction *= -1;

                Node tempNode = targetNode;

                targetNode = previousNode;

                previousNode = tempNode;

            }

            if (OverShotTarget())
            {
                currentNode = targetNode;

                transform.localPosition = currentNode.transform.position;

                GameObject otherPortal = GetPortal(currentNode.transform.position);

                if (otherPortal != null)
                {
                    transform.localPosition = otherPortal.transform.position;

                    currentNode = otherPortal.GetComponent<Node>();
                }

                Node moveToNode = CanMove(nextDirection);

                if (moveToNode != null)
                    direction = nextDirection;

                if (moveToNode == null)
                    moveToNode = CanMove(direction);

                if (moveToNode != null)
                {
                    targetNode = moveToNode;
                    previousNode = currentNode;
                    currentNode = null;
                }
                else
                {
                    direction = Vector2.zero;
                }

            }
            else
            {
                transform.localPosition += (Vector3)(direction * speed) * Time.deltaTime;
            }
        } 
    }

    void MoveToNode (Vector2 d)
    {
        Node moveToNode = CanMove(d);

        if (moveToNode != null)
        {
            transform.localPosition = moveToNode.transform.position;
            currentNode = moveToNode;
        }
    }

    void UpdateOrientation()
    {
        if (direction == Vector2.left)
        {
            orientation = Vector2.left;
            transform.localScale = new Vector3(-1, 1, 1);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if (direction == Vector2.right)
        {
            orientation = Vector2.right;
            transform.localScale = new Vector3(1, 1, 1);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if (direction == Vector2.up)
        {
            orientation = Vector2.up;
            transform.localScale = new Vector3(1, 1, 1);
            transform.localRotation = Quaternion.Euler(0, 0, 90);

        }
        else if (direction == Vector2.down)
        {
            orientation = Vector2.down;
            transform.localScale = new Vector3(1, 1, 1);
            transform.localRotation = Quaternion.Euler(0, 0, 270);
        }

    }

    void UpdateAnimationState()
    {
        if (direction == Vector2.zero)
        {
            GetComponent<Animator>().enabled = false;
            GetComponent<SpriteRenderer>().sprite = idleSprite;
        } else
        {
            GetComponent<Animator>().enabled = true;
        }
    }

    void ConsumePellet()
    {
        GameObject o = GetTileAtPosition(transform.position);

        if (o != null)
        {
            Tile tile = o.GetComponent<Tile>();

            if (tile != null)
            {
                if (!tile.didConsume && (tile.isPellet || tile.isSuperPellet))
                {
                    o.GetComponent<SpriteRenderer>().enabled = false;
                    tile.didConsume = true;
                    GameObject.Find("Game").GetComponent<GameBoard>().score += 1;
                    pelletsConsumed++;
                    PlayChompSound();

                    if (tile.isSuperPellet)
                    {
                        GameObject[] ghosts = GameObject.FindGameObjectsWithTag("Ghost");

                        foreach (GameObject go in ghosts)
                        {
                            go.GetComponent<Ghost>().StartFrightenedMode();
                        }
                    }
                }
            }
        }
    }

    Node CanMove (Vector2 d)
    {
        Node moveToNode = null;

        for (int i = 0; i < currentNode.neighbors.Length; i++)
        {
            if (currentNode.validDirections [i] == d)
            {
                moveToNode = currentNode.neighbors [i];
                break;
            }
        }

        return moveToNode;
    }

    GameObject GetTileAtPosition (Vector2 pos)
    {
        int tileX = Mathf.RoundToInt(pos.x);
        int tileY = Mathf.RoundToInt(pos.y);

        GameObject tile = GameObject.Find("Game").GetComponent<GameBoard>().board[tileX, tileY];

        if (tile != null)
        return tile;

        return null;
    }

    Node GetNodeAtPosition (Vector2 pos)
    {
        GameObject tile = GameObject.Find("Game").GetComponent<GameBoard> ().board[(int)pos.x, (int)pos.y];

        if (tile != null)
        {
            return tile.GetComponent<Node>();
        }
        return null;

    }

    bool OverShotTarget ()
    {
        float nodeToTarget = LengthFromNode (targetNode.transform.position);
        float nodeToSelf = LengthFromNode (transform.localPosition);

        return nodeToSelf > nodeToTarget;
    }

    float LengthFromNode (Vector2 targetPosition)
    {
        Vector2 vec = targetPosition - (Vector2)previousNode.transform.position;
        return vec.sqrMagnitude;
    }

    GameObject GetPortal (Vector2 pos)
    {
        GameObject tile = GameObject.Find("Game").GetComponent<GameBoard>().board[(int)pos.x, (int)pos.y];

        if (tile != null)
        {
            if (tile.GetComponent<Tile>() != null)
            {

                if (tile.GetComponent<Tile>().isPortal)
                {
                    GameObject otherPortal = tile.GetComponent<Tile>().portalReceiver;
                    return otherPortal;
                }
            }
        }
        return null;
    }


}
